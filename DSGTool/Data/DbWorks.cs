using DSGClient;
using DSGTool.Data.Models;
using System;
using System.Collections.Generic;
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
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "INSERT INTO Gateway(PartitionId, EnvironmentName, GatewayName, HostIp, Port, UserName, Password) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@PartitionId, @EnvironmentName, @GatewayName, @HostIp, @Port, @UserName, @Password)",
                conn);
            cmd.Parameters.AddWithValue("@PartitionId", gateway.PartitionId);
            cmd.Parameters.AddWithValue("@EnvironmentName", gateway.EnvironmentName);
            cmd.Parameters.AddWithValue("@GatewayName", gateway.GatewayName);
            cmd.Parameters.AddWithValue("@HostIp", gateway.Host);
            cmd.Parameters.AddWithValue("@Port", Convert.ToInt32(gateway.Port));
            cmd.Parameters.AddWithValue("@UserName", gateway.Username);
            cmd.Parameters.AddWithValue("@Password", gateway.Password);

            int newId = (int)cmd.ExecuteScalar();

            return newId;
        }

        public void UpdateGateway(Gateway gateway)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "UPDATE Gateway SET PartitionId = @partitionid, EnvironmentName = @environmentname, GatewayName = @gatewayname, HostIp = @hostip, Port = @port, UserName = @username, Password = @password WHERE Id = @id",
                conn);
            cmd.Parameters.AddWithValue("@Id", gateway.Id);
            cmd.Parameters.AddWithValue("@PartitionId", gateway.PartitionId);
            cmd.Parameters.AddWithValue("@EnvironmentName", gateway.EnvironmentName);
            cmd.Parameters.AddWithValue("@GatewayName", gateway.GatewayName);
            cmd.Parameters.AddWithValue("@HostIp", gateway.Host);
            cmd.Parameters.AddWithValue("@Port", Convert.ToInt32(gateway.Port));
            cmd.Parameters.AddWithValue("@UserName", gateway.Username);
            cmd.Parameters.AddWithValue("@Password", gateway.Password);

            cmd.ExecuteNonQuery();
        }

        public void DeleteGateway(string gatewayId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("DELETE FROM Gateway WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", gatewayId);
            cmd.ExecuteNonQuery();
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
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "INSERT INTO MessageType (Name, MessageId, IsSecMsg) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@Name, @MessageId, @IsSecMsg)",
                conn);
            cmd.Parameters.AddWithValue("@Name", messageType.Name);
            cmd.Parameters.AddWithValue("@MessageId", messageType.MessageId);
            cmd.Parameters.AddWithValue("@IsSecMsg", messageType.IsSecMsg);

            int newId = (int)cmd.ExecuteScalar();

            return newId;
        }

        public void UpdateMessageType(MessageType messageType)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "UPDATE MessageType SET Name=@Name, MessageId=@MessageId, IsSecMsg=@IsSecMsg WHERE Id=@Id",
                conn);
            cmd.Parameters.AddWithValue("@Id", messageType.Id);
            cmd.Parameters.AddWithValue("@Name", messageType.Name);
            cmd.Parameters.AddWithValue("@MessageId", messageType.MessageId);
            cmd.Parameters.AddWithValue("@IsSecMsg", messageType.IsSecMsg);

            cmd.ExecuteNonQuery();
        }

        public void DeleteMessageType(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("DELETE FROM MessageType WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

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
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "INSERT INTO GatewayMessageType (GatewayId, MessageTypeId) VALUES (@GatewayId, @MessageTypeId)",
                conn);
            cmd.Parameters.AddWithValue("@GatewayId", gatewayId);
            cmd.Parameters.AddWithValue("@MessageTypeId", messageTypeId);

            cmd.ExecuteNonQuery();
        }

        public void DeleteGatewayMessageType(int gatewayId, int messageTypeId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "DELETE FROM GatewayMessageType WHERE GatewayId=@GatewayId AND MessageTypeId=@MessageTypeId",
                conn);
            cmd.Parameters.AddWithValue("@GatewayId", gatewayId);
            cmd.Parameters.AddWithValue("@MessageTypeId", messageTypeId);

            cmd.ExecuteNonQuery();
        }

        // ===========================
        // DSG Client Methods
        // ===========================

        public List<DSGClientEntity> GetDSGClients()
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
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "INSERT INTO DSGClient(GatewayId, StartingSequenceNumber, EndingSequenceNumber, HeartbeatIntervalSeconds) " +
                "OUTPUT INSERTED.Id " +
                "VALUES (@gatewayid, @startingsequencenumber, @endingsequencenumber, @heartbeatintervalseconds)",
                conn);
            cmd.Parameters.AddWithValue("@gatewayid", dsgClient.GatewayId);
            cmd.Parameters.AddWithValue("@startingsequencenumber", dsgClient.StartingSequenceNumber);
            cmd.Parameters.AddWithValue("@endingsequencenumber", dsgClient.EndingSequenceNumber);
            cmd.Parameters.AddWithValue("@heartbeatintervalseconds", dsgClient.HeartbeatIntervalSeconds);

            int newId = (int)cmd.ExecuteScalar();

            return newId;
        }

        public void UpdateDSGClient(DSGClientEntity dsgClient)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand(
                "UPDATE DSGClient SET GatewayId = @gatewayid, StartingSequenceNumber = @startingsequencenumber, EndingSequenceNumber = @endingsequencenumber, HeartbeatIntervalSeconds = @heartbeatintervalseconds WHERE Id = @id",
                conn);
            cmd.Parameters.AddWithValue("@id", dsgClient.Id);
            cmd.Parameters.AddWithValue("@gatewayid", dsgClient.GatewayId);
            cmd.Parameters.AddWithValue("@startingsequencenumber", dsgClient.StartingSequenceNumber);
            cmd.Parameters.AddWithValue("@endingsequencenumber", dsgClient.EndingSequenceNumber);
            cmd.Parameters.AddWithValue("@heartbeatintervalseconds", dsgClient.HeartbeatIntervalSeconds);

            cmd.ExecuteNonQuery();
        }

        public void DeleteDSGClient(string dsgClientId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using var cmd = new SqlCommand("DELETE FROM DSGClient WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", dsgClientId);
            cmd.ExecuteNonQuery();
        }

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
        public void DeleteAllData()
        {
            ExecuteProcedure("usp_ClearAll");
        }
    }
}
