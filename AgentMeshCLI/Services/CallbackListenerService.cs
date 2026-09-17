using System.Net;
using System.Text.Json;
using AgentMesh.Configuration;
using AgentMesh.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Services;

internal sealed class CallbackListenerService(
    CallbackConfiguration configuration,
    PendingRequestRegistry pendingRequests,
    ConsoleWorkflowProgressNotifier progressNotifier,
    ILogger<CallbackListenerService> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private HttpListener? _listener;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(configuration.ListenUrl.TrimEnd('/') + "/");
        _listener.Start();
        logger.LogInformation("Callback listener started at {CallbackUrl}.", configuration.ListenUrl);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync().WaitAsync(stoppingToken);
                _ = HandleAsync(context);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            _listener.Stop();
            _listener.Close();
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        try
        {
            if (!HttpMethods.IsPost(context.Request.HttpMethod))
            {
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return;
            }

            var path = context.Request.Url?.AbsolutePath.TrimEnd('/').ToLowerInvariant() ?? string.Empty;
            using var reader = new StreamReader(context.Request.InputStream);
            var body = await reader.ReadToEndAsync();
            await DispatchAsync(path, body);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Callback handling failed.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {
            context.Response.Close();
        }
    }

    private async Task DispatchAsync(string path, string body)
    {
        if (path.EndsWith("/workflow-started"))
        {
            var payload = Deserialize<WorkflowStartedCallbackPayload>(body);
            if (pendingRequests.IsActive(payload.RequestId)) await progressNotifier.NotifyWorkflowStart();
        }
        else if (path.EndsWith("/workflow-step-started"))
        {
            var payload = Deserialize<WorkflowStepStartedCallbackPayload>(body);
            if (pendingRequests.IsActive(payload.RequestId)) await progressNotifier.NotifyWorkflowStepStarted(payload.StepName, payload.InputParameters);
        }
        else if (path.EndsWith("/workflow-step-completed"))
        {
            var payload = Deserialize<WorkflowStepCompletedCallbackPayload>(body);
            if (pendingRequests.IsActive(payload.RequestId)) await progressNotifier.NotifyWorkflowStepCompleted(payload.StepName, payload.Elapsed, payload.IsAgentic, payload.ParametersDiff);
        }
        else if (path.EndsWith("/workflow-completed"))
        {
            var payload = Deserialize<WorkflowCompletedCallbackPayload>(body);
            pendingRequests.TryComplete(payload.RequestId, new CallbackResult { WorkflowResult = payload.Result });
        }
        else if (path.EndsWith("/workflow-error"))
        {
            var payload = Deserialize<WorkflowErrorCallbackPayload>(body);
            pendingRequests.TryComplete(payload.RequestId, new CallbackResult { ErrorMessage = payload.ErrorMessage });
        }
        else if (path.EndsWith("/summarization-started"))
        {
            var payload = Deserialize<SummarizationStartedCallbackPayload>(body);
            if (pendingRequests.IsActive(payload.RequestId)) await progressNotifier.NotifyWorkflowStart();
        }
        else if (path.EndsWith("/summarization-step-started"))
        {
            var payload = Deserialize<SummarizationStepStartedCallbackPayload>(body);
            if (pendingRequests.IsActive(payload.RequestId)) await progressNotifier.NotifyWorkflowStepStarted(payload.StepName, payload.InputParameters);
        }
        else if (path.EndsWith("/summarization-step-completed"))
        {
            var payload = Deserialize<SummarizationStepCompletedCallbackPayload>(body);
            if (pendingRequests.IsActive(payload.RequestId)) await progressNotifier.NotifyWorkflowStepCompleted(payload.StepName, payload.Elapsed, payload.IsAgentic, payload.ParametersDiff);
        }
        else if (path.EndsWith("/summarization-completed"))
        {
            var payload = Deserialize<SummarizationCompletedCallbackPayload>(body);
            pendingRequests.TryComplete(payload.RequestId, new CallbackResult { SummarizationResult = payload });
        }
        else if (path.EndsWith("/summarization-error"))
        {
            var payload = Deserialize<SummarizationErrorCallbackPayload>(body);
            pendingRequests.TryComplete(payload.RequestId, new CallbackResult { ErrorMessage = payload.ErrorMessage });
        }
    }

    private static T Deserialize<T>(string body) => JsonSerializer.Deserialize<T>(body, JsonOptions)
        ?? throw new InvalidOperationException($"Unable to deserialize callback payload as {typeof(T).Name}.");

    private static class HttpMethods
    {
        public static bool IsPost(string? method) => string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
    }
}
