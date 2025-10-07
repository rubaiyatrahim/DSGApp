using DSGClient;
using DSGTool.Data.Models;
using System;
using System.Windows.Forms;

namespace DSGTool.Config
{
    public class MessageTypeEditForm : Form
    {
        public MessageType MessageType { get; private set; }
        private TextBox txtName, txtMessageId;
        private CheckBox chkIsSecMsg;

        public MessageTypeEditForm(MessageType? messageType = null)
        {
            MessageType = messageType ?? new MessageType(null, "", "", false);
            Width = 300; Height = 200; Text = "Edit MessageType";

            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "Name" }, 0, 0);
            txtName = new TextBox(); layout.Controls.Add(txtName, 1, 0);

            layout.Controls.Add(new Label { Text = "MessageId" }, 0, 1);
            txtMessageId = new TextBox(); layout.Controls.Add(txtMessageId, 1, 1);

            layout.Controls.Add(new Label { Text = "IsSecMsg" }, 0, 2);
            chkIsSecMsg = new CheckBox(); layout.Controls.Add(chkIsSecMsg, 1, 2);

            var btnOk = new Button { Text = "OK", Dock = DockStyle.Bottom }; btnOk.Click += BtnOk_Click;
            Controls.Add(layout); Controls.Add(btnOk);
        }

        private void LoadData()
        {
            txtName.Text = MessageType.Name;
            txtMessageId.Text = MessageType.MessageId;
            chkIsSecMsg.Checked = MessageType.IsSecMsg;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            MessageType.Name = txtName.Text;
            MessageType.MessageId = txtMessageId.Text;
            MessageType.IsSecMsg = chkIsSecMsg.Checked;
            DialogResult = DialogResult.OK;
        }
    }
}
