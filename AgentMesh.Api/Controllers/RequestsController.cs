using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Authentication;
using AgentMesh.Models;
using AgentMesh.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentMesh.Controllers
{
    /// <summary>
    /// Controller for executing AI workflow requests across registered pipelines.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]
    public sealed class RequestsController(StatelessAppInstance appInstance) : ControllerBase
    {
        /// <summary>
        /// Process a chat request using the default pipeline.
        /// </summary>
        /// <remarks>
        /// Executes the request against the single loaded pipeline.
        /// When multiple pipelines are loaded, this endpoint returns a 400 Bad Request requiring a specific pipeline name route.
        /// </remarks>
        /// <param name="request">The chat request payload containing the user message and optional conversation history.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The request was successfully processed and the workflow result is returned.</response>
        /// <response code="400">Multiple pipelines are loaded; explicit pipeline routing via /api/pipelines/{pipelineName}/requests is required.</response>
        /// <response code="401">The API key is missing or invalid.</response>
        /// <response code="503">No pipelines are loaded or a plugin configuration error exists.</response>
        [HttpPost("requests")]
        [ProducesResponseType(typeof(WorkflowResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WorkflowResult>> PostDefault([FromBody] ProcessRequestApiInput request, CancellationToken cancellationToken)
        {
            return await ExecuteRequest(request.Message, request.Conversation, pipelineName: null, cancellationToken);
        }

        /// <summary>
        /// Process a chat request using a specific named pipeline.
        /// </summary>
        /// <remarks>
        /// Executes the request against the named pipeline specified in the route (matched case-insensitively).
        /// </remarks>
        /// <param name="pipelineName">The unique name of the target pipeline to execute.</param>
        /// <param name="request">The chat request payload containing the user message and optional conversation history.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The request was successfully processed by the specified pipeline.</response>
        /// <response code="401">The API key is missing or invalid.</response>
        /// <response code="404">No pipeline with the specified name was found.</response>
        /// <response code="503">A plugin configuration issue prevents pipeline execution.</response>
        [HttpPost("pipelines/{pipelineName}/requests")]
        [ProducesResponseType(typeof(WorkflowResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<WorkflowResult>> PostNamed([FromRoute] string pipelineName, [FromBody] ProcessRequestApiInput request, CancellationToken cancellationToken)
        {
            return await ExecuteRequest(request.Message, request.Conversation, pipelineName, cancellationToken);
        }

        private async Task<ActionResult<WorkflowResult>> ExecuteRequest(string message, IEnumerable<ContextMessage>? conversation, string? pipelineName, CancellationToken cancellationToken)
        {
            try
            {
                var result = await appInstance.ProcessRequest(message, conversation, pipelineName, cancellationToken);
                return Ok(result);
            }
            catch (PipelineRoutingException ex)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = ex.StatusCode,
                    Title = ex.Title,
                    Detail = ex.Detail
                };

                return StatusCode(ex.StatusCode, problemDetails);
            }
        }
    }
}