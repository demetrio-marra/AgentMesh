namespace AgentMesh.Models.Api
{
    /// <summary>
    /// Response payload returned immediately by the asynchronous request endpoint, before the workflow has finished executing.
    /// </summary>
    public sealed class ProcessRequestAsyncApiOutput
    {
        /// <summary>
        /// The generated identifier for this request, also included in every callback payload delivered for it.
        /// </summary>
        public Guid RequestId { get; set; }
    }
}
