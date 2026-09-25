using System.ComponentModel.DataAnnotations;
using AgentMesh.Models;

namespace AgentMesh.Models.Api
{
    /// <summary>
    /// Input payload for processing a chat or workflow request asynchronously via the API, with optional progress callback URLs.
    /// </summary>
    public sealed class ProcessRequestAsyncApiInput : IValidatableObject
    {
        /// <summary>
        /// The current prompt or question submitted by the user to be processed by the active pipeline.
        /// </summary>
        /// <example>What is the current system status?</example>
        [Required]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional prior conversation messages representing chat history for multi-turn context.
        /// If omitted or empty, the request will be processed as a standalone turn without prior history.
        /// </summary>
        public IEnumerable<ContextMessage>? Conversation { get; set; }

        /// <summary>
        /// Optional callback URL invoked via HTTP POST when the workflow begins executing.
        /// </summary>
        /// <example>http://localhost:5249/callback/workflowStarted</example>
        public string? WorkflowStartedCallbackUrl { get; set; }

        /// <summary>
        /// Optional callback URL invoked via HTTP POST when a workflow step begins executing.
        /// </summary>
        /// <example>http://localhost:5249/callback/workflowStepStarted</example>
        public string? WorkflowStepStartedCallbackUrl { get; set; }

        /// <summary>
        /// Optional callback URL invoked via HTTP POST when a workflow step finishes executing.
        /// </summary>
        /// <example>http://localhost:5249/callback/workflowStepCompleted</example>
        public string? WorkflowStepCompletedCallbackUrl { get; set; }

        /// <summary>
        /// Optional callback URL invoked via HTTP POST when the workflow finishes executing successfully.
        /// </summary>
        /// <example>http://localhost:5249/callback/workflowCompleted</example>
        public string? WorkflowCompletedCallbackUrl { get; set; }

        /// <summary>
        /// Optional callback URL invoked via HTTP POST when the workflow execution fails.
        /// </summary>
        /// <example>http://localhost:5249/callback/workflowError</example>
        public string? WorkflowErrorCallbackUrl { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var callbackUrls = new[]
            {
                WorkflowStartedCallbackUrl,
                WorkflowStepStartedCallbackUrl,
                WorkflowStepCompletedCallbackUrl,
                WorkflowCompletedCallbackUrl,
                WorkflowErrorCallbackUrl
            };

            var suppliedCount = callbackUrls.Count(url => !string.IsNullOrWhiteSpace(url));

            if (suppliedCount != 0 && suppliedCount != callbackUrls.Length)
            {
                yield return new ValidationResult(
                    "All 5 callback URLs (workflowStarted, workflowStepStarted, workflowStepCompleted, workflowCompleted, workflowError) must be supplied together, or none at all.",
                    [
                        nameof(WorkflowStartedCallbackUrl),
                        nameof(WorkflowStepStartedCallbackUrl),
                        nameof(WorkflowStepCompletedCallbackUrl),
                        nameof(WorkflowCompletedCallbackUrl),
                        nameof(WorkflowErrorCallbackUrl)
                    ]);
            }
        }
    }
}
