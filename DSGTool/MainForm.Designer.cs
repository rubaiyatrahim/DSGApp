namespace DSGTool
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Button btnHeartbeat;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnQuit;
        private System.Windows.Forms.RichTextBox txtLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnDownload = new Button();
            btnHeartbeat = new Button();
            btnStop = new Button();
            btnQuit = new Button();
            txtLog = new RichTextBox();
            btnConnect = new Button();
            flowLayoutPanelCards = new FlowLayoutPanel();
            btnDbManager = new Button();
            btnLoadClients = new Button();
            btnDelete = new Button();
            flowLayoutButtons = new FlowLayoutPanel();
            flowLayoutButtons.SuspendLayout();
            SuspendLayout();
            // 
            // btnDownload
            // 
            btnDownload.Location = new Point(225, 13);
            btnDownload.Name = "btnDownload";
            btnDownload.Size = new Size(100, 30);
            btnDownload.TabIndex = 2;
            btnDownload.Text = "Download";
            btnDownload.Click += btnDownload_Click;
            // 
            // btnHeartbeat
            // 
            btnHeartbeat.Location = new Point(331, 13);
            btnHeartbeat.Name = "btnHeartbeat";
            btnHeartbeat.Size = new Size(100, 30);
            btnHeartbeat.TabIndex = 3;
            btnHeartbeat.Text = "Heartbeat";
            btnHeartbeat.Click += btnHeartbeat_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(437, 13);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(100, 30);
            btnStop.TabIndex = 4;
            btnStop.Text = "Stop";
            btnStop.Click += btnStop_Click;
            // 
            // btnQuit
            // 
            btnQuit.Location = new Point(763, 13);
            btnQuit.Name = "btnQuit";
            btnQuit.Size = new Size(100, 30);
            btnQuit.TabIndex = 5;
            btnQuit.Text = "Quit";
            btnQuit.Click += btnQuit_Click;
            // 
            // txtLog
            // 
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Location = new Point(0, 0);
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            txtLog.Size = new Size(100, 96);
            txtLog.TabIndex = 7;
            txtLog.Text = "";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(119, 13);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 30);
            btnConnect.TabIndex = 1;
            btnConnect.Text = "Connect";
            btnConnect.Click += btnConnect_Click;
            // 
            // flowLayoutPanelCards
            // 
            flowLayoutPanelCards.Location = new Point(0, 0);
            flowLayoutPanelCards.Name = "flowLayoutPanelCards";
            flowLayoutPanelCards.Size = new Size(200, 100);
            flowLayoutPanelCards.TabIndex = 6;
            // 
            // btnDbManager
            // 
            btnDbManager.Location = new Point(653, 13);
            btnDbManager.Name = "btnDbManager";
            btnDbManager.Size = new Size(104, 30);
            btnDbManager.TabIndex = 6;
            btnDbManager.Text = "Config";
            btnDbManager.UseVisualStyleBackColor = true;
            btnDbManager.Click += btnDbManager_Click;
            // 
            // btnLoadClients
            // 
            btnLoadClients.Location = new Point(13, 13);
            btnLoadClients.Name = "btnLoadClients";
            btnLoadClients.Size = new Size(100, 30);
            btnLoadClients.TabIndex = 0;
            btnLoadClients.Text = "Load Clients";
            btnLoadClients.Click += btnLoadClients_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(543, 13);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(104, 30);
            btnDelete.TabIndex = 8;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // flowLayoutButtons
            // 
            flowLayoutButtons.Controls.Add(btnLoadClients);
            flowLayoutButtons.Controls.Add(btnConnect);
            flowLayoutButtons.Controls.Add(btnDownload);
            flowLayoutButtons.Controls.Add(btnHeartbeat);
            flowLayoutButtons.Controls.Add(btnStop);
            flowLayoutButtons.Controls.Add(btnDelete);
            flowLayoutButtons.Controls.Add(btnDbManager);
            flowLayoutButtons.Controls.Add(btnQuit);
            flowLayoutButtons.Dock = DockStyle.Top;
            flowLayoutButtons.Location = new Point(0, 0);
            flowLayoutButtons.Margin = new Padding(5, 0, 0, 0);
            flowLayoutButtons.Name = "flowLayoutButtons";
            flowLayoutButtons.Padding = new Padding(10);
            flowLayoutButtons.Size = new Size(1171, 100);
            flowLayoutButtons.TabIndex = 9;
            // 
            // MainForm
            // 
            ClientSize = new Size(1160, 648);
            Controls.Add(flowLayoutButtons);
            Controls.Add(flowLayoutPanelCards);
            Controls.Add(txtLog);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DSG Client GUI";
            Load += MainForm_Load;
            flowLayoutButtons.ResumeLayout(false);
            ResumeLayout(false);
        }
        private Button btnConnect;
        private FlowLayoutPanel flowLayoutPanelCards;
        private Button btnDbManager;
        private Button btnLoadClients;
        private Button btnDelete;
        private FlowLayoutPanel flowLayoutButtons;
    }
}
