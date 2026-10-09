using System.Text.Json;
using AgentMesh.Runtime.Models;
using AgentMesh.Runtime.Models.Api;
using AgentMesh.Runtime.Authentication;
using AgentMesh.Exceptions;
using AgentMesh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgentMesh.Runtime.Controllers
{
    /// <summary>
    /// Controller for executing AI workflow requests across registered pipelines.
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]
    public sealed class RequestsController(IAppInstance appInstance) : ControllerBase
    {
        private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);

        /// <summary>
        /// Process a chat request using the default pipeline.
        /// </summary>
        /// <remarks>
        /// Executes the request against the pipeline owned by this API deployment.
        /// </remarks>
        /// <param name="request">The chat request payload containing the user message and optional conversation history.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The request was successfully processed and the workflow result is returned, alongside a generated request id.</response>
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
            return await ExecuteRequest(request.Message, request.Conversation, cancellationToken);
        }

        /// <summary>
        /// Process a chat request and stream workflow progress and the terminal result as server-sent events.
        /// </summary>
        [Tags("Requests")]
        [HttpPost("requests/stream")]
        [Produces("text/event-stream")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> PostDefaultStream([FromBody] ProcessRequestApiInput request, CancellationToken cancellationToken)
        {
            return StreamChatAsync(request, cancellationToken);
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
        /// <response code="400">The callback URLs were partially supplied (1-4 of 5).</response>
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
            return ExecuteRequestAsync(request);
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
        /// Summarize conversation messages and stream workflow progress and the terminal result as server-sent events.
        /// </summary>
        [Tags("Summarization")]
        [HttpPost("summarize/stream")]
        [Produces("text/event-stream")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> PostSummarizeStream([FromBody] SummarizationApiInput request, CancellationToken cancellationToken)
        {
            return StreamSummarizationAsync(request, cancellationToken);
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

        private async Task<ActionResult<ProcessRequestApiOutput>> ExecuteRequest(string message, IEnumerable<ContextMessage>? conversation, CancellationToken cancellationToken)
        {
            try
            {
                var requestId = Guid.NewGuid();
                var result = await appInstance.ProcessRequest(message, conversation, cancellationToken);
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

        private ActionResult<ProcessRequestAsyncApiOutput> ExecuteRequestAsync(ProcessRequestAsyncApiInput request)
        {
            try
            {
                var requestId = appInstance.ProcessRequestAsync(
                    request.Message,
                    request.Conversation,
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

        private async Task<IActionResult> StreamChatAsync(ProcessRequestApiInput request, CancellationToken cancellationToken)
        {
            Guid requestId = Guid.Empty;
            var terminalSent = false;

            try
            {
                var result = await appInstance.ProcessRequestStreamAsync(
                    request.Message,
                    request.Conversation,
                    id => StartStreamAsync(id, cancellationToken, value => requestId = value),
                    (eventName, payload, token) => WriteStreamEventAsync(eventName, payload, token),
                    cancellationToken);

                await WriteTerminalAsync("workflowCompleted", new WorkflowCompletedCallbackPayload
                {
                    RequestId = requestId,
                    Result = result
                }, cancellationToken, () => terminalSent = true);
            }
            catch (PipelineRoutingException ex) when (!Response.HasStarted)
            {
                return StatusCode(ex.StatusCode, new ProblemDetails
                {
                    Status = ex.StatusCode,
                    Title = ex.Title,
                    Detail = ex.Detail
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex) when (Response.HasStarted && !cancellationToken.IsCancellationRequested && !terminalSent)
            {
                await WriteTerminalAsync("workflowError", new WorkflowErrorCallbackPayload
                {
                    RequestId = requestId,
                    ErrorMessage = ex.Message
                }, cancellationToken, () => terminalSent = true);
            }

            return new EmptyResult();
        }

        private async Task<IActionResult> StreamSummarizationAsync(SummarizationApiInput request, CancellationToken cancellationToken)
        {
            Guid requestId = Guid.Empty;
            var terminalSent = false;

            try
            {
                var result = await appInstance.SummarizeStreamAsync(
                    request.SummarizationLanguage,
                    request.Conversation!,
                    id => StartStreamAsync(id, cancellationToken, value => requestId = value),
                    (eventName, payload, token) => WriteStreamEventAsync(eventName, payload, token),
                    cancellationToken);

                await WriteTerminalAsync("workflowCompleted", new SummarizationCompletedCallbackPayload
                {
                    RequestId = requestId,
                    SummarizedContent = result.SummarizedContent,
                    SummarizedContentDatetime = result.SummarizedContentDatetime
                }, cancellationToken, () => terminalSent = true);
            }
            catch (PipelineRoutingException ex) when (!Response.HasStarted)
            {
                return StatusCode(ex.StatusCode, new ProblemDetails
                {
                    Status = ex.StatusCode,
                    Title = ex.Title,
                    Detail = ex.Detail
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex) when (Response.HasStarted && !cancellationToken.IsCancellationRequested && !terminalSent)
            {
                await WriteTerminalAsync("workflowError", new WorkflowErrorCallbackPayload
                {
                    RequestId = requestId,
                    ErrorMessage = ex.Message
                }, cancellationToken, () => terminalSent = true);
            }

            return new EmptyResult();
        }

        private async Task StartStreamAsync(Guid requestId, CancellationToken cancellationToken, Action<Guid> setRequestId)
        {
            setRequestId(requestId);
            Response.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers.CacheControl = "no-cache";
            await Response.StartAsync(cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        private async Task WriteStreamEventAsync(string eventName, object payload, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(payload, StreamJsonOptions);
            await Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        private async Task WriteTerminalAsync(string eventName, object payload, CancellationToken cancellationToken, Action markSent)
        {
            await WriteStreamEventAsync(eventName, payload, cancellationToken);
            markSent();
        }
    }
}