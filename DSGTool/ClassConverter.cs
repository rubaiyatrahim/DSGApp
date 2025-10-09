using DBManager;
using DSGModels.Models;
using DSGClient;

namespace DSGTool
{
    public static class ClassConverter
    {
        public static Gateway ToClass(GatewayEntity gwe)
        {
            return new Gateway(
                gwe.Id,
                gwe.PartitionId,
                gwe.EnvironmentName,
                gwe.GatewayName,
                gwe.Host,
                gwe.Port,
                gwe.Username,
                gwe.Password);
        }
        public static MessageType ToClass(MessageTypeEntity mte)
        {
            return new MessageType(
                mte.Id,
                mte.Name,
                mte.MessageId,
                mte.IsSecMsg);
        }
        public static GatewayEntity ToEntity(Gateway gateway)
        {
            return new GatewayEntity
            (
                gateway.Id,
                gateway.PartitionId,
                gateway.EnvironmentName,
                gateway.GatewayName,
                gateway.Host,
                gateway.Port,
                gateway.Username,
                gateway.Password
            );
        }
        public static MessageTypeEntity ToEntity(MessageType messageType)
        {
            return new MessageTypeEntity
            (
                messageType.Id,
                messageType.Name,
                messageType.MessageId,
                messageType.IsSecMsg
            );
        }
    }
}
