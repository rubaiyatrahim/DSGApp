using DSGClient;
using DSGTool.Data;
using DSGTool.Data.Models;
using System;
using System.Collections.Generic;
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

            tabControl.TabPages.Add(new TabPage("Gateways") { Controls = { dgvGateways } });
            tabControl.TabPages.Add(new TabPage("Message Types") { Controls = { dgvMessageTypes } });
            tabControl.TabPages.Add(new TabPage("DSG Clients") { Controls = { dgvDSGClients } });
            tabControl.TabPages.Add(new TabPage("Gateway ↔ MessageType") { Controls = { dgvGatewayMessageMap } });

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
                dgvGateways.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value?.ToString()))
                        {
                            var g = new Gateway(0,
                                row.Cells["PartitionId"].Value?.ToString(),
                                row.Cells["EnvironmentName"].Value?.ToString(),
                                row.Cells["GatewayName"].Value?.ToString(),
                                row.Cells["HostIp"].Value?.ToString(),
                                Convert.ToInt32(row.Cells["Port"].Value ?? 0),
                                row.Cells["UserName"].Value?.ToString(),
                                row.Cells["Password"].Value?.ToString()
                            );
                            _dbWorks.InsertGateway(g);
                        }
                        else
                        {
                            var g = new Gateway(
                                Convert.ToInt32(row.Cells["Id"].Value),
                                row.Cells["PartitionId"].Value?.ToString(),
                                row.Cells["EnvironmentName"].Value?.ToString(),
                                row.Cells["GatewayName"].Value?.ToString(),
                                row.Cells["HostIp"].Value?.ToString(),
                                Convert.ToInt32(row.Cells["Port"].Value ?? 0),
                                row.Cells["UserName"].Value?.ToString(),
                                row.Cells["Password"].Value?.ToString()
                            );
                            _dbWorks.UpdateGateway(g);
                        }
                        ReloadAndReselect(dgvGateways, LoadGateways, e.RowIndex);
                        LoadGatewayMessageMap();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving Gateway: {ex.Message}");
                        ReloadAndReselect(dgvGateways, LoadGateways, e.RowIndex);
                    }
                }));
            };

            dgvGateways.UserDeletingRow += (s, e) =>
            {
                if (e.Row.Cells["Id"].Value != null)
                    _dbWorks.DeleteGateway(e.Row.Cells["Id"].Value.ToString());
            };

            // MessageTypes
            dgvMessageTypes.CellEndEdit += (s, e) =>
            {
                var row = dgvMessageTypes.Rows[e.RowIndex];
                if (row.IsNewRow) return;
                dgvMessageTypes.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value?.ToString()))
                        {
                            var m = new MessageType(0,
                                row.Cells["Name"].Value?.ToString(),
                                row.Cells["MessageId"].Value?.ToString(),
                                Convert.ToBoolean(row.Cells["IsSecMsg"].Value ?? false));
                            _dbWorks.InsertMessageType(m);
                        }
                        else
                        {
                            var m = new MessageType(
                                Convert.ToInt32(row.Cells["Id"].Value),
                                row.Cells["Name"].Value?.ToString(),
                                row.Cells["MessageId"].Value?.ToString(),
                                Convert.ToBoolean(row.Cells["IsSecMsg"].Value ?? false)
                            );
                            _dbWorks.UpdateMessageType(m);
                        }
                        ReloadAndReselect(dgvMessageTypes, LoadMessageTypes, e.RowIndex);
                        LoadGatewayMessageMap();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving MessageType: {ex.Message}");
                        ReloadAndReselect(dgvMessageTypes, LoadMessageTypes, e.RowIndex);
                    }
                }));
            };

            dgvMessageTypes.UserDeletingRow += (s, e) =>
            {
                if (e.Row.Cells["Id"].Value != null)
                    _dbWorks.DeleteMessageType(Convert.ToInt32(e.Row.Cells["Id"].Value));
            };

            // DSG Clients
            dgvDSGClients.CellEndEdit += (s, e) =>
            {
                var row = dgvDSGClients.Rows[e.RowIndex];
                if (row.IsNewRow) return;
                dgvDSGClients.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (row.Cells["Id"].Value == null || string.IsNullOrEmpty(row.Cells["Id"].Value?.ToString()))
                        {
                            var c = new DSGClientEntity(0,
                                Convert.ToInt32(row.Cells["GatewayId"].Value ?? 0),
                                row.Cells["StartingSequenceNumber"].Value?.ToString(),
                                row.Cells["EndingSequenceNumber"].Value?.ToString(),
                                Convert.ToInt32(row.Cells["HeartbeatIntervalSeconds"].Value ?? 0)
                            );
                            _dbWorks.InsertDSGClient(c);
                        }
                        else
                        {
                            var c = new DSGClientEntity(
                                Convert.ToInt32(row.Cells["Id"].Value),
                                Convert.ToInt32(row.Cells["GatewayId"].Value ?? 0),
                                row.Cells["StartingSequenceNumber"].Value?.ToString(),
                                row.Cells["EndingSequenceNumber"].Value?.ToString(),
                                Convert.ToInt32(row.Cells["HeartbeatIntervalSeconds"].Value ?? 0)
                            );
                            _dbWorks.UpdateDSGClient(c);
                        }
                        ReloadAndReselect(dgvDSGClients, LoadDSGClients, e.RowIndex);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving DSG Client: {ex.Message}");
                        ReloadAndReselect(dgvDSGClients, LoadDSGClients, e.RowIndex);
                    }
                }));
            };

            dgvDSGClients.UserDeletingRow += (s, e) =>
            {
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
                clb.Tag = gateway.Id; // remember gateway
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
                    dgv.Rows[selectedRowIndex].Selected = true;

                clb.Visible = false;
            };
        }

        private void ReloadAndReselect(DataGridView dgv, Action loader, int rowIndex)
        {
            loader();
            if (rowIndex < dgv.Rows.Count)
                dgv.Rows[rowIndex].Selected = true;
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
            dgvGateways.Columns[0].Visible = false;
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
