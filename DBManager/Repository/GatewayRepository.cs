using DBManager.Common;
using DSGModels.Models;
using Microsoft.Data.SqlClient;

namespace DBManager.Repositories
{
    public class GatewayRepository
    {
        private readonly string _connectionString;

        public GatewayRepository(string connectionString) => _connectionString = connectionString;

        public List<GatewayEntity> GetAll()
        {
            var gateways = new List<GatewayEntity>();
            using var reader = SqlHelper.GetDataBySelect(_connectionString, "SELECT * FROM Gateway");
            while (reader.Read())
            {
                gateways.Add(new GatewayEntity(
                    Convert.ToInt32(reader["Id"]),
                    reader["PartitionId"].ToString(),
                    reader["EnvironmentName"].ToString(),
                    reader["GatewayName"].ToString(),
                    reader["HostIp"].ToString(),
                    Convert.ToInt32(reader["Port"]),
                    reader["UserName"].ToString(),
                    reader["Password"].ToString()
                ));
            }
            return gateways;
        }

        public int Insert(GatewayEntity g) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                @"INSERT INTO Gateway (PartitionId, EnvironmentName, GatewayName, HostIp, Port, UserName, Password)
                  OUTPUT INSERTED.Id VALUES (@PartitionId,@EnvironmentName,@GatewayName,@HostIp,@Port,@UserName,@Password)",
                new SqlParameter("@PartitionId", g.PartitionId),
                new SqlParameter("@EnvironmentName", g.EnvironmentName),
                new SqlParameter("@GatewayName", g.GatewayName),
                new SqlParameter("@HostIp", g.Host),
                new SqlParameter("@Port", g.Port),
                new SqlParameter("@UserName", g.Username),
                new SqlParameter("@Password", g.Password));

        public void Update(GatewayEntity g) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                @"UPDATE Gateway SET PartitionId=@PartitionId, EnvironmentName=@EnvironmentName,
                  GatewayName=@GatewayName, HostIp=@HostIp, Port=@Port, UserName=@UserName, Password=@Password
                  WHERE Id=@Id",
                new SqlParameter("@Id", g.Id),
                new SqlParameter("@PartitionId", g.PartitionId),
                new SqlParameter("@EnvironmentName", g.EnvironmentName),
                new SqlParameter("@GatewayName", g.GatewayName),
                new SqlParameter("@HostIp", g.Host),
                new SqlParameter("@Port", g.Port),
                new SqlParameter("@UserName", g.Username),
                new SqlParameter("@Password", g.Password));

        public void Delete(int id) =>
            SqlHelper.ExecuteNonQuery(_connectionString, "DELETE FROM Gateway WHERE Id=@Id",
                new SqlParameter("@Id", id));
    }
}
