using System.Net.Http.Json;
using System.Text.Json;
using AgentMeshWeb.Models;

namespace AgentMeshWeb.Services;

public sealed class AgentMeshApiClient(HttpClient httpClient) : IAgentMeshApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ConfigurationSummaryApiOutput> GetConfigurationSummaryAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("api/configuration", cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ConfigurationSummaryApiOutput>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty configuration summary.");
    }

    public Task<WorkflowResult> StreamChatAsync(string message, IReadOnlyList<ContextMessage> conversation, Func<WorkflowProgress, Task> onProgress, CancellationToken cancellationToken) =>
        ReadStreamAsync(
            "api/requests/stream",
            new ProcessRequestApiInput { Message = message, Conversation = conversation },
            payload => JsonSerializer.Deserialize<WorkflowResult>(payload.GetProperty("result"), JsonOptions)
                ?? throw new InvalidOperationException("The API returned no workflow result."),
            onProgress,
            cancellationToken);

    public Task<SummarizationResult> StreamSummarizationAsync(string language, IReadOnlyList<ContextMessage> conversation, Func<WorkflowProgress, Task> onProgress, CancellationToken cancellationToken) =>
        ReadStreamAsync(
            "api/summarize/stream",
            new SummarizationApiInput { SummarizationLanguage = language, Conversation = conversation },
            payload => new SummarizationResult(
                payload.GetProperty("summarizedContent").GetString() ?? string.Empty,
                payload.GetProperty("summarizedContentDatetime").GetDateTime()),
            onProgress,
            cancellationToken);

    private async Task<TResult> ReadStreamAsync<TResult>(string route, object request, Func<JsonElement, TResult> parseCompletion, Func<WorkflowProgress, Task> onProgress, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        string? eventName = null;
        string? data = null;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Length == 0)
            {
                if (eventName is null && data is null)
                {
                    continue;
                }
                if (eventName is null || data is null)
                {
                    throw new InvalidOperationException("The API returned an incomplete stream event.");
                }

                JsonElement payload;
                try
                {
                    payload = JsonSerializer.Deserialize<JsonElement>(data, JsonOptions);
                }
                catch (JsonException exception)
                {
                    throw new InvalidOperationException("The API returned malformed stream data.", exception);
                }

                switch (eventName)
                {
                    case "workflowStarted":
                        await onProgress(new WorkflowProgress("started", "Workflow started.", eventName, data));
                        break;
                    case "workflowStepStarted":
                        await onProgress(new WorkflowProgress("step-started", $"Running {GetRequiredString(payload, "stepName")}.", eventName, data));
                        break;
                    case "workflowStepCompleted":
                        await onProgress(new WorkflowProgress("step-completed", $"Completed {GetRequiredString(payload, "stepName")}.", eventName, data));
                        break;
                    case "workflowCompleted":
                        return parseCompletion(payload);
                    case "workflowError":
                        throw new InvalidOperationException(payload.GetProperty("errorMessage").GetString() ?? "The API workflow failed.");
                    default:
                        throw new InvalidOperationException($"The API returned an unknown stream event '{eventName}'.");
                }

                eventName = null;
                data = null;
                continue;
            }

            if (line.StartsWith("event: ", StringComparison.Ordinal) && eventName is null)
            {
                eventName = line[7..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal) && data is null)
            {
                data = line[6..];
            }
            else
            {
                throw new InvalidOperationException("The API returned malformed stream framing.");
            }
        }

        throw new InvalidOperationException("The API stream ended before a terminal completion event.");
    }

    private static string GetRequiredString(JsonElement payload, string propertyName) =>
        payload.GetProperty(propertyName).GetString() ?? throw new InvalidOperationException($"The API stream omitted '{propertyName}'.");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException($"API request failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
}