using AgentMesh.Contracts.Models.Knowledge;

namespace AgentMesh.Services
{
    public interface IKnowledgeService
    {
        Task<KnowledgeQueryResult> QueryKnowledgeAsync(KnowledgeQuery query, CancellationToken cancellationToken = default);
    }
}
