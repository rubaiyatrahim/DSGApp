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
            SuspendLayout();
            // 
            // btnDownload
            // 
            btnDownload.Location = new Point(120, 12);
            btnDownload.Name = "btnDownload";
            btnDownload.Size = new Size(100, 30);
            btnDownload.TabIndex = 1;
            btnDownload.Text = "Download";
            btnDownload.Click += btnDownload_Click;
            // 
            // btnHeartbeat
            // 
            btnHeartbeat.Location = new Point(226, 12);
            btnHeartbeat.Name = "btnHeartbeat";
            btnHeartbeat.Size = new Size(100, 30);
            btnHeartbeat.TabIndex = 2;
            btnHeartbeat.Text = "Heartbeat";
            btnHeartbeat.Click += btnHeartbeat_Click;
            // 
            // btnStop
            // 
            btnStop.Location = new Point(332, 12);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(100, 30);
            btnStop.TabIndex = 3;
            btnStop.Text = "Stop";
            btnStop.Click += btnStop_Click;
            // 
            // btnQuit
            // 
            btnQuit.Location = new Point(438, 12);
            btnQuit.Name = "btnQuit";
            btnQuit.Size = new Size(100, 30);
            btnQuit.TabIndex = 4;
            btnQuit.Text = "Quit";
            btnQuit.Click += btnQuit_Click;
            // 
            // txtLog
            // 
            txtLog.Font = new Font("Consolas", 9F);
            txtLog.Location = new Point(12, 50);
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            txtLog.Size = new Size(1303, 488);
            txtLog.TabIndex = 5;
            txtLog.Text = "";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(14, 12);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(100, 30);
            btnConnect.TabIndex = 0;
            btnConnect.Text = "Connect";
            btnConnect.Click += btnConnect_Click;
            // 
            // MainForm
            // 
            ClientSize = new Size(1327, 550);
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
    }
}
