using AgentMesh.Authentication;
using AgentMesh.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentMesh.Controllers
{
    /// <summary>
    /// Controller exposing a read-only summary of sandbox and agent configuration, equivalent to the CLI startup printout.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]
    public sealed class ConfigurationController(
        IAppInstance appInstance) : ControllerBase
    {
        /// <summary>
        /// Retrieve the current sandbox and agent configuration summary.
        /// </summary>
        /// <remarks>
        /// Returns the same non-sensitive fields printed by the CLI startup summary: sandbox URL/name, agent id, and per-agent role/model/temperature.
        /// Provider API keys and system prompts are never included.
        /// </remarks>
        /// <response code="200">The configuration summary was successfully retrieved.</response>
        /// <response code="401">The API key is missing or invalid.</response>
        [Tags("Configuration")]
        [HttpGet("configuration")]
        [ProducesResponseType(typeof(ConfigurationSummaryApiOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<ConfigurationSummaryApiOutput> Get()
        {
            var configurationSummary = appInstance.GetConfigurationSummary();
            var agents = configurationSummary.Agents
                .Select(agent => new AgentConfigurationSummaryApiOutput
                {
                    AgentRole = agent.AgentRole,
                    Model = agent.Model,
                    Provider = agent.Provider,
                    CostPerMillionInputTokens = agent.CostPerMillionInputTokens,
                    CostPerMillionOutputTokens = agent.CostPerMillionOutputTokens,
                    CostPerHour = agent.CostPerHour,
                    Temperature = agent.Temperature
                })
                .ToList();

            return Ok(new ConfigurationSummaryApiOutput
            {
                SandboxServiceUrl = configurationSummary.SandboxServiceUrl,
                SandboxName = configurationSummary.SandboxName,
                AgentId = configurationSummary.AgentId,
                Agents = agents
            });
        }
    }
}
