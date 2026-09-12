using System.ComponentModel.DataAnnotations;
using AgentMesh.Models;

namespace AgentMesh.Models.Api
{
    /// <summary>
    /// Input payload for processing a chat or workflow request via the API.
    /// </summary>
    public sealed class ProcessRequestApiInput
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
    }
}
