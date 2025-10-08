namespace DSGModels.Models
{
    public class MessageTypeEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MessageId { get; set; }
        public bool IsSecMsg { get; set; }

        public MessageTypeEntity(int? id, string name, string messageId, bool isSecMsg)
        {
            if (id != null) Id = (int)id;
            Name = name;
            MessageId = messageId;
            IsSecMsg = isSecMsg;
        }
    }
}
