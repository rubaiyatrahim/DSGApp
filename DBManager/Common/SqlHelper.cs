using Microsoft.Data.SqlClient;
using System.Data;

namespace DBManager.Common
{
    internal static class SqlHelper
    {
        public static int ExecuteNonQuery(string connectionString, string sql, params SqlParameter[] parameters)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }

        public static int ExecuteProcedure(string connectionString, string procedure, params SqlParameter[] parameters)
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand(procedure, conn);
            cmd.CommandType = CommandType.StoredProcedure;
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }
    }
}
