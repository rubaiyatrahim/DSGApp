namespace DSGClient
{
    public class Gateway
    {
        private int _id;
        private string _partitionId;
        private string _environmentname;
        private string _gatewayname;
        private string _host;
        private int _port;
        private string _username;
        private string _password;

        public Gateway(int? id, string partitionId, string environmentName, string gatewayName, string host, int port, string userName, string password)
        {
            if (id != null) _id = (int)id;
            _partitionId = partitionId;
            _environmentname = environmentName;
            _gatewayname = gatewayName;
            _host = host;
            _port = port;
            _username = userName;
            _password = password;
        }
        public int Id { get => _id; set => _id = value; }
        public string PartitionId { get => _partitionId; set => _partitionId = value; }
        public string EnvironmentName { get => _environmentname; set => _environmentname = value; }
        public string GatewayName { get => _gatewayname; set => _gatewayname = value; }
        public string Host { get => _host; set => _host = value; }
        public int Port { get => _port; set => _port = value; }
        public string Username { get => _username; set => _username = value; }
        public string Password { get => _password; set => _password = value; }
    }
}
