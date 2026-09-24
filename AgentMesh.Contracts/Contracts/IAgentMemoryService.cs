using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Models;

namespace AgentMesh.Application.Contracts
{
    public interface IAgentMemoryService
    {
        Task AddConversationHistory(string userId, IEnumerable<ContextMessage> conversationHistory, CancellationToken cancellationToken = default);

        Task<IEnumerable<AgentMemoryQueryResultItem>> Query(string userId, string query, CancellationToken cancellationToken = default);
    }
}