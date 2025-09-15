using DSGClient;

namespace DSGTool
{
    /**
     * Main form class.
     * */
    public partial class MainForm : Form
    {
        // DSG client
        private DSGClientPool _clientPool;

        // Cancellation token
        private CancellationTokenSource _cts;

        // Connection properties
        private const string HOST = "192.168.17.14";
        private const string USERID = "SD_TEST4";
        private const string PASSWORD = "cse.123";
        private const int HEARTBEAT_INTERVAL_SECONDS = 2;

        public MainForm() { InitializeComponent(); }
        private Dictionary<string, GatewayCard> _cards = new();
        private Dictionary<string, GatewayStats> _stats = new();
        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Create cancellation token
            _cts = new CancellationTokenSource();

            // Hook Console.WriteLine to Log() method
            Console.SetOut(new TextBoxWriter(Log));

            Gateway gatewayOM = new Gateway("CSETEST1", "DSGOMGateway", HOST, 7530, USERID, PASSWORD);
            Gateway gatewayMD = new Gateway("CSETEST1", "DSGMDGateway", HOST, 7536, USERID, PASSWORD);

            MessageType msgType_EXP_INDEX_WATCH = new MessageType("EXP_INDEX_WATCH", "15203", false),
                msgType_EXP_STAT_UPDATE = new MessageType("EXP_STAT_UPDATE", "15355", false),
                msgType_Announcement = new MessageType("Announcement", "618", true);

            // Create multi-client
            _clientPool = new DSGClientPool();
            _clientPool.AddClient(gatewayOM, new List<MessageType> { msgType_EXP_INDEX_WATCH, msgType_EXP_STAT_UPDATE }, HEARTBEAT_INTERVAL_SECONDS);
            _clientPool.AddClient(gatewayMD, new List<MessageType> { msgType_Announcement }, HEARTBEAT_INTERVAL_SECONDS);

            flowLayoutPanel1.WrapContents = true;
            flowLayoutPanel1.AutoScroll = true;

            var card1 = new GatewayCard(gatewayOM.GatewayName);
            flowLayoutPanel1.Controls.Add(card1);
            _cards[gatewayOM.GatewayName] = card1;

            var stats1 = new GatewayStats(gatewayOM.GatewayName);
            _stats[gatewayOM.GatewayName] = stats1;

            var card2 = new GatewayCard(gatewayMD.GatewayName);
            flowLayoutPanel1.Controls.Add(card2);
            _cards[gatewayMD.GatewayName] = card2;

            var stats2 = new GatewayStats(gatewayMD.GatewayName);
            _stats[gatewayMD.GatewayName] = stats2;


            _clientPool.MessageReceived += OnMessageReceived;
            _clientPool.StatusChanged += OnStatusChanged;
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

                //card.UpdateStatus(_clientPool.GetClientByGatewayName(gatewayName).IsConnected);
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
            => await _clientPool.SendDownloadAllAsync("1", "1", "1000000");
        
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
                if (_clientPool != null)
                    await _clientPool.StopAllAsync();
            }
            catch (Exception ex)
            {
                Log("Error during shutdown: " + ex.Message);
            }
            finally
            {
                _cts?.Cancel();
            }
        }
    }
}
