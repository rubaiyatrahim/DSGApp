using DSGClient;
using DSGTool.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DSGTool.Config
{
    public class GatewayMessageTypeEditForm : Form
    {
        public int SelectedGatewayId { get; private set; }
        public List<int> SelectedMessageTypeIds { get; private set; } = new List<int>();

        private ComboBox cmbGateway;
        private CheckedListBox clbMessageTypes;

        public GatewayMessageTypeEditForm(List<Gateway> gateways, List<MessageType> messageTypes, GatewayMessageType? existing = null)
        {
            Width = 400; Height = 400; Text = "Assign MessageTypes to Gateway";

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            layout.Controls.Add(new Label { Text = "Gateway" }, 0, 0);
            cmbGateway = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var g in gateways) cmbGateway.Items.Add(new { g.Id, g.GatewayName });
            cmbGateway.DisplayMember = "GatewayName"; cmbGateway.ValueMember = "Id";
            layout.Controls.Add(cmbGateway, 1, 0);

            clbMessageTypes = new CheckedListBox { Dock = DockStyle.Fill };
            foreach (var m in messageTypes) clbMessageTypes.Items.Add(new { m.Id, m.Name }, false);
            clbMessageTypes.DisplayMember = "Name"; clbMessageTypes.ValueMember = "Id";
            layout.Controls.Add(clbMessageTypes, 0, 1); layout.SetColumnSpan(clbMessageTypes, 2);

            var btnOk = new Button { Text = "OK", Dock = DockStyle.Bottom }; btnOk.Click += BtnOk_Click;
            Controls.Add(layout); Controls.Add(btnOk);

            if (existing != null)
            {
                cmbGateway.SelectedValue = existing.GatewayId;
                for (int i = 0; i < clbMessageTypes.Items.Count; i++)
                {
                    dynamic item = clbMessageTypes.Items[i];
                    if (item.Id == existing.MessageTypeId)
                        clbMessageTypes.SetItemChecked(i, true);
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (cmbGateway.SelectedItem == null) { DialogResult = DialogResult.Cancel; return; }
            SelectedGatewayId = Convert.ToInt32(((dynamic)cmbGateway.SelectedItem).Id);
            SelectedMessageTypeIds.Clear();
            foreach (var item in clbMessageTypes.CheckedItems)
                SelectedMessageTypeIds.Add(Convert.ToInt32(((dynamic)item).Id));
            DialogResult = DialogResult.OK;
        }
    }
}
