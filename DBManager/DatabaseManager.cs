using DBManager.Repositories;
using DBManager.Repository;
using DBManager.Xml;
using Microsoft.Data.SqlClient;

namespace DBManager
{
    public class DatabaseManager : IDisposable
    {
        public GatewayRepository Gateways { get; }
        public MessageTypeRepository MessageTypes { get; }
        public GatewayMessageTypeRepository GatewayMessageTypes { get; }
        public DSGClientRepository DSGClients { get; }
        public StoredProcedureExecutor Procedures { get; }
        public XmlMessageLoader XmlLoader { get; }

        private readonly string _connectionString;

        public DatabaseManager(string connectionString)
        {
            _connectionString = connectionString;
            Gateways = new GatewayRepository(connectionString);
            MessageTypes = new MessageTypeRepository(connectionString);
            GatewayMessageTypes = new GatewayMessageTypeRepository(connectionString);
            DSGClients = new DSGClientRepository(connectionString);
            Procedures = new StoredProcedureExecutor(connectionString);
            XmlLoader = new XmlMessageLoader(connectionString);
        }

        public void Dispose()
        {
            XmlLoader.Dispose();
        }
    }
}
