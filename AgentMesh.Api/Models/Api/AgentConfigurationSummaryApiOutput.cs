namespace AgentMesh.Models.Api
{
    /// <summary>
    /// Non-sensitive configuration summary for a single configured agent.
    /// </summary>
    public sealed class AgentConfigurationSummaryApiOutput
    {
        /// <summary>
        /// The agent's unique role identifier.
        /// </summary>
        public string AgentRole { get; set; } = string.Empty;

        /// <summary>
        /// The LLM provider model name configured for this agent.
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// The sampling temperature configured for this agent.
        /// </summary>
        public double Temperature { get; set; }
    }
}
