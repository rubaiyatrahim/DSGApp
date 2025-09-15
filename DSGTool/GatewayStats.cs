using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSGTool
{
    public class GatewayStats
    {
        private readonly Dictionary<string, int> _messageCounts = new();

        public string GatewayName { get; }
        public int TotalMessages { get; private set; }

        public GatewayStats(string gatewayName)
        {
            GatewayName = gatewayName;
        }

        public void IncrementMessageCount(string msgType)
        {
            TotalMessages++;

            if (_messageCounts.ContainsKey(msgType))
                _messageCounts[msgType]++;
            else
                _messageCounts[msgType] = 1;
        }

        public int GetCount(string msgType)
            => _messageCounts.TryGetValue(msgType, out int count) ? count : 0;

        public IReadOnlyDictionary<string, int> GetAllCounts() => _messageCounts;
    }

}
