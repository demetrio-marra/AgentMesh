namespace AgentMesh.Models.Api
{
    /// <summary>
    /// Structured configuration summary equivalent to the CLI startup printout: sandbox and per-agent configuration.
    /// </summary>
    public sealed class ConfigurationSummaryApiOutput
    {
        /// <summary>
        /// The URL of the configured JS sandbox service.
        /// </summary>
        public string SandboxServiceUrl { get; set; } = string.Empty;

        /// <summary>
        /// The name of the configured JS sandbox.
        /// </summary>
        public string SandboxName { get; set; } = string.Empty;

        /// <summary>
        /// The configured user agent identifier.
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// The configuration summary for each registered agent.
        /// </summary>
        public IReadOnlyList<AgentConfigurationSummaryApiOutput> Agents { get; set; } = Array.Empty<AgentConfigurationSummaryApiOutput>();
    }
}
