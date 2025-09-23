using DSGClient;
using DSGTool.Data;
using DSGTool.Data.Models;
using System;
using System.Linq;
using System.Windows.Forms;

namespace DSGTool.Config
{
    public class ConfigurationManager : Form
    {
        private readonly DbWorks _dbWorks;

        private DataGridView dgvGateways;
        private DataGridView dgvMessageTypes;
        private DataGridView dgvDSGClients;
        private DataGridView dgvGatewayMessageMap;

        public ConfigurationManager(string connectionString)
        {
            _dbWorks = new DbWorks(connectionString);
            Width = 1000; Height = 600;
            Text = "Configuration Manager";

            BuildUI();
            LoadAllData();
            EnableInlineCrud();
            EnableGatewayMessageMapInlineCheckedListBox(dgvGatewayMessageMap);
        }

        private void BuildUI()
        {
            var tabControl = new TabControl { Dock = DockStyle.Fill };

            dgvGateways = BuildGrid(new[] { "Id", "PartitionId", "EnvironmentName", "GatewayName", "HostIp", "Port", "UserName", "Password" });
            dgvMessageTypes = BuildGrid(new[] { "Id", "Name", "MessageId", "IsSecMsg" });
            dgvDSGClients = BuildGrid(new[] { "Id", "GatewayId", "StartingSequenceNumber", "EndingSequenceNumber", "HeartbeatIntervalSeconds" });
            dgvGatewayMessageMap = BuildGrid(new[] { "Id", "GatewayName", "MessageTypeName" });

            // Replace GatewayId text column with dropdown list column
            int gwIndex = dgvDSGClients.Columns["GatewayId"].Index;
            dgvDSGClients.Columns.RemoveAt(gwIndex);
            var comboCol = new DataGridViewComboBoxColumn
            {
                Name = "GatewayId",
                HeaderText = "Gateway",
                DisplayMember = "GatewayName",
                ValueMember = "Id",
                DataSource = _dbWorks.GetGateways().ToList(),
                FlatStyle = FlatStyle.Flat
            };
            dgvDSGClients.Columns.Insert(gwIndex, comboCol);

            int gmGwIndex = dgvGatewayMessageMap.Columns["GatewayName"].Index;
            dgvGatewayMessageMap.Columns.RemoveAt(gmGwIndex);
            var gmComboCol = new DataGridViewComboBoxColumn
            {
                Name = "GatewayName",
                HeaderText = "Gateway",
                DisplayMember = "GatewayName",
                ValueMember = "GatewayName",
                DataSource = _dbWorks.GetGateways().Select(g => new { g.GatewayName }).ToList(),
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            };
            dgvGatewayMessageMap.Columns.Insert(gmGwIndex, gmComboCol);

            tabControl.TabPages.Add(new TabPage("Gateways") { Controls = { dgvGateways } });
            tabControl.TabPages.Add(new TabPage("Message Types") { Controls = { dgvMessageTypes } });
            tabControl.TabPages.Add(new TabPage("DSG Clients") { Controls = { dgvDSGClients } });
            tabControl.TabPages.Add(new TabPage("Gateway - MessageType") { Controls = { dgvGatewayMessageMap } });

            Controls.Add(tabControl);
        }

