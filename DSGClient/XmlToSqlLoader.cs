using System.Collections.Concurrent;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace DSGClient
{
    public sealed class XmlToSqlLoader : IDisposable
    {
        private readonly string _connectionString;
        private readonly BlockingCollection<string> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _worker;
        private volatile bool _disposed;
        private readonly string _fallbackFile = "XmlToSqlLoader_Failed.log";

        public XmlToSqlLoader(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _worker = Task.Run(ProcessQueueAsync);
        }

        public void EnqueueXml(string xml)
        {
            if (_disposed)
            {
                File.AppendAllText(_fallbackFile,
                    $"{DateTime.UtcNow.ToString("yyyy-mm-dd hh:mm:ss.fff")} DROPPED (disposed) XML={xml}{Environment.NewLine}");
                return;
            }

            _queue.Add(xml);
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                foreach (var xml in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    try
                    {
                        await InsertXmlAsync(xml);
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(_fallbackFile,
                            $"{DateTime.UtcNow.ToString("yyyy-mm-dd hh:mm:ss.fff")} ERROR {ex} XML={xml}{Environment.NewLine}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
        }

        private async Task InsertXmlAsync(string xml)
        {
            var doc = XDocument.Parse(xml);
            var docRoot = doc.Root ?? throw new InvalidOperationException("Missing root element");

            string tableName = docRoot.Attribute("name")?.Value
                               ?? throw new InvalidOperationException("Missing name attribute");

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            // Ensure table
            string createSql = $@"
IF OBJECT_ID('dbo.[{tableName}]', 'U') IS NULL
    CREATE TABLE dbo.[{tableName}] (
        Id INT IDENTITY PRIMARY KEY,
        ReceivedTime DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()
    );";
            using (var cmd = new SqlCommand(createSql, conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Collect fields
            var fields = new Dictionary<string, string>();
            foreach (var field in docRoot.Elements("field"))
            {
                string name = field.Attribute("name")?.Value;
                string value = field.Value;

                if (!string.IsNullOrEmpty(name))
                    fields[name] = value;
            }

            // Ensure columns
            foreach (var kv in fields)
            {
                string alterSql = $@"
IF COL_LENGTH('dbo.[{tableName}]', '{kv.Key}') IS NULL
    ALTER TABLE dbo.[{tableName}] ADD [{kv.Key}] NVARCHAR(MAX) NULL;";
                using (var cmd = new SqlCommand(alterSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            // Insert row
            var columnList = string.Join(",", fields.Keys.Select(k => $"[{k}]"));
            var paramList = string.Join(",", fields.Keys.Select((k, i) => $"@p{i}"));
            var parameters = fields.Values.Select((v, i) => new SqlParameter($"@p{i}", v)).ToArray();

            string insertSql = $@"
INSERT INTO dbo.[{tableName}] ({columnList})
VALUES ({paramList});";

            using (var cmd = new SqlCommand(insertSql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _queue.CompleteAdding();
            _cts.Cancel();

            try { _worker.Wait(); } catch { /* ignore */ }

            _cts.Dispose();
            _queue.Dispose();
        }
    }
}
