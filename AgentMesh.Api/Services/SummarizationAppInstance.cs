using System.Net.Http.Json;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Models;
using AgentMesh.Models.Api;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Services;

public sealed class SummarizationAppInstance(
    IServiceProvider serviceProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<SummarizationAppInstance> logger)
{
    public async Task<SummarizationApiOutput> SummarizeAsync(
        string summarizationLanguage,
        IEnumerable<ContextMessage> conversation,
        CancellationToken cancellationToken)
    {
        using var executionScope = serviceProvider.CreateScope();
        var pipeline = ResolvePipeline(executionScope.ServiceProvider);
        var result = await ExecuteAsync(pipeline, summarizationLanguage, conversation, cancellationToken);

        return new SummarizationApiOutput
        {
            RequestId = Guid.NewGuid(),
            SummarizedContent = result.Content,
            SummarizedContentDatetime = result.Datetime
        };
    }

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
        try
        {
            var pipeline = ResolvePipeline(executionScope.ServiceProvider);
            var requestId = Guid.NewGuid();
            var callbackContext = executionScope.ServiceProvider.GetRequiredService<SummarizationCallbackContext>();
            callbackContext.RequestId = requestId;
            callbackContext.WorkflowStartedCallbackUrl = workflowStartedCallbackUrl;
            callbackContext.WorkflowStepStartedCallbackUrl = workflowStepStartedCallbackUrl;
            callbackContext.WorkflowStepCompletedCallbackUrl = workflowStepCompletedCallbackUrl;
            callbackContext.WorkflowCompletedCallbackUrl = workflowCompletedCallbackUrl;
            callbackContext.WorkflowErrorCallbackUrl = workflowErrorCallbackUrl;

            _ = RunInBackgroundAsync(
                executionScope,
                pipeline,
                requestId,
                summarizationLanguage,
                conversation.ToList(),
                workflowCompletedCallbackUrl,
                workflowErrorCallbackUrl);

            return requestId;
        }
        catch
        {
            executionScope.Dispose();
            throw;
        }
    }

    private async Task RunInBackgroundAsync(
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
            var result = await ExecuteAsync(pipeline, summarizationLanguage, conversation, CancellationToken.None);

            if (!string.IsNullOrWhiteSpace(workflowCompletedCallbackUrl))
            {
                await PostCallbackAsync(workflowCompletedCallbackUrl, new SummarizationCompletedCallbackPayload
                {
                    RequestId = requestId,
                    SummarizedContent = result.Content,
                    SummarizedContentDatetime = result.Datetime
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

    private async Task<(string Content, DateTime Datetime)> ExecuteAsync(
        ISummarizationPipeline pipeline,
        string summarizationLanguage,
        IEnumerable<ContextMessage> conversation,
        CancellationToken cancellationToken)
    {
        pipeline.SetParameterInitialValues(summarizationLanguage, conversation, DateTime.UtcNow);
        await pipeline.ExecuteAsync(cancellationToken);
        return (pipeline.SummarizedContent, pipeline.SummarizedContentDatetime);
    }

    private ISummarizationPipeline ResolvePipeline(IServiceProvider scopedServiceProvider)
    {
        if (scopedServiceProvider.GetRequiredService<PluginHostState>().HasConfigurationIssues)
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
            var httpClient = httpClientFactory.CreateClient(nameof(SummarizationAppInstance));
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
}