using DSGClient;
using DSGTool.Data.Models;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DSGTool.Config
{
    public class DSGClientEditForm : Form
    {
        public DSGClientEntity DSGClient { get; private set; }

        private ComboBox cmbGateway;
        private TextBox txtStartSeq, txtEndSeq, txtHeartbeat;

        public DSGClientEditForm(List<Gateway> gateways, DSGClientEntity? client = null)
        {
            DSGClient = client ?? new DSGClientEntity(0, 0, "", "", 0);
            Width = 400; Height = 250; Text = "Edit DSGClient";

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(10) };
            for (int i = 0; i < 4; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "Gateway" }, 0, 0);
            cmbGateway = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var g in gateways) cmbGateway.Items.Add(new { g.Id, g.GatewayName });
            cmbGateway.DisplayMember = "GatewayName"; cmbGateway.ValueMember = "Id";
            layout.Controls.Add(cmbGateway, 1, 0);

            layout.Controls.Add(new Label { Text = "StartingSequenceNumber" }, 0, 1);
            txtStartSeq = new TextBox(); layout.Controls.Add(txtStartSeq, 1, 1);

            layout.Controls.Add(new Label { Text = "EndingSequenceNumber" }, 0, 2);
            txtEndSeq = new TextBox(); layout.Controls.Add(txtEndSeq, 1, 2);

            layout.Controls.Add(new Label { Text = "HeartbeatIntervalSeconds" }, 0, 3);
            txtHeartbeat = new TextBox(); layout.Controls.Add(txtHeartbeat, 1, 3);

            var btnOk = new Button { Text = "OK", Dock = DockStyle.Bottom }; btnOk.Click += BtnOk_Click;
            Controls.Add(layout); Controls.Add(btnOk);

            LoadData();
        }

        private void LoadData()
        {
            if (DSGClient.GatewayId > 0)
            {
                for (int i = 0; i < cmbGateway.Items.Count; i++)
                {
                    dynamic item = cmbGateway.Items[i];
                    if (item.Id == DSGClient.GatewayId) { cmbGateway.SelectedIndex = i; break; }
                }
            }
            txtStartSeq.Text = DSGClient.StartingSequenceNumber;
            txtEndSeq.Text = DSGClient.EndingSequenceNumber;
            txtHeartbeat.Text = DSGClient.HeartbeatIntervalSeconds.ToString();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            DSGClient.GatewayId = Convert.ToInt32(((dynamic)cmbGateway.SelectedItem).Id);
            DSGClient.StartingSequenceNumber = txtStartSeq.Text;
            DSGClient.EndingSequenceNumber = txtEndSeq.Text;
            DSGClient.HeartbeatIntervalSeconds = int.TryParse(txtHeartbeat.Text, out var h) ? h : 0;
            DialogResult = DialogResult.OK;
        }
    }
}
