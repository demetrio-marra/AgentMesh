using System.Globalization;
using AgentMesh.Application.Configuration;
using AgentMesh.Authentication;
using AgentMesh.Application.Services;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        SESJSSandboxConfiguration sesJSSandboxConfiguration,
        UserConfiguration userConfiguration,
        IEnumerable<AgentFlatConfigurationRecord> agentsConfigurations) : ControllerBase
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
            var agents = agentsConfigurations
                .Select(agentConfig => new AgentConfigurationSummaryApiOutput
                {
                    AgentRole = agentConfig.AgentUniqueRole,
                    Model = agentConfig.ProviderModelName,
                    Temperature = Convert.ToDouble(agentConfig.Temperature, CultureInfo.InvariantCulture)
                })
                .ToList();

            return Ok(new ConfigurationSummaryApiOutput
            {
                SandboxServiceUrl = sesJSSandboxConfiguration.SandboxServiceURL,
                SandboxName = sesJSSandboxConfiguration.SandboxName,
                AgentId = userConfiguration.AgentId,
                Agents = agents
            });
        }
    }
}
