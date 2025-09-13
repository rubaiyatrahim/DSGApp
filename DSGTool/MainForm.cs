using DSGClient;
using System.Collections.Concurrent;
using System.Text;

namespace DSGTool
{
    /**
     * Main form class.
     * */
    public partial class MainForm : Form
    {
        // Thread-safe log queue
        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly System.Windows.Forms.Timer _logTimer;
        private readonly string _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DSGClient_Log.txt");
        private readonly object _fileLock = new();

        // DSG client
        private DSGClientPool _clientPool;

        // Cancellation token
        private CancellationTokenSource _cts;

        // Connection properties
        private const string HOST = "192.168.17.14";
        private const string USERID = "SD_TEST4";
        private const string PASSWORD = "cse.123";
        private const int HEARTBEAT_INTERVAL_SECONDS = 2;

        public MainForm() 
        { 
            InitializeComponent();

            // Setup periodic UI log flush (every 200 ms)
            _logTimer = new System.Windows.Forms.Timer { Interval = 10 };
            _logTimer.Tick += (s, e) => FlushLogQueue();
            _logTimer.Start();
        }

        /*
        private int _downloadLogCounter = 0;

        public void LogDownload(string message)
        {
            if (++_downloadLogCounter % 10 == 0) // Only log every 100th event
                Log($"[Download] {message}");
        }*/

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
            string line = $"[{DateTime.Now:dd-MMM-yyyy HH:mm:ss.fff}] {message}";
            _logQueue.Enqueue(line);

            // Write to file asynchronously
            Task.Run(() => AppendLogToFile(line));
            /*
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
            }*/
        }

        /// <summary>
        /// Append log line to file (async, thread-safe).
        /// </summary>
        private void AppendLogToFile(string line)
        {
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // Optional: handle logging errors (e.g. disk full)
            }
        }

        /// <summary>
        /// Flush queued log lines to UI (runs every 200ms).
        /// </summary>
        private void FlushLogQueue()
        {
            if (_logQueue.IsEmpty) return;

            StringBuilder sb = new();
            while (_logQueue.TryDequeue(out string line))
                sb.AppendLine(line);

            txtLog.AppendText(Environment.NewLine + sb.ToString());
            txtLog.ScrollToCaret();
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
