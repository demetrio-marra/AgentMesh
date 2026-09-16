namespace AgentMesh.Services;

internal sealed class SummarizationCallbackContext
{
    public Guid RequestId { get; set; }

    public string? WorkflowStartedCallbackUrl { get; set; }

    public string? WorkflowStepStartedCallbackUrl { get; set; }

    public string? WorkflowStepCompletedCallbackUrl { get; set; }

    public string? WorkflowCompletedCallbackUrl { get; set; }

    public string? WorkflowErrorCallbackUrl { get; set; }

    public bool IsActive => RequestId != Guid.Empty;
}