        private DataGridView BuildGrid(string[] columns)
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            };
            foreach (var col in columns) dgv.Columns.Add(col, col);
            return dgv;
        }

        private void EnableInlineCrud()
        {
            // Gateways
            dgvGateways.CellEndEdit += (s, e) =>
            {
                var row = dgvGateways.Rows[e.RowIndex];
                if (row.IsNewRow) return;

                dgvGateways.EndEdit();
                dgvGateways.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        int id = 0;
                        if (row.Cells["Id"].Value != null && int.TryParse(row.Cells["Id"].Value.ToString(), out var parsedId))
                            id = parsedId;
                        bool isNew = id == 0;

                        var g = new Gateway(
                            isNew ? null : id,
                            row.Cells["PartitionId"].Value?.ToString() ?? string.Empty,
                            row.Cells["EnvironmentName"].Value?.ToString() ?? string.Empty,
                            row.Cells["GatewayName"].Value?.ToString() ?? string.Empty,
                            row.Cells["HostIp"].Value?.ToString() ?? string.Empty,
                            Convert.ToInt32(row.Cells["Port"].Value ?? 0),
                            row.Cells["UserName"].Value?.ToString() ?? string.Empty,
                            row.Cells["Password"].Value?.ToString() ?? string.Empty
                        );

                        if (isNew)
                            g.Id = _dbWorks.InsertGateway(g); // assume Insert returns new Id
                        else
                            _dbWorks.UpdateGateway(g);

                        ReloadAndSelect(dgvGateways, LoadGateways, e.RowIndex);
                        LoadGatewayMessageMap();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving Gateway: {ex.Message}");
                        ReloadAndSelect(dgvGateways, LoadGateways, e.RowIndex);
                    }
                }));
            };

            dgvGateways.UserDeletingRow += (s, e) =>
            {
                if (!ConfirmDelete()) { e.Cancel = true; return; }
                if (e.Row.Cells["Id"].Value != null)
                    _dbWorks.DeleteGateway(e.Row.Cells["Id"].Value.ToString());
            };

            // MessageTypes
            dgvMessageTypes.CellEndEdit += (s, e) =>
            {
                var row = dgvMessageTypes.Rows[e.RowIndex];
                if (row.IsNewRow) return;

                dgvMessageTypes.EndEdit();
                dgvMessageTypes.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        int id = 0;
                        if (row.Cells["Id"].Value != null && int.TryParse(row.Cells["Id"].Value.ToString(), out var parsedId))
                            id = parsedId;
                        bool isNew = id == 0;

                        var m = new MessageType(
                            isNew ? null : id,
                            row.Cells["Name"].Value?.ToString() ?? string.Empty,
                            row.Cells["MessageId"].Value?.ToString() ?? string.Empty,
                            Convert.ToBoolean(row.Cells["IsSecMsg"].Value ?? false)
                        );

                        if (isNew)
                            m.Id = _dbWorks.InsertMessageType(m);
                        else
                            _dbWorks.UpdateMessageType(m);

                        ReloadAndSelect(dgvMessageTypes, LoadMessageTypes, e.RowIndex);
                        LoadGatewayMessageMap();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving MessageType: {ex.Message}");
                        ReloadAndSelect(dgvMessageTypes, LoadMessageTypes, e.RowIndex);
                    }
                }));
            };

            dgvMessageTypes.UserDeletingRow += (s, e) =>
            {
                if (!ConfirmDelete()) { e.Cancel = true; return; }
                if (e.Row.Cells["Id"].Value != null)
                    _dbWorks.DeleteMessageType(Convert.ToInt32(e.Row.Cells["Id"].Value));
            };

            // DSG Clients
            dgvDSGClients.CellEndEdit += (s, e) =>
            {
                var row = dgvDSGClients.Rows[e.RowIndex];
                if (row.IsNewRow) return;

                dgvDSGClients.EndEdit();
                dgvDSGClients.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        int id = 0;
                        if (row.Cells["Id"].Value != null && int.TryParse(row.Cells["Id"].Value.ToString(), out var parsedId))
                            id = parsedId;
                        bool isNew = id == 0;

                        int gatewayId = 0;
                        if (row.Cells["GatewayId"].Value != null && int.TryParse(row.Cells["GatewayId"].Value.ToString(), out var gwId))
                            gatewayId = gwId;

                        var c = new DSGClientEntity(
                            isNew ? null : id,
                            gatewayId,
                            row.Cells["StartingSequenceNumber"].Value?.ToString() ?? string.Empty,
                            row.Cells["EndingSequenceNumber"].Value?.ToString() ?? string.Empty,
                            Convert.ToInt32(row.Cells["HeartbeatIntervalSeconds"].Value ?? 0)
                        );

                        if (isNew)
                            c.Id = _dbWorks.InsertDSGClient(c);
                        else
                            _dbWorks.UpdateDSGClient(c);

                        ReloadAndSelect(dgvDSGClients, LoadDSGClients, e.RowIndex);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving DSG Client: {ex.Message}");
                        ReloadAndSelect(dgvDSGClients, LoadDSGClients, e.RowIndex);
                    }
                }));
            };

            dgvDSGClients.UserDeletingRow += (s, e) =>
            {
                if (!ConfirmDelete()) { e.Cancel = true; return; }
                if (e.Row.Cells["Id"].Value != null)
                    _dbWorks.DeleteDSGClient(e.Row.Cells["Id"].Value.ToString());
            };
        }

        private void EnableGatewayMessageMapInlineCheckedListBox(DataGridView dgv)
        {
            CheckedListBox clb = new CheckedListBox { Visible = false, CheckOnClick = true };
            dgv.Controls.Add(clb);

            dgv.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex != dgv.Columns["MessageTypeName"].Index) return;

                var row = dgv.Rows[e.RowIndex];
                string gatewayName = row.Cells["GatewayName"].Value?.ToString();
                var gateway = _dbWorks.GetGateways().FirstOrDefault(g => g.GatewayName == gatewayName);
                if (gateway == null) return;

                var allMessageTypes = _dbWorks.GetMessageTypes();
                var currentIds = _dbWorks.GetMessageTypeIdsForGateway(gateway.Id);

                clb.Items.Clear();
                foreach (var mt in allMessageTypes)
                    clb.Items.Add(mt.Name, currentIds.Contains(mt.Id));

                var cellRect = dgv.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                clb.SetBounds(cellRect.X, cellRect.Y, cellRect.Width, cellRect.Height * 5);
                clb.Visible = true;
                clb.BringToFront();
                clb.Tag = gateway.Id;
                clb.Focus();
            };

            clb.LostFocus += (s, e) =>
            {
                if (!(clb.Tag is int gatewayId)) return;

                var allMessageTypes = _dbWorks.GetMessageTypes();
                var selectedNames = clb.CheckedItems.Cast<string>().ToList();
                var selectedIds = allMessageTypes.Where(m => selectedNames.Contains(m.Name)).Select(m => m.Id).ToList();

                var existingIds = _dbWorks.GetMessageTypeIdsForGateway(gatewayId);
                foreach (var id in existingIds) _dbWorks.DeleteGatewayMessageType(gatewayId, id);
                foreach (var id in selectedIds) _dbWorks.InsertGatewayMessageType(gatewayId, id);

                int selectedRowIndex = dgv.CurrentCell.RowIndex;
                LoadGatewayMessageMap();
                if (selectedRowIndex < dgv.Rows.Count)
                {
                    dgv.Rows[selectedRowIndex].Selected = true;
                    dgv.CurrentCell = dgv.Rows[selectedRowIndex].Cells[1];
                }

                clb.Visible = false;
            };
        }

        private bool ConfirmDelete()
        {
            return MessageBox.Show("Are you sure you want to delete this record?",
                                   "Confirm Delete",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void ReloadAndSelect(DataGridView dgv, Action loader, int rowIndex)
        {
            loader();

            dgv.Rows[rowIndex].Selected = true;
            dgv.CurrentCell = dgv.Rows[rowIndex].Cells[1];
            dgv.FirstDisplayedScrollingRowIndex = rowIndex;
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
            var gateways = _dbWorks.GetGateways().ToList();

            foreach (var g in gateways)
                dgvGateways.Rows.Add(g.Id, g.PartitionId, g.EnvironmentName, g.GatewayName, g.Host, g.Port, g.Username, g.Password);
            dgvGateways.Columns[0].Visible = false;

            if (dgvDSGClients.Columns["GatewayId"] is DataGridViewComboBoxColumn comboCol)
                comboCol.DataSource = gateways;
        }

        private void LoadMessageTypes()
        {
            dgvMessageTypes.Rows.Clear();
            foreach (var m in _dbWorks.GetMessageTypes())
                dgvMessageTypes.Rows.Add(m.Id, m.Name, m.MessageId, m.IsSecMsg);
            dgvMessageTypes.Columns[0].Visible = false;
        }

        private void LoadDSGClients()
        {
            dgvDSGClients.Rows.Clear();
            foreach (var c in _dbWorks.GetDSGClientEntities())
                dgvDSGClients.Rows.Add(c.Id, c.GatewayId, c.StartingSequenceNumber, c.EndingSequenceNumber, c.HeartbeatIntervalSeconds);
            dgvDSGClients.Columns[0].Visible = false;
        }

        private void LoadGatewayMessageMap()
        {
            dgvGatewayMessageMap.Rows.Clear();
            var gateways = _dbWorks.GetGateways();
            var messageTypes = _dbWorks.GetMessageTypes();

            foreach (var g in gateways)
            {
                var msgIds = _dbWorks.GetMessageTypeIdsForGateway(g.Id);
                var names = msgIds.Select(id => messageTypes.FirstOrDefault(m => m.Id == id)?.Name)
                                  .Where(n => n != null)
                                  .ToList();
                dgvGatewayMessageMap.Rows.Add($"{g.Id}", g.GatewayName, string.Join(", ", names));
            }
            dgvGatewayMessageMap.Columns[0].Visible = false;
        }
    }
}
