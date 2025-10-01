using Microsoft.Data.SqlClient;
using System.Xml.Linq;

namespace DSGClient
{
    public class XmlToSqlLoader
    {
        private readonly string _connectionString;

        public XmlToSqlLoader(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InsertXml(string xml)
        {
            var doc = XDocument.Parse(xml);
            var smsg = doc.Element("smsg");

            if (smsg == null) throw new InvalidOperationException("Invalid XML: missing <smsg>");

            string tableName = smsg.Attribute("name")?.Value
                               ?? throw new InvalidOperationException("Missing smsg name");

            var fields = smsg.Elements("field").ToList();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // 1. Ensure table exists with Id + dynamic columns
            var createCols = string.Join(", ",
                fields.Select(f => $"[{f.Attribute("name").Value}] NVARCHAR(MAX)"));

            var createSql = $@"
IF OBJECT_ID('{tableName}', 'U') IS NULL
BEGIN
    CREATE TABLE [{tableName}] (
        Id INT IDENTITY PRIMARY KEY,
        {createCols}
    )
END";

            using (var cmd = new SqlCommand(createSql, conn))
            {
                cmd.ExecuteNonQuery();
            }

            // 2. Ensure all columns exist (schema evolves)
            foreach (var field in fields)
            {
                string colName = field.Attribute("name").Value;
                string alterSql = $@"
IF COL_LENGTH('{tableName}', '{colName}') IS NULL
    ALTER TABLE [{tableName}] ADD [{colName}] NVARCHAR(MAX)";
                using (var cmd = new SqlCommand(alterSql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            // 3. Insert row
            var colNames = string.Join(", ", fields.Select(f => $"[{f.Attribute("name").Value}]"));
            var paramNames = string.Join(", ", fields.Select((f, i) => $"@p{i}"));
            var insertSql = $"INSERT INTO [{tableName}] ({colNames}) VALUES ({paramNames})";

            using (var cmd = new SqlCommand(insertSql, conn))
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", fields[i].Value);
                }
                cmd.ExecuteNonQuery();
            }
        }
    }
}
