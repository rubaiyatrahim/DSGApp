using DSGClient;
using DSGTool.Data.Models;
using System.Collections.Concurrent;
using System.Text;

namespace DSGTool
{
    public partial class MainForm : Form
    {
        // DSG clients pool
        private DSGClientPool _clientPool;

        // Cancellation token
        private CancellationTokenSource _cts;

        // Connection properties
        private const string HOST = "192.168.17.14";
        private const string USERID = "SD_TEST4";
        private const string PASSWORD = "cse.123";
        private const int HEARTBEAT_INTERVAL_SECONDS = 2;

        // Cards and stats dictionaries
        private Dictionary<string, GatewayCard> _cards = new();
        private Dictionary<string, GatewayStats> _stats = new();

        // Logging
        private readonly ConcurrentQueue<string> _logQueue = new();
        private System.Windows.Forms.Timer _logTimer;


        public MainForm() 
        { 
            InitializeComponent();

            // Setup periodic log flush (Timer runs on UI thread)
            _logTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _logTimer.Tick += (s, e) => FlushLogsToUI();

        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Create cancellation token
            _cts = new CancellationTokenSource();

            // Hook Console.WriteLine to Log() method
            Console.SetOut(new TextBoxWriter(Log));

            // Initialize timer to flush logs periodically ---
            _logTimer.Start();

            await Task.Run(() =>
            {
                LoadClientsPool();
            });
            BuildCards();
        }

        private void LoadClientsPool()
        {
            var loader = new ClientLoader();

            //LoadSampleDataFromCode();
            //loader.DeleteAllMasterData();
            //AddSampleDataToDb(loader);

            _clientPool = loader.LoadClients();
            _clientPool.MessageReceived += OnMessageReceived;
            _clientPool.StatusChanged += OnStatusChanged;
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
            }
            catch (Exception ex)
            {
                Log("Connection error: " + ex.Message);
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

        private async void btnStop_Click(object sender, EventArgs e) => await StopAllAsync();

        private async void btnQuit_Click(object sender, EventArgs e) => Close();

        // Queue log messages instead of blocking UI
        private void Log(string message)
        {
            string logTime = DateTime.Now.ToString("[dd-MMM-yyyy HH:mm:ss.fff]");
            _logQueue.Enqueue($"{logTime} {message}");

            // Log to file
            using (StreamWriter sw = new StreamWriter("DSGClient_Log.txt", true))
            {
                sw.WriteLine($"{logTime} {message}");
            }

        }

        // Flush queue to UI every 50ms
        private void FlushLogsToUI()
        {
            // If queue empty, nothing to do
            if (_logQueue.IsEmpty) return;

            // Ensure we run on UI thread
            if (InvokeRequired)
            {
                BeginInvoke(new Action(FlushLogsToUI));
                return;
            }

            var sb = new StringBuilder();
            while (_logQueue.TryDequeue(out var entry))
                sb.AppendLine(entry);

            if (sb.Length == 0) return;

            txtLog.AppendText(sb.ToString());
            txtLog.ScrollToCaret();

            // Trim if grows too large for performance
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
                await StopAllAsync();
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
                    var stopTask = _clientPool.StopAllAsync();
                    await Task.WhenAny(stopTask, Task.Delay(200)); // wait max 200ms
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
    }
}
