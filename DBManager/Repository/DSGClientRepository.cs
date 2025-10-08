using DBManager.Common;
using Microsoft.Data.SqlClient;
using DSGModels.Models;
using System;
using System.Collections.Generic;

namespace DBManager.Repositories
{
    public class DSGClientRepository
    {
        private readonly string _connectionString;
        public DSGClientRepository(string conn) => _connectionString = conn;

        public List<DSGClientEntity> GetAll()
        {
            var list = new List<DSGClientEntity>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM DSGClient", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new DSGClientEntity(
                    Convert.ToInt32(reader["Id"]),
                    Convert.ToInt32(reader["GatewayId"]),
                    reader["StartingSequenceNumber"].ToString(),
                    reader["EndingSequenceNumber"].ToString(),
                    Convert.ToInt32(reader["HeartbeatIntervalSeconds"])
                ));
            }
            return list;
        }

        public int Insert(DSGClientEntity c) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                "INSERT INTO DSGClient (GatewayId, StartingSequenceNumber, EndingSequenceNumber, HeartbeatIntervalSeconds) OUTPUT INSERTED.Id VALUES (@GatewayId,@Start,@End,@Hb)",
                new SqlParameter("@GatewayId", c.GatewayId),
                new SqlParameter("@Start", c.StartingSequenceNumber),
                new SqlParameter("@End", c.EndingSequenceNumber),
                new SqlParameter("@Hb", c.HeartbeatIntervalSeconds));

        public void Update(DSGClientEntity c) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                "UPDATE DSGClient SET GatewayId=@GatewayId, StartingSequenceNumber=@Start, EndingSequenceNumber=@End, HeartbeatIntervalSeconds=@Hb WHERE Id=@Id",
                new SqlParameter("@Id", c.Id),
                new SqlParameter("@GatewayId", c.GatewayId),
                new SqlParameter("@Start", c.StartingSequenceNumber),
                new SqlParameter("@End", c.EndingSequenceNumber),
                new SqlParameter("@Hb", c.HeartbeatIntervalSeconds));

        public void Delete(int id) =>
            SqlHelper.ExecuteNonQuery(_connectionString, "DELETE FROM DSGClient WHERE Id=@Id", new SqlParameter("@Id", id));
    }
}
