using DBManager.Common;
using DSGModels.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace DBManager.Repositories
{
    public class MessageTypeRepository
    {
        private readonly string _connectionString;

        public MessageTypeRepository(string conn) => _connectionString = conn;

        public List<MessageTypeEntity> GetAll()
        {
            var list = new List<MessageTypeEntity>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM MessageType", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new MessageTypeEntity(
                    Convert.ToInt32(reader["Id"]),
                    reader["Name"].ToString(),
                    reader["MessageId"].ToString(),
                    Convert.ToBoolean(reader["IsSecMsg"])
                ));
            }
            return list;
        }

        public int Insert(MessageTypeEntity mt) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                "INSERT INTO MessageType (Name, MessageId, IsSecMsg) OUTPUT INSERTED.Id VALUES (@Name, @MessageId, @IsSecMsg)",
                new SqlParameter("@Name", mt.Name),
                new SqlParameter("@MessageId", mt.MessageId),
                new SqlParameter("@IsSecMsg", mt.IsSecMsg));

        public void Update(MessageTypeEntity mt) =>
            SqlHelper.ExecuteNonQuery(_connectionString,
                "UPDATE MessageType SET Name=@Name, MessageId=@MessageId, IsSecMsg=@IsSecMsg WHERE Id=@Id",
                new SqlParameter("@Id", mt.Id),
                new SqlParameter("@Name", mt.Name),
                new SqlParameter("@MessageId", mt.MessageId),
                new SqlParameter("@IsSecMsg", mt.IsSecMsg));

        public void Delete(int id) =>
            SqlHelper.ExecuteNonQuery(_connectionString, "DELETE FROM MessageType WHERE Id=@Id", new SqlParameter("@Id", id));
    }
}
