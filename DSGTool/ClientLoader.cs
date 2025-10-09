using DSGClient;
using DBManager;
using DSGTool;
using DSGModels.Models;    

public class ClientLoader
{
    private readonly DatabaseManager _db;

    public ClientLoader()
    {
        string connectionString = "Server=192.168.102.15;Database=DSGData;User Id=rubaiyat;Password=12345;TrustServerCertificate=True;";
        _db = new DatabaseManager(connectionString);
        Console.WriteLine("Database connection established.");
    }

    public DSGClientPool LoadClients()
    {
        Console.WriteLine("Loading DSG Clients from database...");

        var gateways = _db.Gateways.GetAll();
        Console.WriteLine($"Loaded {gateways.Count} Gateways from database.");

        var messageTypes = _db.MessageTypes.GetAll();
        Console.WriteLine($"Loaded {messageTypes.Count} Message Types from database.");

        var clients = _db.DSGClients.GetAll();
        Console.WriteLine($"Loaded {clients.Count} DSG Client entities from database.");

        var clientPool = new DSGClientPool();

        foreach (var client in clients)
        {
            List<int> gatewayMessageTypeIds = GetMessageTypeIdsForGateway(client.GatewayId);
            var gatewayMessageTypes = messageTypes.Where(mt => gatewayMessageTypeIds.Contains(mt.Id)).ToList();
            var gatewayClient = gateways.Single(x => x.Id == client.GatewayId);
            clientPool.AddClient(
                ClassConverter.ToClass(gatewayClient),
                gatewayMessageTypes.Select(x => ClassConverter.ToClass(x)).ToList(),
                client.StartingSequenceNumber,
                client.EndingSequenceNumber,
                client.HeartbeatIntervalSeconds
            );
        }

        Console.WriteLine("All DSG Clients loaded into the client pool.");
        return clientPool;
    }

    public int AddGateway(Gateway g) => _db.Gateways.Insert(ClassConverter.ToEntity(g));
    public int AddMessageType(MessageType mt) => _db.MessageTypes.Insert(ClassConverter.ToEntity(mt));
    public void AddGatewayMessageType(int gId, int mId) => _db.GatewayMessageTypes.Insert(new GatewayMessageTypeEntity(gId, mId));
    public void DeleteAllMasterData() => _db.Procedures.ClearAll();
    public void DeleteMessagesByGateway(string gatewayName) => _db.Procedures.ClearMessagesByGateway(gatewayName);
    public int AddDSGClient(DSGClientEntity dce) => _db.DSGClients.Insert(dce);
    private List<int> GetMessageTypeIdsForGateway(int gId) => _db.GatewayMessageTypes.GetMessageTypeIdsForGateway(gId);
}
