using AgentMesh.Models;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Services
{
    public interface IAppInstance
    {
        AgentMeshConfiguration GetConfigurationSummary();
        Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, CancellationToken cancellationToken = default);
        Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, string? pipelineName, CancellationToken cancellationToken = default);
        Guid ProcessRequestAsync(string message, IEnumerable<ContextMessage>? conversation, string? pipelineName, string? workflowStartedCallbackUrl, string? workflowStepStartedCallbackUrl, string? workflowStepCompletedCallbackUrl, string? workflowCompletedCallbackUrl, string? workflowErrorCallbackUrl);
        Task<SummarizationResult> SummarizeAsync(string summarizationLanguage, IEnumerable<ContextMessage> conversation, CancellationToken cancellationToken = default);
        Guid SummarizeAsync(string summarizationLanguage, IEnumerable<ContextMessage> conversation, string? workflowStartedCallbackUrl, string? workflowStepStartedCallbackUrl, string? workflowStepCompletedCallbackUrl, string? workflowCompletedCallbackUrl, string? workflowErrorCallbackUrl);
        Guid SummarizeInBackground(string summarizationLanguage, IEnumerable<ContextMessage> conversation, string? workflowStartedCallbackUrl, string? workflowStepStartedCallbackUrl, string? workflowStepCompletedCallbackUrl, string? workflowCompletedCallbackUrl, string? workflowErrorCallbackUrl);
    }
}