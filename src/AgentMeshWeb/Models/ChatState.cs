namespace AgentMeshWeb.Models
{
    public sealed record ChatState(
        IReadOnlyList<ContextMessage> Messages,
        int TokenCount,
        decimal CumulatedCost,
        long Revision);
}