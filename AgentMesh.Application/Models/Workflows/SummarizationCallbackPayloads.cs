using AgentMesh.Models;

namespace AgentMesh.Application.Models.Workflows;

/// <summary>
/// Payload delivered to the workflowStarted callback URL for summarization requests.
/// </summary>
public sealed class SummarizationStartedCallbackPayload
{
    /// <summary>
    /// The identifier of the request this event belongs to.
    /// </summary>
    public Guid RequestId { get; set; }
}

/// <summary>
/// Payload delivered to the workflowStepStarted callback URL for summarization requests.
/// </summary>
public sealed class SummarizationStepStartedCallbackPayload
{
    /// <summary>
    /// The identifier of the request this event belongs to.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// The name of the workflow step that started executing.
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// The parameters consumed as input by the step.
    /// </summary>
    public IEnumerable<EWDisplayParameterRecord> InputParameters { get; set; } = [];
}

/// <summary>
/// Payload delivered to the workflowStepCompleted callback URL for summarization requests.
/// </summary>
public sealed class SummarizationStepCompletedCallbackPayload
{
    /// <summary>
    /// The identifier of the request this event belongs to.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// The name of the workflow step that finished executing.
    /// </summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Elapsed execution time for the step.
    /// </summary>
    public TimeSpan Elapsed { get; set; }

    /// <summary>
    /// Whether the step invoked an AI agent.
    /// </summary>
    public bool IsAgentic { get; set; }

    /// <summary>
    /// Parameter differences observed before and after the step's execution.
    /// </summary>
    public IEnumerable<EWDisplayDiffParameterRecord> ParametersDiff { get; set; } = [];
}

/// <summary>
/// Payload delivered to the workflowCompleted callback URL for summarization requests.
/// </summary>
public sealed class SummarizationCompletedCallbackPayload
{
    /// <summary>
    /// The identifier of the request this event belongs to.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// The summarized text produced by the summarization pipeline.
    /// </summary>
    public string SummarizedContent { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp associated with the summarized content.
    /// </summary>
    public DateTime SummarizedContentDatetime { get; set; }
}

/// <summary>
/// Payload delivered to the workflowError callback URL for summarization requests.
/// </summary>
public sealed class SummarizationErrorCallbackPayload
{
    /// <summary>
    /// The identifier of the request this event belongs to.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// The error message describing why summarization execution failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
