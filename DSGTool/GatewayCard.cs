using System.Drawing;
using System.Windows.Forms;

namespace DSGTool
{
    public class GatewayCard : UserControl
    {
        private Label lblTitle;
        private Label lblStatus;
        private Label lblTotal;
        private FlowLayoutPanel pnlMessageTypes;

        public string GatewayName { get; private set; }

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
            this.Width = 250;
            this.Height = 150;

            lblTitle = new Label()
            {
                Text = GatewayName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black
            };

            lblStatus = new Label()
            {
                Text = "Disconnected",
                ForeColor = Color.Red,
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            lblTotal = new Label()
            {
                Text = "Total: 0",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Italic)
            };

            pnlMessageTypes = new FlowLayoutPanel()
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                AutoScroll = true
            };

            var layout = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false
            };

            layout.Controls.Add(lblTitle);
            layout.Controls.Add(lblStatus);
            layout.Controls.Add(lblTotal);
            layout.Controls.Add(pnlMessageTypes);

            this.Controls.Add(layout);
        }

        public void UpdateStatus(bool? connected)
        {
            if (connected is null) 
                return;
            lblStatus.Text = (bool)connected ? "Connected" : "Disconnected";
            lblStatus.ForeColor = (bool)connected ? Color.Green : Color.Red;
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
