using DSGClient;
using DSGTool.Data;
using DSGTool.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DSGTool.Config
{
    public class DbCrudManagerForm : Form
    {
        private readonly DbWorks _dbWorks;

        private DataGridView dgvGateways;
        private DataGridView dgvMessageTypes;
        private DataGridView dgvDSGClients;
        private DataGridView dgvGatewayMessageMap;

        public DbCrudManagerForm(string connectionString)
        {
            _dbWorks = new DbWorks(connectionString);
            Width = 1000; Height = 600;
            Text = "Configuration Manager";

            BuildUI();
            LoadAllData();
        }

        private void BuildUI()
        {
            var tabControl = new TabControl { Dock = DockStyle.Fill };

            dgvGateways = BuildGrid(new[] { "Id", "PartitionId", "EnvironmentName", "GatewayName", "HostIp", "Port", "UserName", "Password" });
            dgvMessageTypes = BuildGrid(new[] { "Id", "Name", "MessageId", "IsSecMsg" });
            dgvDSGClients = BuildGrid(new[] { "Id", "GatewayId", "StartingSequenceNumber", "EndingSequenceNumber", "HeartbeatIntervalSeconds" });
            dgvGatewayMessageMap = BuildGrid(new[] { "Id", "GatewayName", "MessageTypeName" });

            tabControl.TabPages.Add(CreateTabPage("Gateways", dgvGateways,
                AddGateway_Click, EditGateway_Click, DeleteGateway_Click));

            tabControl.TabPages.Add(CreateTabPage("Message Types", dgvMessageTypes,
                AddMessageType_Click, EditMessageType_Click, DeleteMessageType_Click));

            tabControl.TabPages.Add(CreateTabPage("DSG Clients", dgvDSGClients,
                AddDSGClient_Click, EditDSGClient_Click, DeleteDSGClient_Click));

            tabControl.TabPages.Add(CreateTabPage("Gateway ↔ MessageType", dgvGatewayMessageMap,
                AddGatewayMessageType_Click, /*EditGatewayMessageType_Click*/null, DeleteGatewayMessageType_Click));

            Controls.Add(tabControl);
        }

        private DataGridView BuildGrid(string[] columns)
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };
            foreach (var col in columns) dgv.Columns.Add(col, col);
            return dgv;
        }

        private TabPage CreateTabPage(string title, DataGridView dgv, EventHandler add, EventHandler edit, EventHandler delete)
        {
            var tab = new TabPage(title);
            var panel = new Panel { Dock = DockStyle.Fill };
            dgv.Dock = DockStyle.Top; dgv.Height = 400;

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, FlowDirection = FlowDirection.LeftToRight };
            if (add != null) { var btnAdd = new Button { Text = "Add", Width = 80 }; btnAdd.Click += add; btnPanel.Controls.Add(btnAdd); }
            if (edit != null) { var btnEdit = new Button { Text = "Edit", Width = 80 }; btnEdit.Click += edit; btnPanel.Controls.Add(btnEdit); }
            if (delete != null) { var btnDelete = new Button { Text = "Delete", Width = 80 }; btnDelete.Click += delete; btnPanel.Controls.Add(btnDelete); }

            panel.Controls.Add(dgv);
            panel.Controls.Add(btnPanel);
            tab.Controls.Add(panel);
            return tab;
        }

        private void LoadAllData()
        {
            LoadGateways();
            LoadMessageTypes();
            LoadDSGClients();
            LoadGatewayMessageMap();
        }

        private void LoadGateways()
        {
            dgvGateways.Rows.Clear();
            foreach (var g in _dbWorks.GetGateways())
                dgvGateways.Rows.Add(g.Id, g.PartitionId, g.EnvironmentName, g.GatewayName, g.Host, g.Port, g.Username, g.Password);
            dgvGateways.Columns[0].Visible = false; // Hide Id column
        }

        private void LoadMessageTypes()
        {
            dgvMessageTypes.Rows.Clear();
            foreach (var m in _dbWorks.GetMessageTypes())
                dgvMessageTypes.Rows.Add(m.Id, m.Name, m.MessageId, m.IsSecMsg);
            dgvMessageTypes.Columns[0].Visible = false; // Hide Id column
        }

        private void LoadDSGClients()
        {
            dgvDSGClients.Rows.Clear();
            foreach (var c in _dbWorks.GetDSGClientEntities())
                dgvDSGClients.Rows.Add(c.Id, c.GatewayId, c.StartingSequenceNumber, c.EndingSequenceNumber, c.HeartbeatIntervalSeconds);
            dgvDSGClients.Columns[0].Visible = false; // Hide Id column
        }

        private void LoadGatewayMessageMap()
        {
            dgvGatewayMessageMap.Rows.Clear();
            var gateways = _dbWorks.GetGateways();
            var messageTypes = _dbWorks.GetMessageTypes();

            foreach (var g in gateways)
            {
                var msgIds = _dbWorks.GetMessageTypeIdsForGateway(g.Id);
                foreach (var id in msgIds)
                {
                    var msg = messageTypes.FirstOrDefault(m => m.Id == id);
                    if (msg != null)
                        dgvGatewayMessageMap.Rows.Add($"{g.Id}_{msg.Id}", g.GatewayName, msg.Name);
                }
            }
        }

        // ===============================
        // Gateway CRUD
        // ===============================
        private void AddGateway_Click(object sender, EventArgs e)
        {
            using var form = new GatewayEditForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                _dbWorks.InsertGateway(form.Gateway);
                LoadGateways();
                LoadGatewayMessageMap();
            }
        }

        private void EditGateway_Click(object sender, EventArgs e)
        {
            if (dgvGateways.SelectedRows.Count == 0) return;
            var row = dgvGateways.SelectedRows[0];
            var g = new Gateway(
                Convert.ToInt32(row.Cells["Id"].Value),
                row.Cells["PartitionId"].Value.ToString(),
                row.Cells["EnvironmentName"].Value.ToString(),
                row.Cells["GatewayName"].Value.ToString(),
                row.Cells["HostIp"].Value.ToString(),
                Convert.ToInt32(row.Cells["Port"].Value),
                row.Cells["UserName"].Value.ToString(),
                row.Cells["Password"].Value.ToString()
            );
            using var form = new GatewayEditForm(g);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _dbWorks.UpdateGateway(form.Gateway);
                LoadGateways();
                LoadGatewayMessageMap();
            }
        }

        private void DeleteGateway_Click(object sender, EventArgs e)
        {
            if (dgvGateways.SelectedRows.Count == 0) return;
            var id = dgvGateways.SelectedRows[0].Cells["Id"].Value.ToString();
            _dbWorks.DeleteGateway(id);
            LoadGateways();
            LoadGatewayMessageMap();
        }

        // ===============================
        // MessageType CRUD
        // ===============================
        private void AddMessageType_Click(object sender, EventArgs e)
        {
            using var form = new MessageTypeEditForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                _dbWorks.InsertMessageType(form.MessageType);
                LoadMessageTypes();
                LoadGatewayMessageMap();
            }
        }

        private void EditMessageType_Click(object sender, EventArgs e)
        {
            if (dgvMessageTypes.SelectedRows.Count == 0) return;
            var row = dgvMessageTypes.SelectedRows[0];
            var m = new MessageType(
                Convert.ToInt32(row.Cells["Id"].Value),
                row.Cells["Name"].Value.ToString(),
                row.Cells["MessageId"].Value.ToString(),
                Convert.ToBoolean(row.Cells["IsSecMsg"].Value)
            );
            using var form = new MessageTypeEditForm(m);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _dbWorks.UpdateMessageType(form.MessageType);
                LoadMessageTypes();
                LoadGatewayMessageMap();
            }
        }

        private void DeleteMessageType_Click(object sender, EventArgs e)
        {
            if (dgvMessageTypes.SelectedRows.Count == 0) return;
            var id = Convert.ToInt32(dgvMessageTypes.SelectedRows[0].Cells["Id"].Value);
            _dbWorks.DeleteMessageType(id);
            LoadMessageTypes();
            LoadGatewayMessageMap();
        }

        // ===============================
        // DSGClient CRUD
        // ===============================
        private void AddDSGClient_Click(object sender, EventArgs e)
        {
            using var form = new DSGClientEditForm(_dbWorks.GetGateways());
            if (form.ShowDialog() == DialogResult.OK)
            {
                _dbWorks.InsertDSGClient(form.DSGClient);
                LoadDSGClients();
            }
        }

        private void EditDSGClient_Click(object sender, EventArgs e)
        {
            if (dgvDSGClients.SelectedRows.Count == 0) return;
            var row = dgvDSGClients.SelectedRows[0];
            var client = new DSGClientEntity(
                Convert.ToInt32(row.Cells["Id"].Value),
                Convert.ToInt32(row.Cells["GatewayId"].Value),
                row.Cells["StartingSequenceNumber"].Value.ToString(),
                row.Cells["EndingSequenceNumber"].Value.ToString(),
                Convert.ToInt32(row.Cells["HeartbeatIntervalSeconds"].Value)
            );
            using var form = new DSGClientEditForm(_dbWorks.GetGateways(), client);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _dbWorks.UpdateDSGClient(form.DSGClient);
                LoadDSGClients();
            }
        }

        private void DeleteDSGClient_Click(object sender, EventArgs e)
        {
            if (dgvDSGClients.SelectedRows.Count == 0) return;
            var id = dgvDSGClients.SelectedRows[0].Cells["Id"].Value.ToString();
            _dbWorks.DeleteDSGClient(id);
            LoadDSGClients();
        }

        // ===============================
        // GatewayMessageType CRUD
        // ===============================
        private void AddGatewayMessageType_Click(object sender, EventArgs e)
        {
            using var form = new GatewayMessageTypeEditForm(_dbWorks.GetGateways(), _dbWorks.GetMessageTypes());
            if (form.ShowDialog() == DialogResult.OK)
            {
                _dbWorks.InsertGatewayMessageType(form.SelectedGatewayId, form.SelectedMessageTypeIds.First());
                LoadGatewayMessageMap();
            }
        }

        private void EditGatewayMessageType_Click(object sender, EventArgs e)
        {
            if (dgvGatewayMessageMap.SelectedRows.Count == 0) return;
            var row = dgvGatewayMessageMap.SelectedRows[0];
            int gatewayId = _dbWorks.GetGateways().FirstOrDefault(g => g.GatewayName == row.Cells["GatewayName"].Value.ToString())?.Id ?? 0;
            int messageTypeId = _dbWorks.GetMessageTypes().FirstOrDefault(m => m.Name == row.Cells["MessageTypeName"].Value.ToString())?.Id ?? 0;

            var existing = new GatewayMessageType { GatewayId = gatewayId, MessageTypeId = messageTypeId };
            using var form = new GatewayMessageTypeEditForm(_dbWorks.GetGateways(), _dbWorks.GetMessageTypes(), existing);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _dbWorks.DeleteGatewayMessageType(existing.GatewayId, existing.MessageTypeId);
                _dbWorks.InsertGatewayMessageType(form.SelectedGatewayId, form.SelectedMessageTypeIds.First());
                LoadGatewayMessageMap();
            }
        }

        private void DeleteGatewayMessageType_Click(object sender, EventArgs e)
        {
            if (dgvGatewayMessageMap.SelectedRows.Count == 0) return;
            var row = dgvGatewayMessageMap.SelectedRows[0];
            int gatewayId = _dbWorks.GetGateways().FirstOrDefault(g => g.GatewayName == row.Cells["GatewayName"].Value.ToString())?.Id ?? 0;
            int messageTypeId = _dbWorks.GetMessageTypes().FirstOrDefault(m => m.Name == row.Cells["MessageTypeName"].Value.ToString())?.Id ?? 0;

            _dbWorks.DeleteGatewayMessageType(gatewayId, messageTypeId);
            LoadGatewayMessageMap();
        }
    }
}
