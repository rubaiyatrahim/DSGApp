namespace DSGTool.Data.Models
{
    public class GatewayEntity
    {
        public int Id { get; set; }
        public string PartitionId { get; set; }
        public string EnvironmentName { get; set; }
        public string GatewayName { get; set; }
        public string HostIp { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public GatewayEntity(int? id, string partitionId, string environmentName, string gatewayName, string hostIp, int port, string userName, string password)
        {
            if (id != null) Id = (int)id;            
            PartitionId = partitionId;
            EnvironmentName = environmentName;
            GatewayName = gatewayName;
            HostIp = hostIp;
            Port = port;
            UserName = userName;
            Password = password;
        }
    }
}
