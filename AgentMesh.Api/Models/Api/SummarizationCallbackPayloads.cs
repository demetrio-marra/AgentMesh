using AgentMesh.Models;

namespace AgentMesh.Models.Api;

public sealed class SummarizationStartedCallbackPayload
{
    public Guid RequestId { get; set; }
}

public sealed class SummarizationStepStartedCallbackPayload
{
    public Guid RequestId { get; set; }

    public string StepName { get; set; } = string.Empty;

    public IEnumerable<EWDisplayParameterRecord> InputParameters { get; set; } = [];
}

public sealed class SummarizationStepCompletedCallbackPayload
{
    public Guid RequestId { get; set; }

    public string StepName { get; set; } = string.Empty;

    public TimeSpan Elapsed { get; set; }

    public bool IsAgentic { get; set; }

    public IEnumerable<EWDisplayDiffParameterRecord> ParametersDiff { get; set; } = [];
}

public sealed class SummarizationCompletedCallbackPayload
{
    public Guid RequestId { get; set; }

    public string SummarizedContent { get; set; } = string.Empty;

    public DateTime SummarizedContentDatetime { get; set; }
}

public sealed class SummarizationErrorCallbackPayload
{
    public Guid RequestId { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;
}