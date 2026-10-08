using AgentMesh.Contracts.Models.AgentMemory;
using AgentMesh.Models;

namespace AgentMesh.Contracts
{
    public interface IAgentMemoryService
    {
        Task AddConversationHistory(string userId, IEnumerable<ContextMessage> conversationHistory, CancellationToken cancellationToken = default);

        Task<IEnumerable<AgentMemoryQueryResultItem>> Query(string userId, string query, CancellationToken cancellationToken = default);
    }
}