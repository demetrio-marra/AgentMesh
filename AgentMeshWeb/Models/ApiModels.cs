using System.Text.Json.Serialization;

namespace AgentMeshWeb.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatMessageRole
{
    User,
    Assistant
}

public sealed record ContextMessage(ChatMessageRole Role, DateTime Date, string Text);

public sealed class ProcessRequestApiInput
{
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<ContextMessage> Conversation { get; init; } = [];
}

public sealed class SummarizationApiInput
{
    public string SummarizationLanguage { get; init; } = string.Empty;
    public IReadOnlyList<ContextMessage> Conversation { get; init; } = [];
}

public sealed class WorkflowResult
{
    public string Message { get; init; } = string.Empty;
    public int CountOfMessages { get; init; }
    public int CountOfTokens { get; init; }
    public decimal CumulatedCost { get; init; }
}

public sealed class ConfigurationSummaryApiOutput
{
    public string SandboxServiceUrl { get; init; } = string.Empty;
    public string SandboxName { get; init; } = string.Empty;
    public string AgentId { get; init; } = string.Empty;
    public IReadOnlyList<AgentConfigurationSummaryApiOutput> Agents { get; init; } = [];
}

public sealed class AgentConfigurationSummaryApiOutput
{
    public string AgentRole { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public double Temperature { get; init; }
}

public sealed record WorkflowProgress(string Kind, string Message);
public sealed record SummarizationResult(string Content, DateTime CreatedAt);