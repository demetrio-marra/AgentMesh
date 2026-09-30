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
    public IReadOnlyList<EWStepStatisticsRecord> MainPipelineStepsData { get; init; } = [];
    public IReadOnlyList<AgentExecutionCost> AgentsCostData { get; init; } = [];
    public int CountOfMessages { get; init; }
    public int CountOfTokens { get; init; }
    public decimal CumulatedCost { get; init; }
}

public sealed class EWStepStatisticsRecord
{
    public string StepName { get; init; } = string.Empty;
    public DateTime StartedOnUtc { get; init; }
    public DateTime CompletedOnUtc { get; init; }
    public IReadOnlyList<EWDisplayParameterRecord> ParametersBefore { get; init; } = [];
    public IReadOnlyList<EWDisplayParameterRecord> InputParameters { get; init; } = [];
    public IReadOnlyList<EWDisplayParameterRecord> ParametersAfter { get; init; } = [];
    public bool IsAgentic { get; init; }
    public string? AgentName { get; init; }
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
}

public sealed class EWDisplayParameterRecord
{
    public string Name { get; init; } = string.Empty;
    public string? Value { get; init; }
}

public sealed class AgentExecutionCost
{
    public string AgentName { get; init; } = string.Empty;
    public decimal CostPerMillionInputTokens { get; init; }
    public decimal CostPerMillionOutputTokens { get; init; }
    public int ConsumedInputTokens { get; init; }
    public int ConsumedOutputTokens { get; init; }
    public decimal? CostPerHour { get; init; }
    public TimeSpan Elapsed { get; init; }
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

public sealed record WorkflowProgress(string Kind, string Message, string EventType, string RawData);
public sealed record SummarizationResult(string Content, DateTime CreatedAt);