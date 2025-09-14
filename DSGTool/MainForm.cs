using DSGClient;
using System.ComponentModel;
using System.Windows.Forms;

namespace DSGTool
{
    /**
     * Main form class.
     * */
    public partial class MainForm : Form
    {
        private BindingList<ClientViewModel> _clients = new();

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
            InitializeClientGrid();
        }

        private void InitializeClientGrid()
        {
            dataGridViewClients.AutoGenerateColumns = false;

            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Gateway", DataPropertyName = "Name" });
            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Host", DataPropertyName = "Host" });
            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Port", DataPropertyName = "Port" });
            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Message Types", DataPropertyName = "MessageTypes" });
            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", DataPropertyName = "Status" });

            dataGridViewClients.Columns.Add(new DataGridViewButtonColumn { Name = "ConnectButton", HeaderText = "", Text = "Connect", UseColumnTextForButtonValue = true });
            dataGridViewClients.Columns.Add(new DataGridViewButtonColumn { Name = "StopButton", HeaderText = "", Text = "Stop", UseColumnTextForButtonValue = true });
            dataGridViewClients.Columns.Add(new DataGridViewButtonColumn { Name = "DownloadButton", HeaderText = "", Text = "Download", UseColumnTextForButtonValue = true });

            dataGridViewClients.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "Message Counts",
                DataPropertyName = "MessageCountsText", // property from a view model
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dataGridViewClients.DataSource = _clients;
            dataGridViewClients.CellContentClick += dataGridViewClients_CellContentClick;

        }

        private async void dataGridViewClients_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var client = _clients[e.RowIndex];

            var columnName = dataGridViewClients.Columns[e.ColumnIndex].Name;
            try
            {
                if (columnName == "ConnectButton")
                {
                    await client.Client.StartAsync(_cts.Token);
                    client.Status = "Connected";
                    Log($"{client.Name} connected.");
                }
                else if (columnName == "StopButton")
                {
                    await client.Client.StopAsync();
                    client.Status = "Stopped";
                    Log($"{client.Name} stopped.");
                }
                else if (columnName == "DownloadButton")
                {
                    await client.Client.DownloadAsync("1", "1", "1000000");
                    Log($"{client.Name} download requested.");
                }
                ApplyRowColor(client);
                //dataGridViewClients.Refresh(); // Refresh UI to update status column
            }
            catch (Exception ex)
            {
                Log($"Error on {columnName} for {client.Name}: {ex.Message}");
            }
        }
        private void ApplyRowColor(ClientViewModel client)
        {
            var idx = _clients.IndexOf(client);
            if (idx < 0) return;
            var row = dataGridViewClients.Rows[idx];

            switch (client.Status)
            {
                case "Connected":
                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                    break;
                case "Stopped":
                    row.DefaultCellStyle.BackColor = Color.LightYellow;
                    break;
                default:
                    row.DefaultCellStyle.BackColor = Color.White;
                    break;
            }
        }

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
            _clientPool.MessageReceived += OnMessageReceived;
            var clientOM = _clientPool.AddClient(gatewayOM, new List<MessageType> { msgType_EXP_INDEX_WATCH, msgType_EXP_STAT_UPDATE }, HEARTBEAT_INTERVAL_SECONDS);
            var clientMD = _clientPool.AddClient(gatewayMD, new List<MessageType> { msgType_Announcement }, HEARTBEAT_INTERVAL_SECONDS);

            _clients.Add(new ClientViewModel(gatewayOM) { Name = gatewayOM.GatewayName, Host = gatewayOM.Host, Port = gatewayOM.Port, MessageTypes = clientOM.MessageTypes, Status = "Stopped", Client = clientOM });
            _clients.Add(new ClientViewModel(gatewayMD) { Name = gatewayMD.GatewayName, Host = gatewayMD.Host, Port = gatewayMD.Port, MessageTypes = clientMD.MessageTypes, Status = "Stopped", Client = clientMD });
            
            dataGridViewClients.DataSource = _clients;
        }

        private void OnMessageReceived(string gatewayName, string msgType)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnMessageReceived(gatewayName, msgType)));
                return;
            }

            var clientVm = _clients.FirstOrDefault(c => c.Gateway.GatewayName == gatewayName);
            if (clientVm != null)
            {
                clientVm.Gateway.IncrementMessageCount(msgType);
                clientVm.Refresh(); // tells DataGridView to rebind MessageCountsText
                dataGridViewClients.Refresh();
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

                foreach (var c in _clients)
                    c.Status = "Stopped";

                dataGridViewClients.Refresh();
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
