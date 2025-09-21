namespace DSGTool.Data.Models
{
    public class GatewayMessageType
    {
        public int GatewayId { get; set; }
        public int MessageTypeId { get; set; }

        public GatewayMessageType() { }

        public GatewayMessageType(int gatewayId, int messageTypeId)
        {
            GatewayId = gatewayId;
            MessageTypeId = messageTypeId;
        }
    }
}
