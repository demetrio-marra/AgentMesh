using System.Collections.Concurrent;

namespace AgentMeshWeb.Services;

public sealed class ChatOperationRegistry
{
    private readonly ConcurrentDictionary<string, ChatOperationState> _operations = new();

    public ActiveChatOperation Start(string chatId)
    {
        var state = _operations.GetOrAdd(chatId, _ => new ChatOperationState());
        lock (state.Sync)
        {
            if (state.Active)
            {
                throw new InvalidOperationException("A chat operation is already active.");
            }

            state.Active = true;
            state.Generation++;
            state.Cancellation = new CancellationTokenSource();
            return new ActiveChatOperation(chatId, state.Generation, state.Cancellation.Token);
        }
    }

    public bool IsCurrent(ActiveChatOperation operation)
    {
        if (!_operations.TryGetValue(operation.ChatId, out var state))
        {
            return false;
        }
        lock (state.Sync)
        {
            return state.Active && state.Generation == operation.Generation;
        }
    }

    public void Stop(string chatId) => Invalidate(chatId, keepActive: false);

    public void Reset(string chatId) => Invalidate(chatId, keepActive: false);

    public void Complete(ActiveChatOperation operation)
    {
        if (!_operations.TryGetValue(operation.ChatId, out var state))
        {
            return;
        }
        lock (state.Sync)
        {
            if (state.Generation == operation.Generation)
            {
                state.Active = false;
                state.Cancellation?.Dispose();
                state.Cancellation = null;
            }
        }
    }

    private void Invalidate(string chatId, bool keepActive)
    {
        if (!_operations.TryGetValue(chatId, out var state))
        {
            return;
        }
        lock (state.Sync)
        {
            state.Cancellation?.Cancel();
            state.Active = keepActive;
            state.Generation++;
        }
    }

    private sealed class ChatOperationState
    {
        public object Sync { get; } = new();
        public long Generation { get; set; }
        public bool Active { get; set; }
        public CancellationTokenSource? Cancellation { get; set; }
    }
}

public sealed record ActiveChatOperation(string ChatId, long Generation, CancellationToken CancellationToken);