using AgentMesh.Models;
using AgentMesh.Models.Workflows;

namespace AgentMesh.Application.Models.Workflows
{
    /// <summary>
    /// Payload delivered to the workflowStarted callback URL.
    /// </summary>
    public sealed class WorkflowStartedCallbackPayload
    {
        /// <summary>
        /// The identifier of the request this event belongs to.
        /// </summary>
        public Guid RequestId { get; set; }
    }

    /// <summary>
    /// Payload delivered to the workflowStepStarted callback URL.
    /// </summary>
    public sealed class WorkflowStepStartedCallbackPayload
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
    /// Payload delivered to the workflowStepCompleted callback URL.
    /// </summary>
    public sealed class WorkflowStepCompletedCallbackPayload
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
    /// Payload delivered to the workflowCompleted callback URL.
    /// </summary>
    public sealed class WorkflowCompletedCallbackPayload
    {
        /// <summary>
        /// The identifier of the request this event belongs to.
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// The final result produced by the workflow.
        /// </summary>
        public WorkflowResult Result { get; set; }
    }

    /// <summary>
    /// Payload delivered to the workflowError callback URL.
    /// </summary>
    public sealed class WorkflowErrorCallbackPayload
    {
        /// <summary>
        /// The identifier of the request this event belongs to.
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// The error message describing why the workflow execution failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
