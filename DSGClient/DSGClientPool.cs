namespace DSGClient
{
    public sealed class DSGClientPool : IAsyncDisposable
    {
        private readonly List<DSGClient> _clients = new();
        public event Action<string, string>? MessageReceived; // clientId, msgType
        public event Action<string, bool> StatusChanged;

        public DSGClientPool() { }
        
        public DSGClient AddClient(Gateway gateway, List<MessageType> messageTypes, string startingSequenceNumber, string endingSequenceNumber, int heartbeatSeconds = 2)
        {
            var client = new DSGClient(gateway, messageTypes, startingSequenceNumber, endingSequenceNumber, heartbeatSeconds);

            client.MessageReceived += (clientGateName, messageType) => MessageReceived?.Invoke(clientGateName, messageType);
            client.StatusChanged += (clientGatewayName, connected) => StatusChanged?.Invoke(clientGatewayName, connected);

            _clients.Add(client);
            return client;
        }

        public async Task StartAllAsync(CancellationToken appStop = default) 
            => await Task.WhenAll(_clients.Select(c => c.StartAsync(appStop)));
        public async Task SendDownloadAllAsync() 
            => await Task.WhenAll(_clients.Select(c => c.DownloadAsync()));
        public async Task SendHeartbeatAllAsync() 
            => await Task.WhenAll(_clients.Select(c => c.SendHeartbeatAsync()));
        public async Task SendLogoutAllAsync() 
            => await Task.WhenAll(_clients.Select(c => c.LogoutAsync()));        
        public async Task StopAllAsync() 
            => await Task.WhenAll(_clients.Select(c => c.StopAsync()));
        public List<DSGClient> Clients => _clients;
                    
        public async ValueTask DisposeAsync()
        {
            foreach (var client in _clients)
            {
                await client.DisposeAsync();
            }
        }
    }
}
