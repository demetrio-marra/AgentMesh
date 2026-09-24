using System.Net.Http.Json;
using System.Net.Http.Headers;
using AgentMesh.Configuration;
using AgentMesh.Models;

namespace AgentMesh.Services;

internal sealed class AgentMeshApiClient(
    HttpClient httpClient,
    CallbackUrlFactory callbackUrlFactory)
{
    public async Task<ConfigurationSummaryApiOutput> GetConfigurationSummaryAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("api/configuration", cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ConfigurationSummaryApiOutput>(cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty configuration summary.");
    }

    public async Task<Guid> SubmitChatAsync(string message, IEnumerable<ContextMessage> conversation, CancellationToken cancellationToken)
    {
        var callbacks = callbackUrlFactory.CreateWorkflowCallbacks();
        var request = new ProcessRequestAsyncApiInput
        {
            Message = message,
            Conversation = conversation.ToList(),
            WorkflowStartedCallbackUrl = callbacks.Started,
            WorkflowStepStartedCallbackUrl = callbacks.StepStarted,
            WorkflowStepCompletedCallbackUrl = callbacks.StepCompleted,
            WorkflowCompletedCallbackUrl = callbacks.Completed,
            WorkflowErrorCallbackUrl = callbacks.Error
        };

        using var response = await httpClient.PostAsJsonAsync("api/requests/async", request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<ProcessRequestAsyncApiOutput>(cancellationToken))?.RequestId
            ?? throw new InvalidOperationException("The API returned no request identifier.");
    }

    public async Task<Guid> SubmitSummarizationAsync(string language, IEnumerable<ContextMessage> conversation, CancellationToken cancellationToken)
    {
        var callbacks = callbackUrlFactory.CreateSummarizationCallbacks();
        var request = new SummarizationAsyncApiInput
        {
            SummarizationLanguage = language,
            Conversation = conversation.ToList(),
            WorkflowStartedCallbackUrl = callbacks.Started,
            WorkflowStepStartedCallbackUrl = callbacks.StepStarted,
            WorkflowStepCompletedCallbackUrl = callbacks.StepCompleted,
            WorkflowCompletedCallbackUrl = callbacks.Completed,
            WorkflowErrorCallbackUrl = callbacks.Error
        };

        using var response = await httpClient.PostAsJsonAsync("api/summarize/async", request, cancellationToken);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<SummarizationAsyncApiOutput>(cancellationToken))?.RequestId
            ?? throw new InvalidOperationException("The API returned no request identifier.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"API request failed with {(int)response.StatusCode}: {detail}");
    }
}

internal sealed class CallbackUrlFactory(CallbackConfiguration configuration)
{
    public CallbackUrls CreateWorkflowCallbacks() => Create("workflow");
    public CallbackUrls CreateSummarizationCallbacks() => Create("summarization");

    private CallbackUrls Create(string prefix)
    {
        var baseUrl = configuration.BaseUrl.TrimEnd('/');
        return new CallbackUrls(
            $"{baseUrl}/callbacks/{prefix}-started",
            $"{baseUrl}/callbacks/{prefix}-step-started",
            $"{baseUrl}/callbacks/{prefix}-step-completed",
            $"{baseUrl}/callbacks/{prefix}-completed",
            $"{baseUrl}/callbacks/{prefix}-error");
    }
}

internal sealed record CallbackUrls(string Started, string StepStarted, string StepCompleted, string Completed, string Error);
