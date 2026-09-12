using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Costs;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

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
    public class StatelessAppInstance(
        IServiceProvider serviceProvider,
        IEnumerable<AgentFlatConfigurationRecord> agentsConfigurations,
        PluginHostState pluginHostState)
    {
        public async Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, CancellationToken cancellationToken)
        {
            return await ProcessRequest(message, conversation, pipelineName: null, cancellationToken);
        }

        public async Task<WorkflowResult> ProcessRequest(string message, IEnumerable<ContextMessage>? conversation, string? pipelineName, CancellationToken cancellationToken)
        {
            var requestDatetime = DateTime.UtcNow;

            using var executionScope = serviceProvider.CreateScope();

            var pipeline = ResolvePipeline(executionScope.ServiceProvider, pipelineName);
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
