using System.Collections.Concurrent;
using AgentMesh.Models;

namespace AgentMesh.Services;

internal sealed class PendingRequestRegistry
{
    private readonly ConcurrentDictionary<Guid, PendingRequest> _requests = new();
    private readonly ConcurrentDictionary<Guid, CallbackResult> _earlyResults = new();
    private readonly ConcurrentDictionary<Guid, byte> _abandonedRequests = new();

    public PendingRequest Register(Guid requestId, bool isSummarization)
    {
        var request = new PendingRequest(isSummarization);
        if (_earlyResults.TryRemove(requestId, out var earlyResult))
        {
            request.Completion.TrySetResult(earlyResult);
            return request;
        }

        if (!_requests.TryAdd(requestId, request))
        {
            throw new InvalidOperationException($"Request {requestId} is already pending.");
        }

        return request;
    }

    public bool IsActive(Guid requestId) => _requests.ContainsKey(requestId);

    public bool TryComplete(Guid requestId, CallbackResult result)
    {
        if (_abandonedRequests.ContainsKey(requestId))
        {
            return false;
        }

        if (_requests.TryRemove(requestId, out var request))
        {
            request.Completion.TrySetResult(result);
            return true;
        }

        _earlyResults.TryAdd(requestId, result);
        return false;
    }

    public bool TryAbandon(Guid requestId)
    {
        if (!_requests.TryRemove(requestId, out var request))
        {
            _abandonedRequests.TryAdd(requestId, 0);
            _earlyResults.TryRemove(requestId, out _);
            return false;
        }

        _abandonedRequests.TryAdd(requestId, 0);
        request.Completion.TrySetCanceled();
        return true;
    }
}

internal sealed class PendingRequest(bool isSummarization)
{
    public bool IsSummarization { get; } = isSummarization;
    public TaskCompletionSource<CallbackResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed class CallbackResult
{
    public WorkflowResult? WorkflowResult { get; init; }
    public SummarizationCompletedCallbackPayload? SummarizationResult { get; init; }
    public string? ErrorMessage { get; init; }
}
