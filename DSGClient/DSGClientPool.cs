namespace DSGClient
{
    public sealed class DSGClientPool : IAsyncDisposable
    {
        private readonly List<DSGClient> _clients = new();
        public event Action<string, string>? MessageReceived; // clientId, msgType

        public DSGClientPool() { }

        public void AddClient(Gateway gateway, List<MessageType> messageTypes, int heartbeatSeconds = 2)
            => _clients.Add(new DSGClient(gateway, messageTypes, heartbeatSeconds));
        public async Task StartAllAsync(CancellationToken appStop = default) 
            => await Task.WhenAll(_clients.Select(c => c.StartAsync(appStop)));
        public async Task SendDownloadAllAsync(string partitionId, string startingSequenceNumber, string endingSequenceNumber) 
            => await Task.WhenAll(_clients.Select(c => c.DownloadAsync(partitionId, startingSequenceNumber, endingSequenceNumber)));
        public async Task SendHeartbeatAllAsync() 
            => await Task.WhenAll(_clients.Select(c => c.SendHeartbeatAsync()));
        public async Task SendLogoutAllAsync() 
            => await Task.WhenAll(_clients.Select(c => c.LogoutAsync()));        
        public async Task StopAllAsync() 
            => await Task.WhenAll(_clients.Select(c => c.StopAsync()));
        
        public async ValueTask DisposeAsync()
        {
            foreach (var client in _clients)
            {
                await client.DisposeAsync();
            }
        }
    }
}
