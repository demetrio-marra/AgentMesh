using AgentMesh.Models.Workflows;

namespace AgentMesh.Api.Models.Api
{
    /// <summary>
    /// Response payload returned by the synchronous request endpoints, carrying the generated request identifier alongside the workflow result.
    /// </summary>
    public sealed class ProcessRequestApiOutput
    {
        /// <summary>
        /// The generated identifier for this request.
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// The comprehensive result produced by executing the pipeline workflow.
        /// </summary>
        public WorkflowResult WorkflowResult { get; set; }
    }
}
