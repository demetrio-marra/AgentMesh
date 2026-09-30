using AgentMeshWeb.Models;

namespace AgentMeshWeb.Services;

public interface IChatContextStore
{
    Task<ChatContextSnapshot> LoadAsync(string chatId, CancellationToken cancellationToken);
    Task<ChatContextSnapshot?> SaveAsync(string chatId, long expectedRevision, ChatContextSnapshot context, CancellationToken cancellationToken);
    Task<ChatContextSnapshot> ClearAsync(string chatId, CancellationToken cancellationToken);
}

public sealed record ChatContextSnapshot(
    long Revision,
    IReadOnlyList<ContextMessage> Messages,
    int TokenCount,
    decimal CumulatedCost)
{
    public static readonly ChatContextSnapshot Empty = new(0, [], 0, 0);
}