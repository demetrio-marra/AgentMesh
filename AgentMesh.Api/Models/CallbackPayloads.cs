using AgentMesh.Models;

namespace AgentMesh.Api.Models;

public sealed class WorkflowStartedCallbackPayload
{
    public Guid RequestId { get; set; }
}

public sealed class WorkflowStepStartedCallbackPayload
{
    public Guid RequestId { get; set; }

    public string StepName { get; set; } = string.Empty;

    public IEnumerable<EWDisplayParameterRecord> InputParameters { get; set; } = [];
}

public sealed class WorkflowStepCompletedCallbackPayload
{
    public Guid RequestId { get; set; }

    public string StepName { get; set; } = string.Empty;

    public TimeSpan Elapsed { get; set; }

    public bool IsAgentic { get; set; }

    public IEnumerable<EWDisplayDiffParameterRecord> ParametersDiff { get; set; } = [];
}

public sealed class WorkflowCompletedCallbackPayload
{
    public Guid RequestId { get; set; }

    public object? Result { get; set; }
}

public sealed class WorkflowErrorCallbackPayload
{
    public Guid RequestId { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;
}
