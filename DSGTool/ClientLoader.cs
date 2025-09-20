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
    }

    public DSGClientPool LoadClients()
    {
        var gateways = _db.GetGateways();
        var messageTypes = _db.GetMessageTypes();
        var clients = _db.GetDSGClients();
        
        var clientPool = new DSGClientPool();

        foreach (var client in clients)
        {
            List<int> gatewayMessageTypeIds = GetMessageTypeIdsForGateway(client.GatewayId);
            var gatewayMessageTypes = messageTypes.Where(mt => gatewayMessageTypeIds.Contains(mt.Id)).ToList();
            clientPool.AddClient(
                gateways.Single(x => x.Id == client.GatewayId),
                gatewayMessageTypes,
                client.StartingSequenceNumber,
                client.EndingSequenceNumber,
                client.HeartbeatIntervalSeconds
            );
        }

        return clientPool;
    }

    public int AddGateway(Gateway g) => _db.InsertGateway(g);
    public int AddMessageType(MessageType mt) => _db.InsertMessageType(mt);
    public void AddGatewayMessageType(int gId, int mId) => _db.InsertGatewayMessageType(gId, mId);
    public void DeleteAllData() => _db.DeleteAllData();
    public int AddDSGClient(DSGClientEntity dce) => _db.InsertDSGClient(dce);
    private List<int> GetMessageTypeIdsForGateway(int gId) => _db.GetMessageTypeIdsForGateway(gId);
}
