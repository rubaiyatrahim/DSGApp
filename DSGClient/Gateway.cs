namespace DSGClient
{
    public class Gateway
    {
        private string _environmentname;
        private string _gatewayname;
        private string _host;
        private int _port;
        private string _username;
        private string _password;

        public Dictionary<string, int> MessageCounts { get; } = new();

        public Gateway(string environmentName, string gatewayName, string host, int port, string userName, string password)
        {
            _environmentname = environmentName;
            _gatewayname = gatewayName;
            _host = host;
            _port = port;
            _username = userName;
            _password = password;
        }

        public string EnvironmentName { get => _environmentname; set => _environmentname = value; }
        public string GatewayName { get => _gatewayname; set => _gatewayname = value; }
        public string Host { get => _host; set => _host = value; }
        public int Port { get => _port; set => _port = value; }
        public string Username { get => _username; set => _username = value; }
        public string Password { get => _password; set => _password = value; }

        public void IncrementMessageCount(string msgType)
        {
            if (MessageCounts.ContainsKey(msgType))
                MessageCounts[msgType]++;
            else
                MessageCounts[msgType] = 1;
        }
    }
}
