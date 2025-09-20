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
        var gatewayEntities = _db.GetGateways();
        var messageTypeEntities = _db.GetMessageTypes();
        List<MessageType> messageTypes = messageTypeEntities.Select(GetMessageTypeFromEntity).ToList();
        var clients = _db.GetDSGClients();
        
        var clientPool = new DSGClientPool();

        foreach (var client in clients)
        {
            List<int> gatewayMessageTypeIds = GetMessageTypeIdsForGateway(client.GatewayId);
            var gatewayMessageTypes = messageTypeEntities
                                                .Where(mt => gatewayMessageTypeIds.Contains(mt.Id)).ToList()
                                                .Select(GetMessageTypeFromEntity).ToList();                              ;
            //var gatewayMessageTypes = gatewayMessageTypeEntities.Select(GetMessageTypeFromEntity).ToList(); ;
            clientPool.AddClient(
                GetGatewayFromEntity(gatewayEntities.Single(x => x.Id == client.GatewayId)),
                gatewayMessageTypes,
                client.StartingSequenceNumber,
                client.EndingSequenceNumber,
                client.HeartbeatIntervalSeconds
            );
        }

        return clientPool;
    }

    public int AddGateway(GatewayEntity ge) => _db.InsertGateway(ge);
    public int AddMessageType(MessageTypeEntity mte) => _db.InsertMessageType(mte);
    public void AddGatewayMessageType(int gId, int mId) => _db.InsertGatewayMessageType(gId, mId);
    public void DeleteAllData() => _db.DeleteAllData();
    public int AddDSGClient(DSGClientEntity dce) => _db.InsertDSGClient(dce);
    private Gateway GetGatewayFromEntity(GatewayEntity ge) => new Gateway(ge.PartitionId, ge.EnvironmentName, ge.GatewayName, ge.HostIp, ge.Port, ge.UserName, ge.Password);
    private MessageType GetMessageTypeFromEntity(MessageTypeEntity mte) => new MessageType(mte.Name, mte.MessageId, mte.IsSecMsg);
    private List<int> GetMessageTypeIdsForGateway(int gId) => _db.GetMessageTypeIdsForGateway(gId);
}
