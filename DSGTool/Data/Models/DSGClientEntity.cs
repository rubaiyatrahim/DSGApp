namespace DSGTool.Data.Models
{
    public class DSGClientEntity
    {
        public int Id { get; set; }
        public int GatewayId { get; set; }
        public string StartingSequenceNumber { get; set; }
        public string EndingSequenceNumber { get; set; }
        public int HeartbeatIntervalSeconds { get; set; }

        public DSGClientEntity(int? id, int gatewayId, string startingSequenceNumber, string endingSequenceNumber, int heartbeatIntervalSeconds)
        {
            if (id != null)
            {
                Id = (int)id;
            }

            GatewayId = gatewayId;
            StartingSequenceNumber = startingSequenceNumber;
            EndingSequenceNumber = endingSequenceNumber;
            HeartbeatIntervalSeconds = heartbeatIntervalSeconds;
        }
    }
}