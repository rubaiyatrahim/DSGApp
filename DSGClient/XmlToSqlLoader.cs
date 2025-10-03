using System.Collections.Concurrent;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace DSGClient
{
    public sealed class XmlToSqlLoader : IDisposable
    {
        private class XmlMessage
        {
            public string Xml { get; set; }
            public string GatewayName { get; set; }
            public int MessageId { get; set; }
            public long SequenceNumber { get; set; }
        }

        private readonly string _connectionString;
        private readonly BlockingCollection<XmlMessage> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _worker;
        private volatile bool _disposed;
        private readonly string _fallbackFile = "XmlToSqlLoader_Failed.log";

        public event Action<string, string, string, long>? MessageReceivedDB; // gatewayName, messageId, tableName, messageCount

        public XmlToSqlLoader(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _worker = Task.Run(ProcessQueueAsync);
        }

        public void EnqueueXml(string xml, string gatewayName, int messageId, long sequenceNumber)
        {
            if (_disposed)
            {
                File.AppendAllText(_fallbackFile,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} DROPPED (disposed) Gateway={gatewayName}, MessageId={messageId}, SequenceNo={sequenceNumber} XML={xml}{Environment.NewLine}");
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
                });
            }
            catch (Exception ex)
            {
                File.AppendAllText(_fallbackFile,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} Exception: {ex.Message} Gateway={gatewayName}, MessageId={messageId}, SequenceNo={sequenceNumber} XML={xml}{Environment.NewLine}");
            }
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                foreach (var xmlMessage in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        await InsertXmlAsync(xmlMessage);
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(_fallbackFile,
                            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} DROPPED (error) Gateway={xmlMessage.GatewayName}, MessageId={xmlMessage.MessageId}, SequenceNo={xmlMessage.SequenceNumber} XML={xmlMessage.Xml} Exception={ex.Message}{Environment.NewLine}");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task InsertXmlAsync(XmlMessage xml)
        {
            var doc = XDocument.Parse(xml.Xml);
            var docRoot = doc.Root ?? throw new InvalidOperationException("Missing root element");

            string tableName = docRoot.Attribute("name")?.Value
                               ?? throw new InvalidOperationException("Missing name attribute");

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Ensure table exists
            string createSql = $@"
            IF OBJECT_ID('dbo.[{tableName}]', 'U') IS NULL
                CREATE TABLE dbo.[{tableName}] (
                    Id INT IDENTITY PRIMARY KEY,
                    ReceivedTime DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
                    GatewayName NVARCHAR(100) NOT NULL,
                    MessageId INT NOT NULL,
                    SequenceNumber BIGINT NOT NULL
                );";
            using (var cmd = new SqlCommand(createSql, conn)) await cmd.ExecuteNonQueryAsync();

            // Collect fields
            var fields = new Dictionary<string, string>();
            foreach (var field in docRoot.Elements("field"))
            {
                string name = field.Attribute("name")?.Value;
                string value = field.Value;
                if (!string.IsNullOrEmpty(name)) fields[name] = value;
            }

            // Ensure columns
            foreach (var kv in fields)
            {
                string alterSql = $@"
                IF COL_LENGTH('dbo.[{tableName}]', '{kv.Key}') IS NULL
                    ALTER TABLE dbo.[{tableName}] ADD [{kv.Key}] NVARCHAR(MAX) NULL;";
                using var cmd = new SqlCommand(alterSql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Insert row
            var columnList = string.Join(",", fields.Keys.Select(k => $"[{k}]"));
            var paramList = string.Join(",", fields.Keys.Select((k, i) => $"@p{i}"));
            var parameters = fields.Values.Select((v, i) => new SqlParameter($"@p{i}", v)).ToArray();

            string insertSql = $@"
            INSERT INTO dbo.[{tableName}] (GatewayName, MessageId, SequenceNumber, {columnList})
            VALUES (@gw, @mid, @seq, {paramList});";

            using var insertCmd = new SqlCommand(insertSql, conn);
            insertCmd.Parameters.Add(new SqlParameter("@gw", xml.GatewayName));
            insertCmd.Parameters.Add(new SqlParameter("@mid", xml.MessageId));
            insertCmd.Parameters.Add(new SqlParameter("@seq", xml.SequenceNumber));
            insertCmd.Parameters.AddRange(parameters);

            await insertCmd.ExecuteNonQueryAsync();
            var messageCount = GetMessageCount(conn, tableName);
            OnMessageReceivedDB(xml.GatewayName, xml.MessageId.ToString(), tableName, (long)messageCount);
        }

        private void OnMessageReceivedDB(string gatewayName, string messageId, string tableName, long messageCount)
        {
            var handler = MessageReceivedDB; // local copy for thread safety
            if (handler != null)
            {
                try
                {
                    handler.Invoke(gatewayName, messageId, tableName, messageCount);
                }
                catch (Exception ex)
                {
                    // Never let an exception in a subscriber crash this class
                    Console.WriteLine($"[XmlToSqlLoader] Error in MessageReceivedDB handler: {ex}");
                }
            }
        }
        private int GetMessageCount(SqlConnection conn, string tableName)
        {
            string countSql = $"SELECT COUNT(1) FROM dbo.[{tableName}];";
            using var cmd = new SqlCommand(countSql, conn);
            var messageCount = cmd.ExecuteScalar();
            return (int)messageCount;
        }

        public async Task ShutdownAsync()
        {
            if (_disposed) return;

            _disposed = true;
            _queue.CompleteAdding();
            _cts.Cancel();

            try { await _worker; } catch { /* ignore */ }
        }

        public void Dispose()
        {
            ShutdownAsync().GetAwaiter().GetResult();
            _cts.Dispose();
            _queue.Dispose();
        }
    }

}
