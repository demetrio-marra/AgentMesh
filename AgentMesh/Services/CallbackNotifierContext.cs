namespace AgentMesh.Services;

public enum WorkflowExecutionContextKind
{
    Chat,
    Summarization
}

public sealed class CallbackNotifierContext
{
    public Guid RequestId { get; set; }

    public WorkflowExecutionContextKind ExecutionKind { get; set; } = WorkflowExecutionContextKind.Chat;

    public string? WorkflowStartedCallbackUrl { get; set; }

    public string? WorkflowStepStartedCallbackUrl { get; set; }

    public string? WorkflowStepCompletedCallbackUrl { get; set; }

    public string? WorkflowCompletedCallbackUrl { get; set; }

    public string? WorkflowErrorCallbackUrl { get; set; }

    public bool IsActive => RequestId != Guid.Empty;
}