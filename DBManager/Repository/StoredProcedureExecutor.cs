using DBManager.Common;
using Microsoft.Data.SqlClient;

namespace DBManager.Repositories
{
    public class StoredProcedureExecutor
    {
        private readonly string _connectionString;
        public StoredProcedureExecutor(string connectionString) => _connectionString = connectionString;

        public int Execute(string name, params SqlParameter[] parameters) =>
            SqlHelper.ExecuteProcedure(_connectionString, name, parameters);

        public void ClearAll() => Execute("usp_ClearAll");

        public void ClearMessagesByGateway(string gatewayName) =>
            Execute("usp_ClearMessageTablesByGateway", new SqlParameter("@GatewayName", gatewayName));
    }
}
