using DSGClient;
using DSGTool.Data.Models;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DSGTool.Data
{
    public class DbWorks
    {
        private readonly string _connectionString;

        public DbWorks(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ===========================
        // Gateway Methods
        // ===========================

        public List<Gateway> GetGateways()
        {
            var gateways = new List<Gateway>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT Id, PartitionId, EnvironmentName, GatewayName, HostIp, Port, UserName, Password FROM Gateway", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                gateways.Add(new Gateway(
                    Convert.ToInt32(reader["Id"].ToString()),
                    reader["PartitionId"].ToString(),
                    reader["EnvironmentName"].ToString(),
                    reader["GatewayName"].ToString(),
                    reader["HostIp"].ToString(),
                    Convert.ToInt32(reader["Port"].ToString()),
                    reader["UserName"].ToString(),
                    reader["Password"].ToString()
                ));
            }

            return gateways;
        }

        public int InsertGateway(Gateway gateway)
            => ExecuteSql("INSERT INTO Gateway(PartitionId, EnvironmentName, GatewayName, HostIp, Port, UserName, Password) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@PartitionId, @EnvironmentName, @GatewayName, @HostIp, @Port, @UserName, @Password)",
                new SqlParameter("@PartitionId", gateway.PartitionId),
                new SqlParameter("@EnvironmentName", gateway.EnvironmentName),
                new SqlParameter("@GatewayName", gateway.GatewayName),
                new SqlParameter("@HostIp", gateway.Host),
                new SqlParameter("@Port", Convert.ToInt32(gateway.Port)),
                new SqlParameter("@UserName", gateway.Username),
                new SqlParameter("@Password", gateway.Password));

        public void UpdateGateway(Gateway gateway)
            => ExecuteSql("UPDATE Gateway SET PartitionId = @partitionid, EnvironmentName = @environmentname, GatewayName = @gatewayname, HostIp = @hostip, Port = @port, UserName = @username, Password = @password WHERE Id = @id",
                new SqlParameter("@Id", gateway.Id),
                new SqlParameter("@PartitionId", gateway.PartitionId),
                new SqlParameter("@EnvironmentName", gateway.EnvironmentName),
                new SqlParameter("@GatewayName", gateway.GatewayName),
                new SqlParameter("@HostIp", gateway.Host),
                new SqlParameter("@Port", Convert.ToInt32(gateway.Port)),
                new SqlParameter("@UserName", gateway.Username),
                new SqlParameter("@Password", gateway.Password));

        public void DeleteGateway(string gatewayId)
            => ExecuteSql("DELETE FROM Gateway WHERE Id = @id", new SqlParameter("@id", gatewayId));

        public void DeleteMessagesByGateway(string gatewayName)
        {
            ExecuteProcedure("usp_ClearMessageTablesByGateway", new SqlParameter("@GatewayName", gatewayName));
        }

        // ===========================
        // MessageType Methods
        // ===========================

        public List<MessageType> GetMessageTypes()
        {
            var messageTypes = new List<MessageType>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT Id, Name, MessageId, IsSecMsg FROM MessageType", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                messageTypes.Add(new MessageType(
                    Convert.ToInt32(reader["Id"].ToString()),
                    reader["Name"].ToString(),
                    reader["MessageId"].ToString(),
                    Convert.ToBoolean(reader["IsSecMsg"])
                ));
            }

            return messageTypes;
        }

        public int InsertMessageType(MessageType messageType)
            => ExecuteSql("INSERT INTO MessageType (Name, MessageId, IsSecMsg) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@Name, @MessageId, @IsSecMsg)",
                new SqlParameter("@Name", messageType.Name),
                new SqlParameter("@MessageId", messageType.MessageId),
                new SqlParameter("@IsSecMsg", messageType.IsSecMsg));

        public void UpdateMessageType(MessageType messageType)
            => ExecuteSql("UPDATE MessageType SET Name = @Name, MessageId = @MessageId, IsSecMsg = @IsSecMsg WHERE Id = @Id",
                new SqlParameter("@Id", messageType.Id),
                new SqlParameter("@Name", messageType.Name),
                new SqlParameter("@MessageId", messageType.MessageId),
                new SqlParameter("@IsSecMsg", messageType.IsSecMsg));

        public void DeleteMessageType(int id)
            => ExecuteSql("DELETE FROM MessageType WHERE Id = @id", new SqlParameter("@id", id));

        // ===============================
        // Gateway MessageType Map Methods
        // ===============================

        public List<int> GetMessageTypeIdsForGateway(int gatewayId)
        {
            var ids = new List<int>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT MessageTypeId FROM GatewayMessageType WHERE GatewayId=@GatewayId", conn);
            cmd.Parameters.AddWithValue("@GatewayId", gatewayId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ids.Add(Convert.ToInt32(reader["MessageTypeId"]));
            }

            return ids;
        }

        public void InsertGatewayMessageType(int gatewayId, int messageTypeId)
            => ExecuteSql("INSERT INTO GatewayMessageType (GatewayId, MessageTypeId) VALUES (@GatewayId, @MessageTypeId)",
                new SqlParameter("@GatewayId", gatewayId),
                new SqlParameter("@MessageTypeId", messageTypeId));

        public void DeleteGatewayMessageType(int gatewayId, int messageTypeId)
            => ExecuteSql("DELETE FROM GatewayMessageType WHERE GatewayId=@GatewayId AND MessageTypeId=@MessageTypeId",
                new SqlParameter("@GatewayId", gatewayId),
                new SqlParameter("@MessageTypeId", messageTypeId));

        // ===========================
        // DSG Client Methods
        // ===========================

        public List<DSGClientEntity> GetDSGClientEntities()
        {
            var dsgClients = new List<DSGClientEntity>();

            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("SELECT Id, GatewayId, StartingSequenceNumber, EndingSequenceNumber, HeartbeatIntervalSeconds FROM DSGClient", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                dsgClients.Add(new DSGClientEntity(
                    Convert.ToInt32(reader["Id"]),
                    Convert.ToInt32(reader["GatewayId"]),
                    reader["StartingSequenceNumber"].ToString(),
                    reader["EndingSequenceNumber"].ToString(),
                    Convert.ToInt32(reader["HeartbeatIntervalSeconds"])
                ));
            }

            return dsgClients;
        }

        public int InsertDSGClient(DSGClientEntity dsgClient)
            => ExecuteSql("INSERT INTO DSGClient(GatewayId, StartingSequenceNumber, EndingSequenceNumber, HeartbeatIntervalSeconds) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@gatewayid, @startingsequencenumber, @endingsequencenumber, @heartbeatintervalseconds)",
                new SqlParameter("@gatewayid", Convert.ToInt32(dsgClient.GatewayId)),
                new SqlParameter("@startingsequencenumber", dsgClient.StartingSequenceNumber),
                new SqlParameter("@endingsequencenumber", dsgClient.EndingSequenceNumber),
                new SqlParameter("@heartbeatintervalseconds", dsgClient.HeartbeatIntervalSeconds)
            );

        public void UpdateDSGClient(DSGClientEntity dsgClient)
            => ExecuteSql("UPDATE DSGClient SET GatewayId = @gatewayid, StartingSequenceNumber = @startingsequencenumber, EndingSequenceNumber = @endingsequencenumber, HeartbeatIntervalSeconds = @heartbeatintervalseconds WHERE Id = @id",
                new SqlParameter("@id", dsgClient.Id),
                new SqlParameter("@gatewayid", dsgClient.GatewayId),
                new SqlParameter("@startingsequencenumber", dsgClient.StartingSequenceNumber),
                new SqlParameter("@endingsequencenumber", dsgClient.EndingSequenceNumber),
                new SqlParameter("@heartbeatintervalseconds", dsgClient.HeartbeatIntervalSeconds)
            );

        public void DeleteDSGClient(string dsgClientId)
            => ExecuteSql("DELETE FROM DSGClient WHERE Id = @id", new SqlParameter("@id", dsgClientId));
        

        // =============================
        // Execute SQL Stored Procedures
        // =============================
        public int ExecuteProcedure(string procedureName, params SqlParameter[] parameters)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(procedureName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }
            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected;
        }
        public int ExecuteSql(string sql, params SqlParameter[] parameters)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(sql, conn)
            {
                CommandType = CommandType.Text // optional, default is Text
            };

            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters);
            }

            int rowsAffected = cmd.ExecuteNonQuery();
            return rowsAffected;
        }

        public void DeleteAllMasterData()
        {
            ExecuteProcedure("usp_ClearAll");
        }
    }
}
