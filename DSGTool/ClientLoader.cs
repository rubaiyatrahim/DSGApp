using DSGClient;
using DSGTool.Data;
using DSGTool.Data.Models;
using System.Net;

public class ClientLoader
{
    private readonly DbWorks _db;

    public ClientLoader()
    {
        string connectionString = "Server=192.168.102.15;Database=DSGData;User Id=rubaiyat;Password=12345;TrustServerCertificate=True;";
        _db = new DbWorks(connectionString);
        //Console.WriteLine("Database connection established.");
    }

    public DSGClientPool LoadClients()
    {
        LogHelper.Info("Loading DSG Clients from database...");

        var gateways = _db.GetGateways();
        LogHelper.Info($"Loaded {gateways.Count} Gateways from database.");

        var messageTypes = _db.GetMessageTypes();
        LogHelper.Info($"Loaded {messageTypes.Count} Message Types from database.");

        var clients = _db.GetDSGClientEntities();
        LogHelper.Info($"Loaded {clients.Count} DSG Client entities from database.");

        var clientPool = new DSGClientPool();

        foreach (var client in clients)
        {
            List<int> gatewayMessageTypeIds = GetMessageTypeIdsForGateway(client.GatewayId);
            var gatewayMessageTypes = messageTypes.Where(mt => gatewayMessageTypeIds.Contains(mt.Id)).ToList();
            var gatewayClient = gateways.Single(x => x.Id == client.GatewayId);
            clientPool.AddClient(
                gatewayClient,
                gatewayMessageTypes,
                client.StartingSequenceNumber,
                client.EndingSequenceNumber,
                client.HeartbeatIntervalSeconds
            );
        }

        LogHelper.Info("All DSG Clients loaded into the client pool.");
        return clientPool;
    }

    public int AddGateway(Gateway g) => _db.InsertGateway(g);
    public int AddMessageType(MessageType mt) => _db.InsertMessageType(mt);
    public void AddGatewayMessageType(int gId, int mId) => _db.InsertGatewayMessageType(gId, mId);
    public void DeleteAllMasterData() => _db.DeleteAllMasterData();
    public int AddDSGClient(DSGClientEntity dce) => _db.InsertDSGClient(dce);
    private List<int> GetMessageTypeIdsForGateway(int gId) => _db.GetMessageTypeIdsForGateway(gId);
}
