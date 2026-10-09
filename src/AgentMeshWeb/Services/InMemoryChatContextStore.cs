using System.Collections.Concurrent;
using AgentMeshWeb.Configuration;
using Microsoft.Extensions.Options;

namespace AgentMeshWeb.Services
{
    public sealed class InMemoryChatContextStore(IOptions<ChatContextConfiguration> configuration) : IChatContextStore
    {
        private readonly ConcurrentDictionary<string, StoredContext> _contexts = new();
        private readonly TimeSpan _idleExpiration = TimeSpan.FromMinutes(configuration.Value.IdleExpirationMinutes);

        public Task<ChatContextSnapshot> LoadAsync(string chatId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stored = _contexts.GetOrAdd(chatId, _ => new StoredContext(ChatContextSnapshot.Empty, DateTime.UtcNow));
            if (DateTime.UtcNow - stored.LastAccessUtc > _idleExpiration)
            {
                _contexts.TryRemove(chatId, out _);
                return Task.FromResult(ChatContextSnapshot.Empty);
            }

            _contexts[chatId] = stored with { LastAccessUtc = DateTime.UtcNow };
            return Task.FromResult(stored.Snapshot);
        }

        public Task<ChatContextSnapshot?> SaveAsync(string chatId, long expectedRevision, ChatContextSnapshot context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (true)
            {
                var current = _contexts.GetOrAdd(chatId, _ => new StoredContext(ChatContextSnapshot.Empty, DateTime.UtcNow));
                if (current.Snapshot.Revision != expectedRevision)
                {
                    return Task.FromResult<ChatContextSnapshot?>(null);
                }

                var saved = context with { Revision = expectedRevision + 1, Messages = context.Messages.ToArray() };
                var replacement = new StoredContext(saved, DateTime.UtcNow);
                if (_contexts.TryUpdate(chatId, replacement, current))
                {
                    return Task.FromResult<ChatContextSnapshot?>(saved);
                }
            }
        }

        public Task<ChatContextSnapshot> ClearAsync(string chatId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            while (true)
            {
                var current = _contexts.GetOrAdd(chatId, _ => new StoredContext(ChatContextSnapshot.Empty, DateTime.UtcNow));
                var cleared = ChatContextSnapshot.Empty with { Revision = current.Snapshot.Revision + 1 };
                if (_contexts.TryUpdate(chatId, new StoredContext(cleared, DateTime.UtcNow), current))
                {
                    return Task.FromResult(cleared);
                }
            }
        }

        private sealed record StoredContext(ChatContextSnapshot Snapshot, DateTime LastAccessUtc);
    }
}