using DSGClient;
using System.ComponentModel;

namespace DSGTool
{
    public class ClientViewModel
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string MessageTypes { get; set; }
        public string Status { get; set; }
        public DSGClient.DSGClient Client { get; set; }

        public Gateway Gateway { get; }

        public string MessageCountsText => string.Join(", ", Gateway.MessageCounts.Select(kv => $"{kv.Key}: {kv.Value}"));

    public event PropertyChangedEventHandler? PropertyChanged;

    public ClientViewModel(Gateway gateway)
    {
        Gateway = gateway;
        Status = "Stopped";
    }

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageCountsText)));
    }
    }
}
