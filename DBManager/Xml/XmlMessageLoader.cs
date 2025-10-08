using Microsoft.Data.SqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DBManager.Xml
{
    /// <summary>
    /// Handles asynchronous XML-to-database insertion and table management.
    /// </summary>
    public sealed class XmlMessageLoader : IDisposable
    {
        private sealed class XmlMessage
        {
            public required string Xml { get; init; }
            public required string GatewayName { get; init; }
            public int MessageId { get; init; }
            public long SequenceNumber { get; init; }
        }

        private readonly string _connectionString;
        private readonly BlockingCollection<XmlMessage> _queue = new(new ConcurrentQueue<XmlMessage>());
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _worker;
        private volatile bool _disposed;
        private readonly string _fallbackFile = "XmlMessageLoader_Failed.log";
        private readonly int _batchSize;
        private readonly ConcurrentDictionary<string, HashSet<string>> _tableColumnsCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Event fired after successful insert with message count.
        /// </summary>
        public event Action<string, string, string, long>? MessageInserted;

        public XmlMessageLoader(string connectionString, int batchSize = 100)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _batchSize = Math.Max(1, batchSize);
            _worker = Task.Run(ProcessQueueAsync, _cts.Token);
        }

        public void EnqueueXml(string xml, string gatewayName, int messageId, long sequenceNumber)
        {
            if (_disposed)
            {
                LogFallback($"DROPPED (disposed)", gatewayName, messageId, sequenceNumber, xml);
                return;
            }

            try
            {
                _queue.Add(new XmlMessage
                {
                    Xml = xml,
                    GatewayName = gatewayName,
                    MessageId = messageId,
                    SequenceNumber = sequenceNumber
                }, _cts.Token);
            }
            catch (Exception ex)
            {
                LogFallback($"Exception: {ex.Message}", gatewayName, messageId, sequenceNumber, xml);
            }
        }

        private async Task ProcessQueueAsync()
        {
            var buffer = new List<XmlMessage>(_batchSize);

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    XmlMessage? msg = null;
                    try
                    {
                        msg = _queue.Take(_cts.Token);
                        buffer.Add(msg);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (buffer.Count >= _batchSize || _queue.Count == 0)
                    {
                        var batch = buffer.ToArray();
                        buffer.Clear();

                        try
                        {
                            await InsertBatchAsync(batch).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            foreach (var m in batch)
                                LogFallback($"BATCH ERROR: {ex.Message}", m.GatewayName, m.MessageId, m.SequenceNumber, m.Xml);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }

            if (buffer.Count > 0)
                await InsertBatchAsync(buffer).ConfigureAwait(false);
        }

        private async Task InsertBatchAsync(IEnumerable<XmlMessage> messages)
        {
            var groups = messages.GroupBy(m =>
            {
                var doc = XDocument.Parse(m.Xml, LoadOptions.None);
                var root = doc.Root ?? throw new InvalidOperationException("Missing root element");
                return root.Attribute("name")?.Value ?? throw new InvalidOperationException("Missing 'name' attribute");
            });

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var group in groups)
            {
                var tableName = group.Key;

                if (!_tableColumnsCache.ContainsKey(tableName))
                {
                    await EnsureTableAsync(conn, tableName);
                    _tableColumnsCache.TryAdd(tableName, await GetExistingColumnsAsync(conn, tableName));
                }

                var knownColumns = _tableColumnsCache[tableName];
                var dataTable = CreateDataTable(knownColumns);

                foreach (var xmlMsg in group)
                {
                    var doc = XDocument.Parse(xmlMsg.Xml, LoadOptions.None);
                    var root = doc.Root!;
                    var fields = root.Elements("field")
                                     .Select(f => (Name: f.Attribute("name")?.Value, Value: f.Value))
                                     .Where(f => !string.IsNullOrEmpty(f.Name))
                                     .ToDictionary(f => f.Name!, f => f.Value);

                    var newColumns = fields.Keys.Where(k => !knownColumns.Contains(k)).ToList();
                    if (newColumns.Count > 0)
                    {
                        foreach (var c in newColumns)
                            await AddColumnAsync(conn, tableName, c);
                        foreach (var c in newColumns)
                        {
                            knownColumns.Add(c);
                            dataTable.Columns.Add(c, typeof(string));
                        }
                    }

                    var row = dataTable.NewRow();
                    row["GatewayName"] = xmlMsg.GatewayName;
                    row["MessageId"] = xmlMsg.MessageId;
                    row["SequenceNumber"] = xmlMsg.SequenceNumber;
                    foreach (var kv in fields)
                        row[kv.Key] = kv.Value ?? (object)DBNull.Value;

                    if (dataTable.Columns.Contains("ReceivedTime"))
                        row["ReceivedTime"] = DateTime.UtcNow;

                    dataTable.Rows.Add(row);
                }

                using var bulk = new SqlBulkCopy(conn)
                {
                    DestinationTableName = $"dbo.[{tableName}]",
                    BatchSize = _batchSize,
                    BulkCopyTimeout = 60
                };

                foreach (DataColumn col in dataTable.Columns)
                    bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

                await bulk.WriteToServerAsync(dataTable);

                var count = await GetMessageCountAsync(conn, tableName);
                OnMessageInserted(group.First().GatewayName, group.First().MessageId.ToString(), tableName, count);
            }
        }

        private static DataTable CreateDataTable(HashSet<string> existingColumns)
        {
            var dt = new DataTable();
            dt.Columns.Add("GatewayName", typeof(string));
            dt.Columns.Add("MessageId", typeof(int));
            dt.Columns.Add("SequenceNumber", typeof(long));
            foreach (var col in existingColumns)
                if (!dt.Columns.Contains(col))
                    dt.Columns.Add(col, typeof(string));
            return dt;
        }

        private static async Task EnsureTableAsync(SqlConnection conn, string tableName)
        {
            string createSql = $@"
                IF OBJECT_ID('dbo.[{tableName}]', 'U') IS NULL
                    CREATE TABLE dbo.[{tableName}] (
                        Id INT IDENTITY PRIMARY KEY,
                        ReceivedTime DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
                        GatewayName NVARCHAR(100) NOT NULL,
                        MessageId INT NOT NULL,
                        SequenceNumber BIGINT NOT NULL
                    );";
            await using var cmd = new SqlCommand(createSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task AddColumnAsync(SqlConnection conn, string tableName, string columnName)
        {
            string alterSql = $@"
                IF COL_LENGTH('dbo.[{tableName}]', '{columnName}') IS NULL
                    ALTER TABLE dbo.[{tableName}] ADD [{columnName}] NVARCHAR(MAX) NULL;";
            await using var cmd = new SqlCommand(alterSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<HashSet<string>> GetExistingColumnsAsync(SqlConnection conn, string tableName)
        {
            string sql = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME=@tbl;";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tbl", tableName);

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(reader.GetString(0));
            return result;
        }

        private static async Task<long> GetMessageCountAsync(SqlConnection conn, string tableName)
        {
            string sql = $"SELECT COUNT_BIG(1) FROM dbo.[{tableName}];";
            await using var cmd = new SqlCommand(sql, conn);
            return (long)await cmd.ExecuteScalarAsync();
        }

        private void OnMessageInserted(string gateway, string msgId, string table, long count)
        {
            try
            {
                MessageInserted?.Invoke(gateway, msgId, table, count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[XmlMessageLoader] Event handler failed: {ex}");
            }
        }

        private void LogFallback(string prefix, string gateway, int msgId, long seq, string xml)
        {
            try
            {
                File.AppendAllText(_fallbackFile,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {prefix} Gateway={gateway}, MessageId={msgId}, Seq={seq} XML={xml}{Environment.NewLine}");
            }
            catch { }
        }

        public async Task ShutdownAsync()
        {
            if (_disposed) return;
            _disposed = true;
            _queue.CompleteAdding();
            _cts.Cancel();
            try { await _worker.ConfigureAwait(false); } catch { }
        }

        public void Dispose()
        {
            ShutdownAsync().GetAwaiter().GetResult();
            _cts.Dispose();
            _queue.Dispose();
        }
    }
}
