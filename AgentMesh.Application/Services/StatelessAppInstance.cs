using System.Net.Http.Json;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Costs;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    /// <summary>
    /// This class provides stateless request processing for non-interactive (API) mode.
    /// It does not persist conversation state or perform host-side context summarization.
    /// Conversation context is supplied per request by the caller.
    /// </summary>
    /// <param name="serviceProvider">The root service provider.</param>
    /// <param name="agentsConfigurations">Configurations for registered agents.</param>
    /// <param name="pluginHostState">Startup plugin host validation state.</param>
    /// <param name="httpClientFactory">Factory used to POST the final workflowCompleted/workflowError callbacks once background execution finishes.</param>
    /// <param name="logger">Logger for background execution failures and callback delivery issues.</param>
    public class StatelessAppInstance(
        IServiceProvider serviceProvider,
        IEnumerable<AgentFlatConfigurationRecord> agentsConfigurations,
        PluginHostState pluginHostState,
        IHttpClientFactory httpClientFactory,
        ILogger<StatelessAppInstance> logger)
    {
        public async Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, CancellationToken cancellationToken)
        {
            return await ProcessRequest(message, conversation, pipelineName: null, cancellationToken);
        }

        public async Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, string? pipelineName, CancellationToken cancellationToken)
        {
            using var executionScope = serviceProvider.CreateScope();
            var pipeline = ResolvePipeline(executionScope.ServiceProvider, pipelineName);
            return await ExecuteRequestAsync(pipeline, message, conversation, cancellationToken);
        }

        /// <summary>
        /// Resolves the target pipeline and generates a request id synchronously - so pipeline routing failures
        /// (no pipelines loaded, ambiguous name, not found, invalid plugin configuration) propagate to the caller
        /// immediately instead of surfacing later via the workflowError callback - then starts the workflow in the
        /// background and returns without waiting for it to complete. The background execution runs with its own
        /// DI scope and is not tied to the caller's cancellation token, since it must keep running after the HTTP
        /// response has already been sent.
        /// </summary>
        public Guid ProcessRequestAsync(
            string message,
            IEnumerable<ContextMessage>? conversation,
            string? pipelineName,
            string? workflowStartedCallbackUrl,
            string? workflowStepStartedCallbackUrl,
            string? workflowStepCompletedCallbackUrl,
            string? workflowCompletedCallbackUrl,
            string? workflowErrorCallbackUrl)
        {
            var executionScope = serviceProvider.CreateScope();

            IChatRequestPipeline pipeline;
            try
            {
                pipeline = ResolvePipeline(executionScope.ServiceProvider, pipelineName);
            }
            catch
            {
                executionScope.Dispose();
                throw;
            }

            var requestId = Guid.NewGuid();

            var callbackContext = executionScope.ServiceProvider.GetRequiredService<CallbackNotifierContext>();
            callbackContext.RequestId = requestId;
            callbackContext.WorkflowStartedCallbackUrl = workflowStartedCallbackUrl;
            callbackContext.WorkflowStepStartedCallbackUrl = workflowStepStartedCallbackUrl;
            callbackContext.WorkflowStepCompletedCallbackUrl = workflowStepCompletedCallbackUrl;
            callbackContext.WorkflowCompletedCallbackUrl = workflowCompletedCallbackUrl;
            callbackContext.WorkflowErrorCallbackUrl = workflowErrorCallbackUrl;

            _ = RunInBackgroundAsync(executionScope, pipeline, requestId, message, conversation, workflowCompletedCallbackUrl, workflowErrorCallbackUrl);

            return requestId;
        }

        private async Task RunInBackgroundAsync(
            IServiceScope executionScope,
            IChatRequestPipeline pipeline,
            Guid requestId,
            string message,
            IEnumerable<ContextMessage>? conversation,
            string? workflowCompletedCallbackUrl,
            string? workflowErrorCallbackUrl)
        {
            try
            {
                var result = await ExecuteRequestAsync(pipeline, message, conversation, CancellationToken.None);

                if (!string.IsNullOrWhiteSpace(workflowCompletedCallbackUrl))
                {
                    await PostCallbackAsync(workflowCompletedCallbackUrl, new WorkflowCompletedCallbackPayload
                    {
                        RequestId = requestId,
                        Result = result
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background workflow execution failed for request {RequestId}.", requestId);

                if (!string.IsNullOrWhiteSpace(workflowErrorCallbackUrl))
                {
                    await PostCallbackAsync(workflowErrorCallbackUrl, new WorkflowErrorCallbackPayload
                    {
                        RequestId = requestId,
                        ErrorMessage = ex.Message
                    });
                }
            }
            finally
            {
                executionScope.Dispose();
            }
        }

        private async Task PostCallbackAsync<TPayload>(string callbackUrl, TPayload payload)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient(nameof(StatelessAppInstance));
                using var response = await httpClient.PostAsJsonAsync(callbackUrl, payload);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Callback POST to {CallbackUrl} returned status {StatusCode}.", callbackUrl, (int)response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Callback POST to {CallbackUrl} failed.", callbackUrl);
            }
        }

        private async Task<WorkflowResult> ExecuteRequestAsync(IChatRequestPipeline pipeline, string message, IEnumerable<ContextMessage>? conversation, CancellationToken cancellationToken)
        {
            var requestDatetime = DateTime.UtcNow;

            var conversationList = conversation?.ToList() ?? [];
            pipeline.SetParameterInitialValues(message, conversationList, requestDatetime);

            var stepsStats = await pipeline.ExecuteAsync(cancellationToken);
            var usageStatistics = stepsStats.ToList();

            var answerText = pipeline.FinalResponse;

            var inputTokens = stepsStats.Where(s => s.CountInputTokensAsContextTokens).Sum(s => s.InputTokens ?? 0);
            var outputTokens = stepsStats.Where(s => s.CountOutputTokensAsContextTokens).Sum(s => s.OutputTokens ?? 0);
            var totalTokens = inputTokens + outputTokens;

            var agentsCosts = CalculateExecutionCosts(usageStatistics);
            var executionCost = agentsCosts.Sum(c => c.TotalCost);

            return new WorkflowResult
            {
                Message = answerText,
                MainPipelineStepsData = usageStatistics,
                ContextSummarizerHasRun = false,
                AgentsCostData = agentsCosts,
                CountOfMessages = conversationList.Count + 2,
                CountOfTokens = totalTokens,
                CountOfMessagesBeforeSummarization = null,
                CountOfTokensBeforeSummarization = null,
                CumulatedCost = executionCost
            };
        }

        private IChatRequestPipeline ResolvePipeline(IServiceProvider scopedServiceProvider, string? pipelineName)
        {
            var pipelines = scopedServiceProvider.GetServices<IChatRequestPipeline>().ToList();

            var duplicatePipelineNames = pipelines
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => string.IsNullOrWhiteSpace(g.Key) || g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatePipelineNames.Count > 0)
            {
                throw PipelineRoutingException.PluginConfigurationInvalid();
            }

            if (pluginHostState.HasConfigurationIssues)
            {
                throw PipelineRoutingException.PluginConfigurationInvalid();
            }

            if (string.IsNullOrWhiteSpace(pipelineName))
            {
                if (pipelines.Count == 0)
                {
                    throw PipelineRoutingException.NoPipelinesLoaded();
                }

                if (pipelines.Count > 1)
                {
                    throw PipelineRoutingException.PipelineNameRequired();
                }

                return pipelines[0];
            }

            var match = pipelines.FirstOrDefault(p => string.Equals(p.Name, pipelineName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                throw PipelineRoutingException.PipelineNotFound();
            }

            return match;
        }

        private List<AgentExecutionCost> CalculateExecutionCosts(IEnumerable<EWStepStatisticsRecord> stepStatistics)
        {
            var costs = new List<AgentExecutionCost>();
            var agenticSteps = stepStatistics.Where(s => s.IsAgentic && !string.IsNullOrWhiteSpace(s.AgentName))
                .ToList();

            var agentsConfigurationsDict = agentsConfigurations.ToDictionary(a => a.AgentUniqueRole, a => a);

            foreach (var step in agenticSteps)
            {
                if (agentsConfigurationsDict.TryGetValue(step.AgentName!, out var agentConfig))
                {
                    var agentCost = new AgentExecutionCost(
                        AgentName: step.AgentName!,
                        CostPerMillionInputTokens: agentConfig.LLMClassCostPerMillionInputTokens,
                        CostPerMillionOutputTokens: agentConfig.LLMClassCostPerMillionOutputTokens,
                        ConsumedInputTokens: step.InputTokens ?? 0,
                        ConsumedOutputTokens: step.OutputTokens ?? 0,
                        CostPerHour: agentConfig.LLMClassCostPerHour,
                        Elapsed: step.Elapsed
                    );
                    costs.Add(agentCost);
                }
            }
            return costs;
        }
    }
}

