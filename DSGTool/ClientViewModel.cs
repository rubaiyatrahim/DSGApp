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
    }
}
