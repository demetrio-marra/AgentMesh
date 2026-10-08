namespace AgentMesh.Application.Models
{
    public class EWAgenticStepExecutionResult : EWStepExecutionResult
    {
        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }
    }
}
