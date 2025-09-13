namespace DSGClient
{
    public class MessageType
    {        
        public string MessageName { get => _messageName; set => _messageName = value; }
        public string MessageId { get => _messageId; set => _messageId = value; }
        public bool IsSecMsg { get => _isSecMsg; set => _isSecMsg = value; }

        private string _messageName = string.Empty;
        private string _messageId = string.Empty;
        private bool _isSecMsg = false;
        
        public MessageType(string messageName, string messageId, bool isSecMsg)
        {
            MessageName = messageName;
            MessageId = messageId;
            IsSecMsg = isSecMsg;
        }
    }
}
