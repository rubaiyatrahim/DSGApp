using DSGClient;
using DSGTool.Data.Models;
using System;
using System.Windows.Forms;

namespace DSGTool.Config
{
    public class GatewayEditForm : Form
    {
        public Gateway Gateway { get; private set; }

        private TextBox txtPartitionId, txtEnvName, txtGatewayName, txtHost, txtPort, txtUser, txtPassword;

        public GatewayEditForm(Gateway? gateway = null)
        {
            Gateway = gateway ?? new Gateway(null, "", "", "", "", 0, "", "");
            Width = 400; Height = 350; Text = "Edit Gateway";

            BuildUI();
            LoadData();
        }

        private void BuildUI()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 7, Padding = new Padding(10) };
            for (int i = 0; i < 7; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "PartitionId" }, 0, 0);
            txtPartitionId = new TextBox(); layout.Controls.Add(txtPartitionId, 1, 0);

            layout.Controls.Add(new Label { Text = "EnvironmentName" }, 0, 1);
            txtEnvName = new TextBox(); layout.Controls.Add(txtEnvName, 1, 1);

            layout.Controls.Add(new Label { Text = "GatewayName" }, 0, 2);
            txtGatewayName = new TextBox(); layout.Controls.Add(txtGatewayName, 1, 2);

            layout.Controls.Add(new Label { Text = "Host" }, 0, 3);
            txtHost = new TextBox(); layout.Controls.Add(txtHost, 1, 3);

            layout.Controls.Add(new Label { Text = "Port" }, 0, 4);
            txtPort = new TextBox(); layout.Controls.Add(txtPort, 1, 4);

            layout.Controls.Add(new Label { Text = "Username" }, 0, 5);
            txtUser = new TextBox(); layout.Controls.Add(txtUser, 1, 5);

            layout.Controls.Add(new Label { Text = "Password" }, 0, 6);
            txtPassword = new TextBox(); layout.Controls.Add(txtPassword, 1, 6);

            var btnOk = new Button { Text = "OK", Dock = DockStyle.Bottom }; btnOk.Click += BtnOk_Click;
            Controls.Add(layout); Controls.Add(btnOk);
        }

        private void LoadData()
        {
            txtPartitionId.Text = Gateway.PartitionId;
            txtEnvName.Text = Gateway.EnvironmentName;
            txtGatewayName.Text = Gateway.GatewayName;
            txtHost.Text = Gateway.Host;
            txtPort.Text = Gateway.Port.ToString();
            txtUser.Text = Gateway.Username;
            txtPassword.Text = Gateway.Password;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            Gateway.PartitionId = txtPartitionId.Text;
            Gateway.EnvironmentName = txtEnvName.Text;
            Gateway.GatewayName = txtGatewayName.Text;
            Gateway.Host = txtHost.Text;
            Gateway.Port = int.TryParse(txtPort.Text, out var p) ? p : 0;
            Gateway.Username = txtUser.Text;
            Gateway.Password = txtPassword.Text;
            DialogResult = DialogResult.OK;
        }
    }
}
