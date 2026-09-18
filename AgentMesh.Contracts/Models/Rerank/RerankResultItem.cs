namespace AgentMesh.Models.Rerank
{
    public readonly record struct RerankResultItem(
        int DocumentIndex,
        string Document,
        double Score
    );
}
