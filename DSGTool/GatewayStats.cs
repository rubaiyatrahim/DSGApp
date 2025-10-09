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
        private readonly Dictionary<string, long> _messageCountsDB = new();
        private readonly Dictionary<string, long> _lastSeqNumbers = new();

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

        public void ResetCounts()
        {
            TotalMessages = 0;
            _messageCounts.Clear();
            _messageCountsDB.Clear();
        }

        public void SetMessageCountDB(string messageId, long messageCount)
        {
            _messageCountsDB[messageId] = messageCount;
        }

        public void SetLastSeqNumber(string messageId, long seqNumber)
        {
            _lastSeqNumbers[messageId] = seqNumber;
        }

        public int GetCount(string msgType)
            => _messageCounts.TryGetValue(msgType, out int count) ? count : 0;

        public long GetCountDB(string msgType)
            => _messageCountsDB.TryGetValue(msgType, out long count) ? count : 0;

        public long GetLastSeqNumber(string messageId)
            => _lastSeqNumbers.TryGetValue(messageId, out long seqNumber) ? seqNumber : 0;

        public IReadOnlyDictionary<string, int> GetAllCounts() => _messageCounts;
        public IReadOnlyDictionary<string, long> GetAllCountsDB() => _messageCountsDB;
        public IReadOnlyDictionary<string, long> GetAllLastSeqNumbers() => _lastSeqNumbers;
    }

}
