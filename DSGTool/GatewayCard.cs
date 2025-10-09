using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DSGTool
{
    public class GatewayCard : UserControl
    {
        private Label lblTitle;
        private Label lblEnvironment;
        private Label lblStatus;
        private Label lblHost;
        private Label lblTotal;
        private Label lblRange;
        private Label lblTotalExceptHB;
        private TableLayoutPanel tblMessageCounts;
        private TableLayoutPanel pnlHeader;
        private Panel pnlTableContainer;
        private Button btnStart;
        private Button btnDownload;
        private Button btnStop;
        private Button btnDelete;

        public string GatewayName { get; private set; }

        public event Action<string>? StartClicked;
        public event Action<string>? DownloadClicked;
        public event Action<string>? StopClicked;
        public event Func<string, Task>? DeleteClickedAsync;

        public GatewayCard(string gatewayName)
        {
            GatewayName = gatewayName;
            BuildUI();
        }

        public void SetEnvironmentLabels(string environment, string hostip, string port, string startingSeqNumber, string endingSeqNumber)
        {
            lblEnvironment.Text = environment;
            lblHost.Text = "@ " + hostip + " : " + port;
            lblRange.Text = "Requested SN: " + startingSeqNumber + ".." + endingSeqNumber;
        }

        private void BuildUI()
        {
            BackColor = Color.White;
            BorderStyle = BorderStyle.FixedSingle;
            Padding = new Padding(10);
            Margin = new Padding(10);
            Width = 360;
            Height = 280;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                AutoSize = false
            };

            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Title
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Total
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Total except HB
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Table container
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

            // --- Labels ---
            lblTitle = new Label
            {
                Text = GatewayName,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.Black,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblEnvironment = new Label
            {
                Text = "<Environment>",
                ForeColor = Color.Black,
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.BottomRight
            };

            var panTitle = new Panel { Width = 330, Height = 20 };
            panTitle.Controls.Add(lblTitle);
            panTitle.Controls.Add(lblEnvironment);

            lblStatus = new Label
            {
                Text = "Disconnected",
                ForeColor = Color.Red,
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblHost = new Label
            {
                Text = "<Host>",
                ForeColor = Color.Gray,
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.BottomRight
            };

            var panStatus = new Panel { Width = 330, Height = 15 };
            panStatus.Controls.Add(lblStatus);
            panStatus.Controls.Add(lblHost);

            lblTotal = new Label
            {
                Text = "Total: 0",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblRange = new Label
            {
                Text = "<Range>",
                ForeColor = Color.Gray,
                AutoSize = true,
                Font = new Font("Segoe UI", 8),
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.BottomRight
            };
            var panRange = new Panel { Width = 330, Height = 15 };
            panRange.Controls.Add(lblTotal);
            panRange.Controls.Add(lblRange);

            lblTotalExceptHB = new Label
            {
                Text = "Total except HB: 0",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Italic)
            };
            var panCountExceptHB = new Panel { Width = 330, Height = 25 };
            panCountExceptHB.Controls.Add(lblTotalExceptHB);

            // --- Header Table (sticky) ---
            pnlHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                Height = 30,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            pnlHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            pnlHeader.Controls.Add(CreateHeaderLabel("Message Id"), 0, 0);
            pnlHeader.Controls.Add(CreateHeaderLabel("LSN"), 1, 0);
            pnlHeader.Controls.Add(CreateHeaderLabel("Received"), 2, 0);
            pnlHeader.Controls.Add(CreateHeaderLabel("DB Count"), 3, 0);

            // --- Scrollable data table ---
            tblMessageCounts = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            tblMessageCounts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tblMessageCounts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tblMessageCounts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tblMessageCounts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            tblMessageCounts.EnableDoubleBuffering(true);

            // Add new row
            AddRowInTable("0", "0", 0, 0);

            pnlTableContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                //BorderStyle = BorderStyle.FixedSingle
            };
            pnlTableContainer.Controls.Add(tblMessageCounts);
            pnlTableContainer.EnableDoubleBuffering(true);

            // --- Combine header + scrollable data ---
            var pnlFullTable = new Panel { Dock = DockStyle.Fill };
            pnlFullTable.Controls.Add(pnlTableContainer);
            pnlFullTable.Controls.Add(pnlHeader); // sticky header

            // --- Buttons ---
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 35
            };

            btnStart = new Button { Text = "Start", Width = 60 };
            btnDownload = new Button { Text = "Download", Width = 80, Enabled = false };
            btnStop = new Button { Text = "Stop", Width = 60, Enabled = false };

            btnStart.Click += (s, e) => StartClicked?.Invoke(GatewayName);
            btnDownload.Click += (s, e) => DownloadClicked?.Invoke(GatewayName);
            btnStop.Click += (s, e) => StopClicked?.Invoke(GatewayName);

            btnDelete = new Button
            {
                Text = "Delete",
                Width = 70,
                BackColor = Color.FromArgb(255, 240, 240),
                ForeColor = Color.DarkRed
            };
            btnDelete.Click += async (s, e) => await HandleDeleteClickAsync();

            buttonPanel.Controls.AddRange(new Control[] { btnStart, btnDownload, btnStop, btnDelete });

            // --- Add panels of controls to main layout ---
            layout.Controls.Add(panTitle, 0, 0);
            layout.Controls.Add(panStatus, 0, 1);
            layout.Controls.Add(panRange, 0, 2);
            layout.Controls.Add(panCountExceptHB, 0, 3);
            layout.Controls.Add(pnlFullTable, 0, 4);
            layout.Controls.Add(buttonPanel, 0, 5);

            Controls.Add(layout);
        }

        private void AddRowInTable(string msgType, string lsn, int receivedCount, long dbCount)
        {
            int newRowIndex = tblMessageCounts.RowCount;
            tblMessageCounts.RowCount++;
            tblMessageCounts.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            var lblType = CreateCellLabel(msgType);
            var lblLsn = CreateCellLabel(lsn.ToString());
            var lblRecv = CreateCellLabel(receivedCount.ToString());
            var lblDb = CreateCellLabel(dbCount.ToString());
            if (msgType != "0")
                HighlightMismatch(lblRecv, lblDb);
            tblMessageCounts.Controls.Add(lblType, 0, newRowIndex);
            tblMessageCounts.Controls.Add(lblLsn, 1, newRowIndex);
            tblMessageCounts.Controls.Add(lblRecv, 2, newRowIndex);
            tblMessageCounts.Controls.Add(lblDb, 3, newRowIndex);
        }

        // --- Create header & data cells ---
        private Label CreateHeaderLabel(string text) =>
            new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = false,
                Height = 30,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Gainsboro,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

        private Label CreateCellLabel(string text) =>
            new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Height = 25,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

        // --- Delete handling ---
        private async Task HandleDeleteClickAsync()
        {
            if (DeleteClickedAsync == null) return;

            if (MessageBox.Show(
                $"Are you sure you want to delete all messages for '{GatewayName}'?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            btnDelete.Enabled = false;
            btnDelete.ForeColor = Color.Gray;

            try
            {
                await DeleteClickedAsync.Invoke(GatewayName);
                MessageBox.Show($"Messages for '{GatewayName}' deleted successfully.",
                    "Delete Completed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting messages for {GatewayName}:\n{ex.Message}",
                    "Delete Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnDelete.Enabled = true;
                btnDelete.ForeColor = Color.DarkRed;
            }
        }

        // Call this when you want to notify subscribers
        public async Task RaiseDeleteAsync()
        {
            if (DeleteClickedAsync != null)
            {
                foreach (var handler in DeleteClickedAsync.GetInvocationList().Cast<Func<string, Task>>())
                {
                    await handler.Invoke(GatewayName);
                }
            }
        }

        // --- Status & totals ---
        public void UpdateStatus(bool? connected)
        {
            if (connected == true)
            {
                lblStatus.Text = "Connected";
                lblStatus.ForeColor = Color.Green;
                btnStart.Enabled = false;
                btnDownload.Enabled = true;
                btnStop.Enabled = true;
            }
            else
            {
                lblStatus.Text = "Disconnected";
                lblStatus.ForeColor = Color.Red;
                btnStart.Enabled = true;
                btnDownload.Enabled = false;
                btnStop.Enabled = false;
            }
        }

        public void UpdateTotalCount(int total) =>
            lblTotal.Text = $"Total: {total}";

        public void UpdateTotalExceptHBCount(int totalExceptHB) =>
            lblTotalExceptHB.Text = $"Total except HB: {totalExceptHB}";

        // --- Update table rows ---
        public void UpdateMessageTypeCount(string msgType, string lsn, int count) =>
            UpdateOrCreateRow(msgType, lsn: lsn, receivedCount: count);

        public void UpdateMessageTypeCountDB(string msgType, long count) =>
            UpdateOrCreateRow(msgType, dbCount: count);

        private void UpdateOrCreateRow(string msgType, string? lsn = null, int? receivedCount = null, long? dbCount = null)
        {
            tblMessageCounts.SuspendLayout();

            try
            {
                for (int row = 0; row < tblMessageCounts.RowCount; row++)
                {
                    var typeLabel = tblMessageCounts.GetControlFromPosition(0, row) as Label;
                    if (typeLabel != null && typeLabel.Text == msgType)
                    {
                        var lsnLabel = tblMessageCounts.GetControlFromPosition(1, row) as Label;
                        var recvLabel = tblMessageCounts.GetControlFromPosition(2, row) as Label;
                        var dbLabel = tblMessageCounts.GetControlFromPosition(3, row) as Label;

                        if (lsn != null) lsnLabel!.Text = lsn;
                        if (receivedCount.HasValue) recvLabel!.Text = receivedCount.Value.ToString();
                        if (dbCount.HasValue) dbLabel!.Text = dbCount.Value.ToString();

                        if (msgType != "0")
                            HighlightMismatch(recvLabel, dbLabel);
                        return;
                    }
                }

                // Add new row
                AddRowInTable(msgType, lsn ?? "0", receivedCount ?? 0, dbCount ?? 0);
            }
            finally
            {
                tblMessageCounts.ResumeLayout();
                tblMessageCounts.Invalidate();
                tblMessageCounts.Update();
            }
        }

        private void HighlightMismatch(Label recvLabel, Label dbLabel)
        {
            if (int.TryParse(recvLabel.Text, out int r) && long.TryParse(dbLabel.Text, out long d) && r != d)
            {
                dbLabel.ForeColor = Color.Red;
            }
            else
            {
                dbLabel.ForeColor = Color.Green;
            }
        }

        public void ResetCounts()
        {
            lblTotal.Text = "Total: 0";
            lblTotalExceptHB.Text = "Total except HB: 0";

            tblMessageCounts.Controls.Clear();
            tblMessageCounts.RowCount = 0;

            AddRowInTable("0", "0", 0, 0);
        }
    }

    // --- Extensions ---
    public static class ControlExtensions
    {
        public static void EnableDoubleBuffering(this Control c, bool enable)
        {
            var prop = c.GetType().GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (prop != null)
                prop.SetValue(c, enable, null);
        }
    }
}
