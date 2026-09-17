using AgentMesh.Api.Models.Api;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Authentication;
using AgentMesh.Models;
using AgentMesh.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentMesh.Controllers
{
    /// <summary>
    /// Controller for executing AI workflow requests across registered pipelines.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]
    public sealed class RequestsController(
        IAppInstance appInstance) : ControllerBase
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
        /// <response code="200">The request was successfully processed and the workflow result is returned, alongside a generated request id.</response>
        /// <response code="400">Multiple pipelines are loaded; explicit pipeline routing via /api/pipelines/{pipelineName}/requests is required.</response>
        /// <response code="401">The API key is missing or invalid.</response>
        /// <response code="503">No pipelines are loaded or a plugin configuration error exists.</response>
        [Tags("Requests")]
        [HttpPost("requests")]
        [ProducesResponseType(typeof(ProcessRequestApiOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ProcessRequestApiOutput>> PostDefault([FromBody] ProcessRequestApiInput request, CancellationToken cancellationToken)
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
        /// <response code="200">The request was successfully processed by the specified pipeline, alongside a generated request id.</response>
        /// <response code="401">The API key is missing or invalid.</response>
        /// <response code="404">No pipeline with the specified name was found.</response>
        /// <response code="503">A plugin configuration issue prevents pipeline execution.</response>
        [Tags("Requests")]
        [HttpPost("pipelines/{pipelineName}/requests")]
        [ProducesResponseType(typeof(ProcessRequestApiOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ProcessRequestApiOutput>> PostNamed([FromRoute] string pipelineName, [FromBody] ProcessRequestApiInput request, CancellationToken cancellationToken)
        {
            return await ExecuteRequest(request.Message, request.Conversation, pipelineName, cancellationToken);
        }

        /// <summary>
        /// Process a chat request asynchronously using the default pipeline.
        /// </summary>
        /// <remarks>
        /// Returns immediately with a generated request id, without waiting for the workflow to complete.
        /// Progress is delivered to the 5 optional callback URLs (workflowStarted, workflowStepStarted, workflowStepCompleted, workflowCompleted, workflowError), which must be supplied all together or not at all.
        /// </remarks>
        /// <param name="request">The chat request payload containing the user message, optional conversation history, and optional callback URLs.</param>
        /// <response code="202">The request was accepted; the workflow is running in the background and progress will be delivered to the supplied callback URLs.</response>
        /// <response code="400">The callback URLs were partially supplied (1-4 of 5), or multiple pipelines are loaded and an explicit pipeline name is required.</response>
        /// <response code="401">The API key is missing or invalid.</response>
        /// <response code="503">No pipelines are loaded or a plugin configuration error exists.</response>
        [Tags("Requests")]
        [HttpPost("requests/async")]
        [ProducesResponseType(typeof(ProcessRequestAsyncApiOutput), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<ProcessRequestAsyncApiOutput> PostDefaultAsync([FromBody] ProcessRequestAsyncApiInput request)
        {
            return ExecuteRequestAsync(request, pipelineName: null);
        }

        /// <summary>
        /// Process a chat request asynchronously using a specific named pipeline.
        /// </summary>
        /// <remarks>
        /// Returns immediately with a generated request id, without waiting for the workflow to complete.
        /// Progress is delivered to the 5 optional callback URLs (workflowStarted, workflowStepStarted, workflowStepCompleted, workflowCompleted, workflowError), which must be supplied all together or not at all.
        /// </remarks>
        /// <param name="pipelineName">The unique name of the target pipeline to execute.</param>
        /// <param name="request">The chat request payload containing the user message, optional conversation history, and optional callback URLs.</param>
        /// <response code="202">The request was accepted; the workflow is running in the background and progress will be delivered to the supplied callback URLs.</response>
        /// <response code="400">The callback URLs were partially supplied (1-4 of 5).</response>
        /// <response code="401">The API key is missing or invalid.</response>
        /// <response code="404">No pipeline with the specified name was found.</response>
        /// <response code="503">A plugin configuration issue prevents pipeline execution.</response>
        [Tags("Requests")]
        [HttpPost("pipelines/{pipelineName}/requests/async")]
        [ProducesResponseType(typeof(ProcessRequestAsyncApiOutput), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<ProcessRequestAsyncApiOutput> PostNamedAsync([FromRoute] string pipelineName, [FromBody] ProcessRequestAsyncApiInput request)
        {
            return ExecuteRequestAsync(request, pipelineName);
        }

        /// <summary>
        /// Summarize conversation messages using the single registered summarization pipeline.
        /// </summary>
        [Tags("Summarization")]
        [HttpPost("summarize")]
        [ProducesResponseType(typeof(SummarizationApiOutput), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<SummarizationApiOutput>> PostSummarize([FromBody] SummarizationApiInput request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await appInstance.SummarizeAsync(
                    request.SummarizationLanguage,
                    request.Conversation!,
                    cancellationToken);

                return Ok(new SummarizationApiOutput
                {
                    RequestId = Guid.NewGuid(),
                    SummarizedContent = result.SummarizedContent,
                    SummarizedContentDatetime = result.SummarizedContentDatetime
                });
            }
            catch (PipelineRoutingException ex)
            {
                return StatusCode(ex.StatusCode, new ProblemDetails
                {
                    Status = ex.StatusCode,
                    Title = ex.Title,
                    Detail = ex.Detail
                });
            }
        }

        /// <summary>
        /// Summarize conversation messages asynchronously using the single registered summarization pipeline.
        /// </summary>
        [Tags("Summarization")]
        [HttpPost("summarize/async")]
        [ProducesResponseType(typeof(SummarizationAsyncApiOutput), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<SummarizationAsyncApiOutput> PostSummarizeAsync([FromBody] SummarizationAsyncApiInput request)
        {
            try
            {
                var requestId = appInstance.SummarizeInBackground(
                    request.SummarizationLanguage,
                    request.Conversation!,
                    request.WorkflowStartedCallbackUrl,
                    request.WorkflowStepStartedCallbackUrl,
                    request.WorkflowStepCompletedCallbackUrl,
                    request.WorkflowCompletedCallbackUrl,
                    request.WorkflowErrorCallbackUrl);

                return Accepted(new SummarizationAsyncApiOutput { RequestId = requestId });
            }
            catch (PipelineRoutingException ex)
            {
                return StatusCode(ex.StatusCode, new ProblemDetails
                {
                    Status = ex.StatusCode,
                    Title = ex.Title,
                    Detail = ex.Detail
                });
            }
        }

        private async Task<ActionResult<ProcessRequestApiOutput>> ExecuteRequest(string message, IEnumerable<ContextMessage>? conversation, string? pipelineName, CancellationToken cancellationToken)
        {
            try
            {
                var requestId = Guid.NewGuid();
                var result = await appInstance.ProcessRequest(message, conversation, pipelineName, cancellationToken);
                return Ok(new ProcessRequestApiOutput { RequestId = requestId, WorkflowResult = result });
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

        private ActionResult<ProcessRequestAsyncApiOutput> ExecuteRequestAsync(ProcessRequestAsyncApiInput request, string? pipelineName)
        {
            try
            {
                var requestId = appInstance.ProcessRequestAsync(
                    request.Message,
                    request.Conversation,
                    pipelineName,
                    request.WorkflowStartedCallbackUrl,
                    request.WorkflowStepStartedCallbackUrl,
                    request.WorkflowStepCompletedCallbackUrl,
                    request.WorkflowCompletedCallbackUrl,
                    request.WorkflowErrorCallbackUrl);

                return Accepted(new ProcessRequestAsyncApiOutput { RequestId = requestId });
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