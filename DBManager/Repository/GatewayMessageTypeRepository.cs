using DBManager.Common;
using DSGModels.Models;
using Microsoft.Data.SqlClient;

namespace DBManager.Repository
{
    public class GatewayMessageTypeRepository
    {
        private readonly string _connectionString;
        public GatewayMessageTypeRepository(string connectionString) => _connectionString = connectionString;

        public List<GatewayMessageTypeEntity> GetAll()
        {
            var list = new List<GatewayMessageTypeEntity>();            
            using var reader = SqlHelper.GetDataBySelect(_connectionString, "SELECT * FROM GatewayMessageType");
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

        public List<int> GetMessageTypeIdsForGateway(int gatewayId)
        {
            var list = new List<int>();
            using var reader = SqlHelper.GetDataBySelect(_connectionString,
                "SELECT MessageTypeId FROM GatewayMessageType WHERE GatewayId=@GatewayId",
                new SqlParameter("@GatewayId", gatewayId));
            while (reader.Read())
            {
                list.Add(Convert.ToInt32(reader["MessageTypeId"]));
            }
            return list;
        }
    }
}
