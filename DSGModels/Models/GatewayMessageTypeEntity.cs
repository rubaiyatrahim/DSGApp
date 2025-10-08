namespace DSGModels.Models
{
    public class GatewayMessageTypeEntity
    {
        public int GatewayId { get; set; }
        public int MessageTypeId { get; set; }

        public GatewayMessageTypeEntity() { }

        public GatewayMessageTypeEntity(int gatewayId, int messageTypeId)
        {
            GatewayId = gatewayId;
            MessageTypeId = messageTypeId;
        }
    }
}
