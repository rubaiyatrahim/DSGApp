using DSGClient;
using DSGTool.Data.Models;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace DSGTool
{
    public partial class MainForm : Form
    {
        // DSG clients pool
        private DSGClientPool _clientPool;

        // Cancellation token
        private CancellationTokenSource _cts;
        private CancellationTokenSource _logCts = new CancellationTokenSource();

        // Connection properties
        private const string HOST = "192.168.17.14";
        private const string USERID = "SD_TEST4";
        private const string PASSWORD = "cse.123";
        private const int HEARTBEAT_INTERVAL_SECONDS = 2;

        // Cards and stats dictionaries
        private Dictionary<string, GatewayCard> _cards = new();
        private Dictionary<string, GatewayStats> _stats = new();

        // Logging
        private string GetLogTime() => DateTime.Now.ToString("[dd-MMM-yyyy HH:mm:ss.fff]");
        private readonly ConcurrentQueue<string> _logUIQueue = new();
        private readonly BlockingCollection<string> _logFileQueue = new();
        private string _logFolder = "";
        private string _logFile = "DSGClient_log.txt";

        private ClientLoader loader;

        public MainForm()
        {
            InitializeComponent();
            SetLogPath();
            StartLogUIWriterTask();
            StartLogFileWriterTask();
        }

        private void SetLogPath()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _logFolder = Path.Combine(exePath, $"{DateTime.Now:yyyyMMdd}");
            if (!Directory.Exists(_logFolder))
                Directory.CreateDirectory(_logFolder);
            _logFile = Path.Combine(_logFolder, _logFile);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Create cancellation token
            _cts = new CancellationTokenSource();

            // Initially disable buttons until clients are loaded
            SetButtonStatesOnLoad(false);
            Log("Application started.");
        }

        private void LoadClientsPool()
        {
            loader = new ClientLoader();

            //LoadSampleDataFromCode();
            //loader.DeleteAllMasterData();
            //AddSampleDataToDb(loader);

            _clientPool = loader.LoadClients();
            _clientPool.MessageReceived += OnMessageReceived;
            _clientPool.StatusChanged += OnStatusChanged;
            DSGClient.DSGClient.Loader.MessageReceivedDB += OnMessageReceivedDB;
        }

        private void LoadSampleDataFromCode()
        {
            Gateway gatewayOM = new Gateway(null, "1", "CSETEST1", "DSGOMGateway", HOST, 7530, USERID, PASSWORD);
            Gateway gatewayMD = new Gateway(null, "1", "CSETEST1", "DSGMDGateway", HOST, 7536, USERID, PASSWORD);

            MessageType msgType_EXP_INDEX_WATCH = new MessageType(null, "EXP_INDEX_WATCH", "15203", false),
                msgType_EXP_STAT_UPDATE = new MessageType(null, "EXP_STAT_UPDATE", "15355", false),
                msgType_Announcement = new MessageType(null, "Announcement", "618", true);

            // Create multi-client
            _clientPool = new DSGClientPool();
            _clientPool.AddClient(gatewayOM, new List<MessageType> { msgType_EXP_INDEX_WATCH, msgType_EXP_STAT_UPDATE }, "1", "1000000", HEARTBEAT_INTERVAL_SECONDS);
            _clientPool.AddClient(gatewayMD, new List<MessageType> { msgType_Announcement }, "1", "1000000", HEARTBEAT_INTERVAL_SECONDS);
        }

        private void AddSampleDataToDb(ClientLoader loader)
        {
            // Add gateways
            int g1 = loader.AddGateway(new Gateway(null, "1", "CSETEST1", "DSGOMGateway", HOST, 7530, USERID, PASSWORD));
            int g2 = loader.AddGateway(new Gateway(null, "1", "CSETEST1", "DSGMDGateway", HOST, 7536, USERID, PASSWORD));

            // Add message types
            int m1 = loader.AddMessageType(new MessageType(null, "EXP_INDEX_WATCH", "15203", false));
            int m2 = loader.AddMessageType(new MessageType(null, "EXP_STAT_UPDATE", "15355", false));
            int m3 = loader.AddMessageType(new MessageType(null, "Announcement", "618", true));

            // Add gateway message types
            loader.AddGatewayMessageType(g1, m1);
            loader.AddGatewayMessageType(g1, m2);
            loader.AddGatewayMessageType(g2, m3);

            // Add DSG clients
            loader.AddDSGClient(new DSGClientEntity(null, g1, "1", "1000000", HEARTBEAT_INTERVAL_SECONDS));
            loader.AddDSGClient(new DSGClientEntity(null, g2, "1", "1000000", HEARTBEAT_INTERVAL_SECONDS));
        }

        private void BuildCards()
        {
            flowLayoutPanel1.WrapContents = true;
            flowLayoutPanel1.AutoScroll = true;

            foreach (var client in _clientPool.Clients)
            {
                string gatewayName = client.GatewayName;

                var stats = new GatewayStats(gatewayName);
                _stats[gatewayName] = stats;

                var card = new GatewayCard(gatewayName);

                card.StartClicked += async gw => await client.StartAsync(_cts.Token);
                card.DownloadClicked += async gw => await client.DownloadAsync();
                card.StopClicked += async gw => await client.StopAsync();
                card.DeleteClickedAsync += async gw => {
                    await Task.Run(() =>
                    {
                        loader.DeleteMessagesByGateway(gatewayName);
                        Log($"All messages for gateway {gatewayName} deleted from database.");
                        if (card.InvokeRequired)
                            card.Invoke(() => card.ResetCounts());
                        else
                            card.ResetCounts();
                        stats.ResetCounts();
                    });
                };

                flowLayoutPanel1.Controls.Add(card);
                _cards[gatewayName] = card;
            }
        }

        private void OnMessageReceived(string gatewayName, string msgType)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnMessageReceived(gatewayName, msgType)));
                return;
            }

            if (_stats.TryGetValue(gatewayName, out var stats) && _cards.TryGetValue(gatewayName, out var card))
            {
                stats.IncrementMessageCount(msgType);

                card.UpdateTotalCount(stats.TotalMessages);
                card.UpdateTotalExceptHBCount(stats.TotalMessages - stats.GetCount("0"));
                card.UpdateMessageTypeCount(msgType, stats.GetCount(msgType));
            }
        }

        private void OnMessageReceivedDB(string gatewayName, string messageId, string tableName, long messageCount)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnMessageReceivedDB(gatewayName, messageId, tableName, messageCount)));
                return;
            }
            if (_stats.TryGetValue(gatewayName, out var stats) && _cards.TryGetValue(gatewayName, out var card))
            {
                stats.SetMessageCountDB(messageId, messageCount);
                card.UpdateMessageTypeCountDB(messageId, messageCount);
            }
        }

        private void OnStatusChanged(string gatewayName, bool connected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnStatusChanged(gatewayName, connected)));
                return;
            }

            if (_cards.TryGetValue(gatewayName, out var card))
            {
                card.UpdateStatus(connected);
            }
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                await _clientPool.StartAllAsync(_cts.Token);
                Log("Clients pool started.");
                SetButtonStatesOnConnect(true);
            }
            catch (Exception ex)
            {
                Log("Connection error: " + ex.Message);
                SetButtonStatesOnConnect(false);
            }
        }

        private async void btnDownload_Click(object sender, EventArgs e)
            => await _clientPool.SendDownloadAllAsync();

        private async void btnHeartbeat_Click(object sender, EventArgs e)
        {
            try
            {
                await _clientPool.SendHeartbeatAllAsync();
                Log("Manual heartbeat sent to all gateways.");
            }
            catch (Exception ex)
            {
                Log("Error during manually sending heartbeat: " + ex.Message);
            }
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            await StopAllAsync();
            SetButtonStatesOnConnect(false);
        }

        private async void btnQuit_Click(object sender, EventArgs e) => Close();

        private void StartLogUIWriterTask()
        {
            // Hook Console.WriteLine to Log() method
            Console.SetOut(new TextBoxWriter(Log));

            Task.Run(async () =>
            {
                while (!_logCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(50); // small interval

                    if (_logUIQueue.IsEmpty)
                        continue;

                    FlushLogsBatchSafe();
                }
            }, _logCts.Token);
        }

        private void FlushLogsBatchSafe()
        {
            if (txtLog.IsDisposed || txtLog.Disposing)
                return;

            if (txtLog.InvokeRequired)
            {
                try
                {
                    txtLog.Invoke(new Action(FlushLogsBatchSafe));
                }
                catch (ObjectDisposedException)
                {
                    return; // safely ignore
                }
            }
            else
            {
                // Flush all queued logs at once
                if (_logUIQueue.IsEmpty) return;

                var sb = new System.Text.StringBuilder();
                while (_logUIQueue.TryDequeue(out var line))
                {
                    sb.AppendLine(line);
                }

                txtLog.AppendText(sb.ToString());
                txtLog.ScrollToCaret();
            }
        }

        private void StartLogFileWriterTask()
        {
            Task.Run(async () =>
            {
                try
                {
                    using var writer = new StreamWriter(_logFile, append: true, Encoding.UTF8);
                    foreach (var line in _logFileQueue.GetConsumingEnumerable())
                    {
                        await writer.WriteLineAsync(line);
                        await writer.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    // If file write fails, show in UI
                    _logUIQueue.Enqueue($"[FileWriterError] {ex.Message}");
                }
            });
        }

        // Queue log messages instead of blocking UI
        private void Log(string message)
        {
            string logTime = GetLogTime();
            string formatted = $"{logTime} {message}";

            _logUIQueue.Enqueue(formatted);
            _logFileQueue.Add(formatted);
        }

        // Flush queue to UI 
        private void FlushLogsToUI()
        {
            if (_logUIQueue.IsEmpty) return;

            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action(FlushLogsToUI));
                return;
            }

            var sb = new StringBuilder();
            while (_logUIQueue.TryDequeue(out var entry))
                sb.AppendLine(entry);

            if (sb.Length > 0)
            {
                txtLog.AppendText(sb.ToString());
                txtLog.ScrollToCaret();
            }

            // Trim to prevent memory issues
            const int maxChars = 100_000;
            const int keepChars = 40_000;
            if (txtLog.TextLength > maxChars)
            {
                txtLog.Text = txtLog.Text[^keepChars..];
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // Stop all clients first
                if (_clientPool != null)
                    await _clientPool.StopAllAsync();

                // Shutdown the shared XML loader AFTER clients are done
                await DSGClient.DSGClient.Loader.ShutdownAsync();

                _logCts.Cancel();

                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                Log("Error during shutdown: " + ex.Message);
            }
        }


        private async Task StopAllAsync()
        {
            try
            {
                // Cancel application-wide token first for immediate effect
                _cts?.Cancel();

                if (_clientPool != null)
                {
                    // Fire-and-forget stop to make UI responsive instantly
                    await _clientPool.StopAllAsync();
                    //await Task.WhenAny(stopTask, Task.Delay(200)); // wait max 200ms
                }
            }
            catch (Exception ex)
            {
                Log("Error during shutdown: " + ex.Message);
            }
        }

        private void btnDbManager_Click(object sender, EventArgs e)
        {
            // Pass your connection string here
            string connectionString = "Server=192.168.102.15;Database=DSGData;User Id=rubaiyat;Password=12345;TrustServerCertificate=True;";

            using var form = new Config.ConfigurationManager(connectionString);
            form.ShowDialog(); // Modal dialog
        }

        private async void btnLoadClients_Click(object sender, EventArgs e)
        {
            btnLoadClients.Enabled = false;
            await Task.Run(() =>
            {
                LoadClientsPool();
            });
            if (_clientPool != null)
            {
                BuildCards();
                SetButtonStatesOnConnect(false);
                btnDelete.Enabled = true;
            }
            else
                btnLoadClients.Enabled = true;
        }

        private void SetButtonStatesOnLoad(bool enabled)
        {
            btnLoadClients.Enabled = !enabled;
            btnConnect.Enabled = enabled;
            btnDownload.Enabled = enabled;
            btnHeartbeat.Enabled = enabled;
            btnStop.Enabled = enabled;
            btnDelete.Enabled = enabled;
        }
        private void SetButtonStatesOnConnect(bool enabled)
        {
            btnConnect.Enabled = !enabled;
            btnDownload.Enabled = enabled;
            btnHeartbeat.Enabled = enabled;
            btnStop.Enabled = enabled;
        }

        private async void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                $"Are you sure you want to delete all messages for all gateways?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                if (_cards == null || _cards.Count == 0)
                    return;

                btnDelete.Enabled = false;
                var tasks = new List<Task>();

                foreach (var card in _cards.Values)
                {
                    tasks.Add(card.RaiseDeleteAsync());
                }

                await Task.WhenAll(tasks);
                MessageBox.Show($"Messages for all gateways deleted successfully.",
                    "Delete Completed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during deletion: {ex.Message}",
                    "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnDelete.Enabled = true;
            }
            finally
            {
                btnDelete.Enabled = true;
            }
        }
    }
}
