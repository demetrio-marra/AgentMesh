using System.Net.Http.Json;
using AgentMesh.Runtime.Models;
using AgentMesh.Models;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Runtime.Services
{
    /// <summary>
    /// Posts workflow progress events to the callback URLs configured for the current request scope.
    /// Every <c>Notify*</c> method is a no-op when its corresponding callback URL is not configured.
    /// </summary>
    internal sealed class CallbackWorkflowProgressNotifier(
        CallbackNotifierContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<CallbackWorkflowProgressNotifier> logger) : IWorkflowProgressNotifier
    {
        public Task NotifyWorkflowStart()
        {
            if (context.ExecutionKind == WorkflowExecutionContextKind.Summarization)
            {
                return PublishAsync("workflowStarted", new SummarizationStartedCallbackPayload
                {
                    RequestId = context.RequestId
                }, context.WorkflowStartedCallbackUrl);
            }

            return PublishAsync("workflowStarted", new WorkflowStartedCallbackPayload
            {
                RequestId = context.RequestId
            }, context.WorkflowStartedCallbackUrl);
        }

        // The final workflow result/error is not known here; AppInstance posts workflowCompleted/workflowError itself once the pipeline finishes.
        public Task NotifyWorkflowEnd() => Task.CompletedTask;

        public Task NotifyWorkflowStepStarted(string stepName, IEnumerable<EWDisplayParameterRecord> inputParameters)
        {
            if (context.ExecutionKind == WorkflowExecutionContextKind.Summarization)
            {
                return PublishAsync("workflowStepStarted", new SummarizationStepStartedCallbackPayload
                {
                    RequestId = context.RequestId,
                    StepName = stepName,
                    InputParameters = inputParameters
                }, context.WorkflowStepStartedCallbackUrl);
            }

            return PublishAsync("workflowStepStarted", new WorkflowStepStartedCallbackPayload
            {
                RequestId = context.RequestId,
                StepName = stepName,
                InputParameters = inputParameters
            }, context.WorkflowStepStartedCallbackUrl);
        }

        public Task NotifyWorkflowStepCompleted(string stepName, EWStepStatisticsRecord statistics)
        {
            if (context.ExecutionKind == WorkflowExecutionContextKind.Summarization)
            {
                return PublishAsync("workflowStepCompleted", new SummarizationStepCompletedCallbackPayload
                {
                    RequestId = context.RequestId,
                    StepName = stepName,
                    Elapsed = statistics.Elapsed,
                    IsAgentic = statistics.IsAgentic,
                    ParametersDiff = statistics.ParametersDiff.ToList()
                }, context.WorkflowStepCompletedCallbackUrl);
            }

            return PublishAsync("workflowStepCompleted", new WorkflowStepCompletedCallbackPayload
            {
                RequestId = context.RequestId,
                StepName = stepName,
                Elapsed = statistics.Elapsed,
                IsAgentic = statistics.IsAgentic,
                ParametersDiff = statistics.ParametersDiff.ToList()
            }, context.WorkflowStepCompletedCallbackUrl);
        }

        private Task PublishAsync<TPayload>(string eventName, TPayload payload, string? callbackUrl)
        {
            if (context.StreamEventSink is not null)
            {
                return context.StreamEventSink(eventName, payload!, context.StreamCancellationToken);
            }

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                return Task.CompletedTask;
            }

            return PostAsync(callbackUrl, payload);
        }

        private async Task PostAsync<TPayload>(string callbackUrl, TPayload payload)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient(nameof(CallbackWorkflowProgressNotifier));
                using var response = await httpClient.PostAsJsonAsync(callbackUrl, payload);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Callback POST to {CallbackUrl} for request {RequestId} returned status {StatusCode}.", callbackUrl, context.RequestId, (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Callback POST to {CallbackUrl} for request {RequestId} failed.", callbackUrl, context.RequestId);
            }
        }
    }
}
