namespace YourApplicationName.Services
{
    public interface IAgentTrackerService
    {
        void AddAgent(string connectionId, string agentName);
        void RemoveAgent(string connectionId);
        Dictionary<string, string> GetActiveAgents();
    }
}
