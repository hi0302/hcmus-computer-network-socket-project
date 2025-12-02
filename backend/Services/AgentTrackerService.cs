using YourApplicationName.Services;
using System.Collections.Concurrent;

namespace YourApplicationName.Services
{
    public class AgentTrackerService : IAgentTrackerService
    {
        // 💡 Dùng ConcurrentDictionary để quản lý danh sách Agent (thread-safe)
        private readonly ConcurrentDictionary<string, string> _activeAgents = new ConcurrentDictionary<string, string>();

        public void AddAgent(string connectionId, string agentName)
        {
            _activeAgents.TryAdd(connectionId, agentName);
        }

        public void RemoveAgent(string connectionId)
        {
            _activeAgents.TryRemove(connectionId, out _);
        }

        public Dictionary<string, string> GetActiveAgents()
        {
            // Trả về một bản sao của danh sách
            return new Dictionary<string, string>(_activeAgents);
        }
    }
}
