using AgentMeshWeb.Models;

namespace AgentMeshWeb.Services;

public interface IAgentMeshApiClient
{
    Task<ConfigurationSummaryApiOutput> GetConfigurationSummaryAsync(CancellationToken cancellationToken);
    Task<WorkflowResult> StreamChatAsync(string message, IReadOnlyList<ContextMessage> conversation, Func<WorkflowProgress, Task> onProgress, CancellationToken cancellationToken);
    Task<SummarizationResult> StreamSummarizationAsync(string language, IReadOnlyList<ContextMessage> conversation, Func<WorkflowProgress, Task> onProgress, CancellationToken cancellationToken);
}