using AgentMesh.Exceptions;
using AgentMesh.Models;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Api.Services;

internal sealed class UnavailableAppInstance(PipelineRoutingException failure) : IAppInstance
{
    public AgentMeshConfiguration GetConfigurationSummary() => throw failure;

    public Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, CancellationToken cancellationToken = default) => throw failure;

    public Guid ProcessRequestAsync(
        string message,
        IEnumerable<ContextMessage>? conversation,
        string? workflowStartedCallbackUrl,
        string? workflowStepStartedCallbackUrl,
        string? workflowStepCompletedCallbackUrl,
        string? workflowCompletedCallbackUrl,
        string? workflowErrorCallbackUrl) => throw failure;

    public Task<SummarizationResult> SummarizeAsync(string summarizationLanguage, IEnumerable<ContextMessage> conversation, CancellationToken cancellationToken = default) => throw failure;

    public Guid SummarizeAsync(
        string summarizationLanguage,
        IEnumerable<ContextMessage> conversation,
        string? workflowStartedCallbackUrl,
        string? workflowStepStartedCallbackUrl,
        string? workflowStepCompletedCallbackUrl,
        string? workflowCompletedCallbackUrl,
        string? workflowErrorCallbackUrl) => throw failure;

    public Guid SummarizeInBackground(
        string summarizationLanguage,
        IEnumerable<ContextMessage> conversation,
        string? workflowStartedCallbackUrl,
        string? workflowStepStartedCallbackUrl,
        string? workflowStepCompletedCallbackUrl,
        string? workflowCompletedCallbackUrl,
        string? workflowErrorCallbackUrl) => throw failure;
}