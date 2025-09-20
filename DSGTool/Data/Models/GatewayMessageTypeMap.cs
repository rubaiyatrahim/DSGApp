namespace DSGTool.Data.Models
{
    public class GatewayMessageTypeMap
    {
        public int GatewayId { get; set; }
        public int MessageTypeId { get; set; }

        public GatewayMessageTypeMap(int gatewayId, int messageTypeId)
        {
            GatewayId = gatewayId;
            MessageTypeId = messageTypeId;
        }
    }
}