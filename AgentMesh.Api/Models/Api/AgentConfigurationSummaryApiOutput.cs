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
        /// The inference provider configured for this agent's LLM.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// The configured cost in USD per million input tokens.
        /// </summary>
        public decimal CostPerMillionInputTokens { get; set; }

        /// <summary>
        /// The configured cost in USD per million output tokens.
        /// </summary>
        public decimal CostPerMillionOutputTokens { get; set; }

        /// <summary>
        /// The configured hourly cost in USD, when the model uses hourly pricing.
        /// </summary>
        public decimal? CostPerHour { get; set; }

        /// <summary>
        /// The sampling temperature configured for this agent.
        /// </summary>
        public double Temperature { get; set; }
    }
}
