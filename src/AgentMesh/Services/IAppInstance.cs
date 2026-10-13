using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IAppInstance
    {
        AgentMeshConfiguration GetConfigurationSummary();
        Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, CancellationToken cancellationToken = default);
        Task<WorkflowResult> ProcessRequestStreamAsync(
            string message,
            IEnumerable<ContextMessage>? conversation,
            Func<Guid, Task> streamReady,
            Func<string, object, CancellationToken, Task> streamEventSink,
            CancellationToken cancellationToken = default);
        Guid ProcessRequestAsync(string message, IEnumerable<ContextMessage>? conversation, string? workflowStartedCallbackUrl, string? workflowStepStartedCallbackUrl, string? workflowStepCompletedCallbackUrl, string? workflowCompletedCallbackUrl, string? workflowErrorCallbackUrl);
        Task<SummarizationResult> SummarizeAsync(string summarizationLanguage, IEnumerable<ContextMessage> conversation, CancellationToken cancellationToken = default);
        Task<SummarizationResult> SummarizeStreamAsync(
            string summarizationLanguage,
            IEnumerable<ContextMessage> conversation,
            Func<Guid, Task> streamReady,
            Func<string, object, CancellationToken, Task> streamEventSink,
            CancellationToken cancellationToken = default);
        Guid SummarizeAsync(string summarizationLanguage, IEnumerable<ContextMessage> conversation, string? workflowStartedCallbackUrl, string? workflowStepStartedCallbackUrl, string? workflowStepCompletedCallbackUrl, string? workflowCompletedCallbackUrl, string? workflowErrorCallbackUrl);
        Guid SummarizeInBackground(string summarizationLanguage, IEnumerable<ContextMessage> conversation, string? workflowStartedCallbackUrl, string? workflowStepStartedCallbackUrl, string? workflowStepCompletedCallbackUrl, string? workflowCompletedCallbackUrl, string? workflowErrorCallbackUrl);
    }
}