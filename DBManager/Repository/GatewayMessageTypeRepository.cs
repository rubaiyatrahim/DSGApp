using DBManager.Common;
using DSGModels.Models;
using Microsoft.Data.SqlClient;

namespace DBManager.Repository
{
    internal class GatewayMessageTypeRepository
    {
        private readonly string _connectionString;
        public GatewayMessageTypeRepository(string connectionString) => _connectionString = connectionString;

        public List<GatewayMessageTypeEntity> GetAll()
        {
            var list = new List<GatewayMessageTypeEntity>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM GatewayMessageType", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new GatewayMessageTypeEntity(
                    Convert.ToInt32(reader["GatewayId"]),
                    Convert.ToInt32(reader["MessageTypeId"])
                ));
            }
            return list;
        }
        public int Insert(GatewayMessageTypeEntity gmt) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                "INSERT INTO GatewayMessageType (GatewayId, MessageTypeId) VALUES (@GatewayId, @MessageTypeId)",
                new SqlParameter("@GatewayId", gmt.GatewayId),
                new SqlParameter("@MessageTypeId", gmt.MessageTypeId));
        public int Update(GatewayMessageTypeEntity gmt) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                "UPDATE GatewayMessageType SET MessageTypeId=@MessageTypeId WHERE GatewayId=@GatewayId",
                new SqlParameter("@GatewayId", gmt.GatewayId),
                new SqlParameter("@MessageTypeId", gmt.MessageTypeId));
        public int Delete(int gatewayId, int messageTypeId) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                "DELETE FROM GatewayMessageType WHERE GatewayId=@GatewayId AND MessageTypeId=@MessageTypeId",
                new SqlParameter("@GatewayId", gatewayId),
                new SqlParameter("@MessageTypeId", messageTypeId));
    }
}
