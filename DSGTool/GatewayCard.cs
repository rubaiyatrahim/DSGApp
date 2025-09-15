using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DSGTool
{
    public class GatewayCard : UserControl
    {
        private Label lblTitle;
        private Label lblStatus;
        private Label lblTotal;
        private FlowLayoutPanel pnlMessageTypes;
        private Button btnStart;
        private Button btnDownload;
        private Button btnStop;

        public string GatewayName { get; private set; }

        // Events for MainForm to subscribe
        public event Action<string>? StartClicked;
        public event Action<string>? DownloadClicked;
        public event Action<string>? StopClicked;

        public GatewayCard(string gatewayName)
        {
            GatewayName = gatewayName;
            BuildUI();
        }

        private void BuildUI()
        {
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Padding = new Padding(10);
            this.Margin = new Padding(10);
            this.Width = 260;
            this.Height = 180;

            // Main Layout
            var layout = new TableLayoutPanel()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Total
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Message types (expandable)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            lblTitle = new Label()
            {
                Text = GatewayName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black,
                Dock = DockStyle.Fill
            };

            lblStatus = new Label()
            {
                Text = "Disconnected",
                ForeColor = Color.Red,
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Dock = DockStyle.Fill
            };

            lblTotal = new Label()
            {
                Text = "Total: 0",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                Dock = DockStyle.Fill
            };

            pnlMessageTypes = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true
            };

            // Buttons Row
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 35
            };

            btnStart = new Button { Text = "Start", Width = 60 };
            btnDownload = new Button { Text = "Download", Width = 80 };
            btnStop = new Button { Text = "Stop", Width = 60 };

            btnStart.Click += (s, e) => StartClicked?.Invoke(GatewayName);
            btnDownload.Click += (s, e) => DownloadClicked?.Invoke(GatewayName);
            btnStop.Click += (s, e) => StopClicked?.Invoke(GatewayName);

            buttonPanel.Controls.Add(btnStart);
            buttonPanel.Controls.Add(btnDownload);
            buttonPanel.Controls.Add(btnStop);

            // Add everything to layout
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblStatus, 0, 1);
            layout.Controls.Add(lblTotal, 0, 2);
            layout.Controls.Add(pnlMessageTypes, 0, 3);
            layout.Controls.Add(buttonPanel, 0, 4);

            this.Controls.Add(layout);
        }

        public void UpdateStatus(bool? connected)
        {
            if (connected is null) return;
            lblStatus.Text = connected.Value ? "Connected" : "Disconnected";
            lblStatus.ForeColor = connected.Value ? Color.Green : Color.Red;
        }

        public void UpdateTotalCount(int total)
        {
            lblTotal.Text = $"Total: {total}";
        }

        public void UpdateMessageTypeCount(string msgType, int count)
        {
            // Find label if exists, otherwise create one
            var existing = pnlMessageTypes.Controls
                .OfType<Label>()
                .FirstOrDefault(l => l.Tag?.ToString() == msgType);

            if (existing != null)
            {
                existing.Text = $"{msgType}: {count}";
            }
            else
            {
                var lbl = new Label()
                {
                    Text = $"{msgType}: {count}",
                    AutoSize = true,
                    Tag = msgType
                };
                pnlMessageTypes.Controls.Add(lbl);
            }
        }

        public void ResetCounts()
        {
            lblTotal.Text = "Total: 0";
            pnlMessageTypes.Controls.Clear();
        }
    }
}
