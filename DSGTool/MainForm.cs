using DSGClient;
using DSGTool.Data.Models;

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

        public MainForm() { InitializeComponent(); }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Create cancellation token
            _cts = new CancellationTokenSource();

            // Hook Console.WriteLine to Log() method
            Console.SetOut(new TextBoxWriter(Log));

            LoadClientsPool();
            BuildCards();
        }

        private void LoadClientsPool()
        {
            /*
            Gateway gatewayOM = new Gateway("1", "CSETEST1", "DSGOMGateway", HOST, 7530, USERID, PASSWORD);
            Gateway gatewayMD = new Gateway("1", "CSETEST1", "DSGMDGateway", HOST, 7536, USERID, PASSWORD);

            MessageType msgType_EXP_INDEX_WATCH = new MessageType("EXP_INDEX_WATCH", "15203", false),
                msgType_EXP_STAT_UPDATE = new MessageType("EXP_STAT_UPDATE", "15355", false),
                msgType_Announcement = new MessageType("Announcement", "618", true);

            // Create multi-client
            _clientPool = new DSGClientPool();
            _clientPool.AddClient(gatewayOM, new List<MessageType> { msgType_EXP_INDEX_WATCH, msgType_EXP_STAT_UPDATE }, "1", "1000000", HEARTBEAT_INTERVAL_SECONDS);
            _clientPool.AddClient(gatewayMD, new List<MessageType> { msgType_Announcement }, "1", "1000000", HEARTBEAT_INTERVAL_SECONDS);

            _clientPool.MessageReceived += OnMessageReceived;
            _clientPool.StatusChanged += OnStatusChanged;
            */

            var loader = new ClientLoader();
            
            //loader.DeleteAllData();
            //LoadSampleData(loader);

            _clientPool = loader.LoadClients();
            _clientPool.MessageReceived += OnMessageReceived;
            _clientPool.StatusChanged += OnStatusChanged;
        }

        private void LoadSampleData(ClientLoader loader)
        {
            // Add gateways
            int g1 = loader.AddGateway(new Gateway(null, "1", "CSETEST1", "DSGOMGateway", HOST, 7530, USERID, PASSWORD));
            int g2 = loader.AddGateway(new Gateway(null, "1", "CSETEST1", "DSGMDGateway", HOST, 7536, USERID, PASSWORD));

            // Add message types
            int m1 = loader.AddMessageType(new MessageTypeEntity(null, "EXP_INDEX_WATCH", "15203", false));
            int m2 = loader.AddMessageType(new MessageTypeEntity(null, "EXP_STAT_UPDATE", "15355", false));
            int m3 = loader.AddMessageType(new MessageTypeEntity(null, "Announcement", "618", true));

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
        
        /**
         * Log a message to the text box.
         * 
         * @param message: Message to log.
         * */
        private void Log(string message)
        {
            if (InvokeRequired) // Check if we're on the UI thread
            {
                BeginInvoke(new Action(() => Log(message))); // Run on the UI thread
                return;
            }

            // Log the message to the text box
            string logTime = DateTime.Now.ToString("[dd-MMM-yyyy HH:mm:ss.fff]");
            txtLog.AppendText($"\r\n{logTime} {message}");
            txtLog.ScrollToCaret();

            // Log to file
            using (StreamWriter sw = new StreamWriter("DSGClient_Log.txt", true))
            {
                sw.WriteLine($"{logTime} {message}");
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

    }
}
