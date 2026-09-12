using AgentMesh.Models.Rerank;

namespace AgentMesh.Services
{
    public interface IRerankerService
    {
        Task<RerankResult> RerankAsync(RerankInputQuery inputQuery, CancellationToken cancellationToken = default);
    }
}
