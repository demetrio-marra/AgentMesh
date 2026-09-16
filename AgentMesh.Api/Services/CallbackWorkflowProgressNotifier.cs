using System.Net.Http.Json;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Models;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Services
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
            return PostIfConfiguredAsync(context.WorkflowStartedCallbackUrl, new WorkflowStartedCallbackPayload
            {
                RequestId = context.RequestId
            });
        }

        // The final workflow result/error is not known here; StatelessAppInstance posts workflowCompleted/workflowError itself once the pipeline finishes.
        public Task NotifyWorkflowEnd() => Task.CompletedTask;

        public Task NotifyWorkflowStepStarted(string stepName, IEnumerable<EWDisplayParameterRecord> inputParameters)
        {
            return PostIfConfiguredAsync(context.WorkflowStepStartedCallbackUrl, new WorkflowStepStartedCallbackPayload
            {
                RequestId = context.RequestId,
                StepName = stepName,
                InputParameters = inputParameters
            });
        }

        public Task NotifyWorkflowStepCompleted(string stepName, EWStepStatisticsRecord statistics)
        {
            return PostIfConfiguredAsync(context.WorkflowStepCompletedCallbackUrl, new WorkflowStepCompletedCallbackPayload
            {
                RequestId = context.RequestId,
                StepName = stepName,
                Elapsed = statistics.Elapsed,
                IsAgentic = statistics.IsAgentic,
                ParametersDiff = statistics.ParametersDiff.ToList()
            });
        }

        private Task PostIfConfiguredAsync<TPayload>(string? callbackUrl, TPayload payload)
        {
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
