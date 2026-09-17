using System.Globalization;
using System.Net.Http.Json;
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Costs;
using AgentMesh.Application.Models.Configuration;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    /// <summary>
    /// Unified stateless application runner that processes user requests across chat and summarization pipelines.
    /// It does not persist conversation state or retain host-side context.
    /// Conversation context is supplied per execution by the caller.
    /// </summary>
    /// <param name="serviceProvider">The root service provider.</param>
    /// <param name="agentsConfigurations">Configurations for registered agents.</param>
    /// <param name="sesJSSandboxConfiguration">Configuration for the JavaScript sandbox service.</param>
    /// <param name="userConfiguration">Configuration for the current user agent.</param>
    /// <param name="pluginHostState">Startup plugin host validation state.</param>
    /// <param name="httpClientFactory">Factory used to POST terminal callbacks once background execution finishes.</param>
    /// <param name="logger">Logger for background execution failures and callback delivery issues.</param>
    public class AppInstance(
        IServiceProvider serviceProvider,
        IEnumerable<AgentFlatConfigurationRecord> agentsConfigurations,
        SESJSSandboxConfiguration sesJSSandboxConfiguration,
        UserConfiguration userConfiguration,
        PluginHostState pluginHostState,
        IHttpClientFactory httpClientFactory,
        ILogger<AppInstance> logger)
    {
        public ConfigurationSummary GetConfigurationSummary()
        {
            var agents = agentsConfigurations
                .Select(agentConfig => new AgentConfigurationSummary
                {
                    AgentRole = agentConfig.AgentUniqueRole,
                    Model = agentConfig.ProviderModelName,
                    Provider = agentConfig.ProviderName,
                    CostPerMillionInputTokens = agentConfig.LLMClassCostPerMillionInputTokens,
                    CostPerMillionOutputTokens = agentConfig.LLMClassCostPerMillionOutputTokens,
                    CostPerHour = agentConfig.LLMClassCostPerHour,
                    Temperature = Convert.ToDouble(agentConfig.Temperature, CultureInfo.InvariantCulture)
                })
                .ToList();

            return new ConfigurationSummary
            {
                SandboxServiceUrl = sesJSSandboxConfiguration.SandboxServiceURL,
                SandboxName = sesJSSandboxConfiguration.SandboxName,
                AgentId = userConfiguration.AgentId,
                Agents = agents
            };
        }

        /// <summary>
        /// Process a chat request synchronously using the default pipeline.
        /// </summary>
        public async Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, CancellationToken cancellationToken = default)
        {
            return await ProcessRequest(message, conversation, pipelineName: null, cancellationToken);
        }

        /// <summary>
        /// Process a chat request synchronously using an optional named pipeline.
        /// </summary>
        public async Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, string? pipelineName, CancellationToken cancellationToken = default)
        {
            using var executionScope = serviceProvider.CreateScope();
            var pipeline = ResolveChatPipeline(executionScope.ServiceProvider, pipelineName);
            return await ExecuteChatRequestAsync(pipeline, message, conversation, cancellationToken);
        }

        /// <summary>
        /// Resolves the target chat pipeline and generates a request id synchronously,
        /// then executes the workflow in the background with progress/terminal callbacks.
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
                pipeline = ResolveChatPipeline(executionScope.ServiceProvider, pipelineName);
            }
            catch
            {
                executionScope.Dispose();
                throw;
            }

            var requestId = Guid.NewGuid();

            var callbackContext = executionScope.ServiceProvider.GetRequiredService<CallbackNotifierContext>();
            callbackContext.RequestId = requestId;
            callbackContext.ExecutionKind = WorkflowExecutionContextKind.Chat;
            callbackContext.WorkflowStartedCallbackUrl = workflowStartedCallbackUrl;
            callbackContext.WorkflowStepStartedCallbackUrl = workflowStepStartedCallbackUrl;
            callbackContext.WorkflowStepCompletedCallbackUrl = workflowStepCompletedCallbackUrl;
            callbackContext.WorkflowCompletedCallbackUrl = workflowCompletedCallbackUrl;
            callbackContext.WorkflowErrorCallbackUrl = workflowErrorCallbackUrl;

            _ = RunChatInBackgroundAsync(executionScope, pipeline, requestId, message, conversation, workflowCompletedCallbackUrl, workflowErrorCallbackUrl);

            return requestId;
        }

        /// <summary>
        /// Summarize conversation messages synchronously using the single registered summarization pipeline.
        /// </summary>
        public async Task<SummarizationResult> SummarizeAsync(
            string summarizationLanguage,
            IEnumerable<ContextMessage> conversation,
            CancellationToken cancellationToken = default)
        {
            using var executionScope = serviceProvider.CreateScope();
            var pipeline = ResolveSummarizationPipeline(executionScope.ServiceProvider);
            return await ExecuteSummarizationAsync(pipeline, summarizationLanguage, conversation, cancellationToken);
        }

        /// <summary>
        /// Resolves the summarization pipeline and generates a request id synchronously,
        /// then executes summarization in the background with progress/terminal callbacks.
        /// </summary>
        public Guid SummarizeAsync(
            string summarizationLanguage,
            IEnumerable<ContextMessage> conversation,
            string? workflowStartedCallbackUrl,
            string? workflowStepStartedCallbackUrl,
            string? workflowStepCompletedCallbackUrl,
            string? workflowCompletedCallbackUrl,
            string? workflowErrorCallbackUrl)
        {
            return SummarizeInBackground(
                summarizationLanguage,
                conversation,
                workflowStartedCallbackUrl,
                workflowStepStartedCallbackUrl,
                workflowStepCompletedCallbackUrl,
                workflowCompletedCallbackUrl,
                workflowErrorCallbackUrl);
        }

        /// <summary>
        /// Alias for background summarization execution.
        /// </summary>
        public Guid SummarizeInBackground(
            string summarizationLanguage,
            IEnumerable<ContextMessage> conversation,
            string? workflowStartedCallbackUrl,
            string? workflowStepStartedCallbackUrl,
            string? workflowStepCompletedCallbackUrl,
            string? workflowCompletedCallbackUrl,
            string? workflowErrorCallbackUrl)
        {
            var executionScope = serviceProvider.CreateScope();

            ISummarizationPipeline pipeline;
            try
            {
                pipeline = ResolveSummarizationPipeline(executionScope.ServiceProvider);
            }
            catch
            {
                executionScope.Dispose();
                throw;
            }

            var requestId = Guid.NewGuid();

            var callbackContext = executionScope.ServiceProvider.GetRequiredService<CallbackNotifierContext>();
            callbackContext.RequestId = requestId;
            callbackContext.ExecutionKind = WorkflowExecutionContextKind.Summarization;
            callbackContext.WorkflowStartedCallbackUrl = workflowStartedCallbackUrl;
            callbackContext.WorkflowStepStartedCallbackUrl = workflowStepStartedCallbackUrl;
            callbackContext.WorkflowStepCompletedCallbackUrl = workflowStepCompletedCallbackUrl;
            callbackContext.WorkflowCompletedCallbackUrl = workflowCompletedCallbackUrl;
            callbackContext.WorkflowErrorCallbackUrl = workflowErrorCallbackUrl;

            _ = RunSummarizationInBackgroundAsync(
                executionScope,
                pipeline,
                requestId,
                summarizationLanguage,
                conversation.ToList(),
                workflowCompletedCallbackUrl,
                workflowErrorCallbackUrl);

            return requestId;
        }

        private async Task RunChatInBackgroundAsync(
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
                var result = await ExecuteChatRequestAsync(pipeline, message, conversation, CancellationToken.None);

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

        private async Task RunSummarizationInBackgroundAsync(
            IServiceScope executionScope,
            ISummarizationPipeline pipeline,
            Guid requestId,
            string summarizationLanguage,
            IEnumerable<ContextMessage> conversation,
            string? workflowCompletedCallbackUrl,
            string? workflowErrorCallbackUrl)
        {
            try
            {
                var result = await ExecuteSummarizationAsync(pipeline, summarizationLanguage, conversation, CancellationToken.None);

                if (!string.IsNullOrWhiteSpace(workflowCompletedCallbackUrl))
                {
                    await PostCallbackAsync(workflowCompletedCallbackUrl, new SummarizationCompletedCallbackPayload
                    {
                        RequestId = requestId,
                        SummarizedContent = result.SummarizedContent,
                        SummarizedContentDatetime = result.SummarizedContentDatetime
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background summarization failed for request {RequestId}.", requestId);

                if (!string.IsNullOrWhiteSpace(workflowErrorCallbackUrl))
                {
                    await PostCallbackAsync(workflowErrorCallbackUrl, new SummarizationErrorCallbackPayload
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

        private async Task<WorkflowResult> ExecuteChatRequestAsync(
            IChatRequestPipeline pipeline,
            string message,
            IEnumerable<ContextMessage>? conversation,
            CancellationToken cancellationToken)
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

        private async Task<SummarizationResult> ExecuteSummarizationAsync(
            ISummarizationPipeline pipeline,
            string summarizationLanguage,
            IEnumerable<ContextMessage> conversation,
            CancellationToken cancellationToken)
        {
            pipeline.SetParameterInitialValues(summarizationLanguage, conversation, DateTime.UtcNow);
            await pipeline.ExecuteAsync(cancellationToken);
            return new SummarizationResult(pipeline.SummarizedContent, pipeline.SummarizedContentDatetime);
        }

        private IChatRequestPipeline ResolveChatPipeline(IServiceProvider scopedServiceProvider, string? pipelineName)
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

        private ISummarizationPipeline ResolveSummarizationPipeline(IServiceProvider scopedServiceProvider)
        {
            if (pluginHostState.HasConfigurationIssues)
            {
                throw PipelineRoutingException.PluginConfigurationInvalid();
            }

            var pipelines = scopedServiceProvider.GetServices<ISummarizationPipeline>().ToList();
            return pipelines.Count switch
            {
                0 => throw PipelineRoutingException.NoPipelinesLoaded(),
                1 => pipelines[0],
                _ => throw PipelineRoutingException.PluginConfigurationInvalid()
            };
        }

        private async Task PostCallbackAsync<TPayload>(string callbackUrl, TPayload payload)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient(nameof(AppInstance));
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
