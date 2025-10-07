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
            flowLayoutPanel1 = new FlowLayoutPanel();
            btnDbManager = new Button();
            btnLoadClients = new Button();
            SuspendLayout();
            // 
            // btnDownload
            // 
            btnDownload.Location = new Point(235, 12);
            btnDownload.Name = "btnDownload";
            btnDownload.Size = new Size(100, 30);
            btnDownload.TabIndex = 2;
            btnDownload.Text = "Download";
            btnDownload.Click += btnDownload_Click;
            // 
            // btnHeartbeat
            // 
            btnHeartbeat.Location = new Point(341, 12);
            btnHeartbeat.Name = "btnHeartbeat";
            btnHeartbeat.Size = new Size(100, 30);
            btnHeartbeat.TabIndex = 3;
            btnHeartbeat.Text = "Heartbeat";
            btnHeartbeat.Click += btnHeartbeat_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(447, 12);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(100, 30);
            btnStop.TabIndex = 4;
            btnStop.Text = "Stop";
            btnStop.Click += btnStop_Click;
            // 
            // btnQuit
            // 
            btnQuit.Location = new Point(553, 12);
            btnQuit.Name = "btnQuit";
            btnQuit.Size = new Size(100, 30);
            btnQuit.TabIndex = 5;
            btnQuit.Text = "Quit";
            btnQuit.Click += btnQuit_Click;
            // 
            // txtLog
            // 
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Location = new Point(12, 355);
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            txtLog.Size = new Size(1303, 183);
            txtLog.TabIndex = 7;
            txtLog.Text = "";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(129, 12);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 30);
            btnConnect.TabIndex = 1;
            btnConnect.Text = "Connect";
            btnConnect.Click += btnConnect_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Location = new Point(3, 55);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1312, 287);
            flowLayoutPanel1.TabIndex = 6;
            // 
            // btnDbManager
            // 
            btnDbManager.Location = new Point(740, 12);
            btnDbManager.Name = "btnDbManager";
            btnDbManager.Size = new Size(104, 30);
            btnDbManager.TabIndex = 6;
            btnDbManager.Text = "Config";
            btnDbManager.UseVisualStyleBackColor = true;
            btnDbManager.Click += btnDbManager_Click;
            // 
            // btnLoadClients
            // 
            btnLoadClients.Location = new Point(14, 12);
            btnLoadClients.Name = "btnLoadClients";
            btnLoadClients.Size = new Size(100, 30);
            btnLoadClients.TabIndex = 0;
            btnLoadClients.Text = "Load Clients";
            btnLoadClients.Click += btnLoadClients_Click;
            // 
            // MainForm
            // 
            ClientSize = new Size(1327, 550);
            Controls.Add(btnLoadClients);
            Controls.Add(btnDbManager);
            Controls.Add(flowLayoutPanel1);
            Controls.Add(btnConnect);
            Controls.Add(btnDownload);
            Controls.Add(btnHeartbeat);
            Controls.Add(btnStop);
            Controls.Add(btnQuit);
            Controls.Add(txtLog);
            Name = "MainForm";
            Text = "DSG Client GUI";
            Load += MainForm_Load;
            ResumeLayout(false);
        }
        private Button btnConnect;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button btnDbManager;
        private Button btnLoadClients;
    }
}
