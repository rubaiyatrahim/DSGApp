namespace DSGClient
{
    public class MessageType
    {        
        public int Id { get => _id; set => _id = value; }
        public string Name { get => _name; set => _name = value; }
        public string MessageId { get => _messageId; set => _messageId = value; }
        public bool IsSecMsg { get => _isSecMsg; set => _isSecMsg = value; }

        private int _id;
        private string _name = string.Empty;
        private string _messageId = string.Empty;
        private bool _isSecMsg = false;
        
        public MessageType(int? id, string name, string messageId, bool isSecMsg)
        {
            if (id != null) _id = (int)id;
            Name = name;
            MessageId = messageId;
            IsSecMsg = isSecMsg;
        }
    }
}
