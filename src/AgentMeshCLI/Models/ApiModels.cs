using System.Text.Json.Serialization;

namespace AgentMeshCLI.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContextMessageRole
    {
        User,
        Assistant
    }

    public sealed class ContextMessage
    {
        public ContextMessageRole Role { get; set; }
        public DateTime Date { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class ProcessRequestApiInput
    {
        public string Message { get; set; } = string.Empty;
        public IEnumerable<ContextMessage>? Conversation { get; set; }
    }

    public sealed class ProcessRequestApiOutput
    {
        public Guid RequestId { get; set; }
        public WorkflowResult WorkflowResult { get; set; } = new();
    }

    public class SummarizationApiInput
    {
        public string SummarizationLanguage { get; set; } = string.Empty;
        public IEnumerable<ContextMessage> Conversation { get; set; } = [];
    }

    public sealed class ConfigurationSummaryApiOutput
    {
        public string SandboxServiceUrl { get; set; } = string.Empty;
        public string SandboxName { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public IReadOnlyList<AgentConfigurationSummaryApiOutput> Agents { get; set; } = [];
    }

    public sealed class AgentConfigurationSummaryApiOutput
    {
        public string AgentRole { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public decimal CostPerMillionInputTokens { get; set; }
        public decimal CostPerMillionOutputTokens { get; set; }
        public decimal? CostPerHour { get; set; }
        public double Temperature { get; set; }
    }

    public sealed class WorkflowResult
    {
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<EWStepStatisticsRecord> MainPipelineStepsData { get; set; } = [];
        public IReadOnlyList<AgentExecutionCost> AgentsCostData { get; set; } = [];
        public int CountOfMessages { get; set; }
        public int CountOfTokens { get; set; }
        public bool ContextSummarizerHasRun { get; set; }
        public int? CountOfMessagesBeforeSummarization { get; set; }
        public int? CountOfTokensBeforeSummarization { get; set; }
        public decimal CumulatedCost { get; set; }
    }

    public sealed class EWStepStatisticsRecord
    {
        public string StepName { get; set; } = string.Empty;
        public DateTime StartedOnUtc { get; set; }
        public DateTime CompletedOnUtc { get; set; }
        public IReadOnlyList<EWDisplayParameterRecord> ParametersBefore { get; set; } = [];
        public IReadOnlyList<EWDisplayParameterRecord> InputParameters { get; set; } = [];
        public IReadOnlyList<EWDisplayParameterRecord> ParametersAfter { get; set; } = [];
        public bool IsAgentic { get; set; }
        public string? AgentName { get; set; }
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }
        public TimeSpan Elapsed => CompletedOnUtc - StartedOnUtc;
        public string HumanReadableElapsed => Elapsed.TotalSeconds < 1 ? "<1s" : $"{Elapsed.Hours}h {Elapsed.Minutes}m {Elapsed.Seconds}s".Trim();
        public IEnumerable<EWDisplayDiffParameterRecord> ParametersDiff => ParametersAfter
            .Select(after => new EWDisplayDiffParameterRecord
            {
                Name = after.Name,
                OldValue = ParametersBefore.Where(before => before.Name == after.Name).Select(before => before.Value).FirstOrDefault(),
                NewValue = after.Value
            })
            .Where(diff => diff.OldValue != diff.NewValue);
    }

    public sealed class EWDisplayParameterRecord
    {
        public string Name { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public sealed class EWDisplayDiffParameterRecord
    {
        public string Name { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }

    public sealed class AgentExecutionCost
    {
        public string AgentName { get; set; } = string.Empty;
        public decimal CostPerMillionInputTokens { get; set; }
        public decimal CostPerMillionOutputTokens { get; set; }
        public int ConsumedInputTokens { get; set; }
        public int ConsumedOutputTokens { get; set; }
        public decimal? CostPerHour { get; set; }
        public TimeSpan Elapsed { get; set; }
        [JsonIgnore] public bool IsHourlyCost => CostPerHour.HasValue;
        [JsonIgnore] public decimal InputCost => IsHourlyCost ? 0 : ConsumedInputTokens / 1_000_000m * CostPerMillionInputTokens;
        [JsonIgnore] public decimal OutputCost => IsHourlyCost ? 0 : ConsumedOutputTokens / 1_000_000m * CostPerMillionOutputTokens;
        [JsonIgnore] public decimal HourlyCost => IsHourlyCost ? (decimal)Elapsed.TotalHours * CostPerHour!.Value : 0;
        [JsonIgnore] public decimal TotalCost => IsHourlyCost ? HourlyCost : InputCost + OutputCost;
    }

    public sealed class SummarizationCompletedCallbackPayload { public Guid RequestId { get; set; } public string SummarizedContent { get; set; } = string.Empty; public DateTime SummarizedContentDatetime { get; set; } }
}
