using System.ComponentModel.DataAnnotations;
using AgentMesh.Models;

namespace AgentMesh.Runtime.Models.Api
{
    public sealed class SummarizationApiInput
    {
        [Required]
        public string SummarizationLanguage { get; set; } = string.Empty;

        [Required]
        public IEnumerable<ContextMessage>? Conversation { get; set; }
    }
}