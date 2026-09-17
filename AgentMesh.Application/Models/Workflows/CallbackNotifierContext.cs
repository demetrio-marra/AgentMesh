namespace AgentMesh.Application.Models.Workflows
{
    /// <summary>
    /// Discriminates the kind of execution for scoped workflow notifications and callbacks.
    /// </summary>
    public enum WorkflowExecutionContextKind
    {
        Chat,
        Summarization
    }

    /// <summary>
    /// Scoped per-request state associating a generated request id and optional progress callback URLs with the current DI scope.
    /// </summary>
    public sealed class CallbackNotifierContext
    {
        /// <summary>
        /// The generated identifier for the request being processed in this scope.
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// The execution kind (Chat or Summarization) for this scope.
        /// </summary>
        public WorkflowExecutionContextKind ExecutionKind { get; set; } = WorkflowExecutionContextKind.Chat;

        /// <summary>
        /// Callback URL invoked via HTTP POST when the workflow begins executing, or null if not configured.
        /// </summary>
        public string? WorkflowStartedCallbackUrl { get; set; }

        /// <summary>
        /// Callback URL invoked via HTTP POST when a workflow step begins executing, or null if not configured.
        /// </summary>
        public string? WorkflowStepStartedCallbackUrl { get; set; }

        /// <summary>
        /// Callback URL invoked via HTTP POST when a workflow step finishes executing, or null if not configured.
        /// </summary>
        public string? WorkflowStepCompletedCallbackUrl { get; set; }

        /// <summary>
        /// Callback URL invoked via HTTP POST when the workflow finishes executing successfully, or null if not configured.
        /// </summary>
        public string? WorkflowCompletedCallbackUrl { get; set; }

        /// <summary>
        /// Callback URL invoked via HTTP POST when the workflow execution fails, or null if not configured.
        /// </summary>
        public string? WorkflowErrorCallbackUrl { get; set; }

        /// <summary>
        /// Whether this context is active with an assigned request id.
        /// </summary>
        public bool IsActive => RequestId != Guid.Empty;
    }
}
