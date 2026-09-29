using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgentMesh.Configuration;
using AgentMesh.Models;

namespace AgentMesh.Services;

internal sealed class AgentMeshApiClient(
    HttpClient httpClient,
    ConsoleWorkflowProgressNotifier progressNotifier)
{
    public async Task<ConfigurationSummaryApiOutput> GetConfigurationSummaryAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync("api/configuration", cancellationToken);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ConfigurationSummaryApiOutput>(cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty configuration summary.");
    }

    public Task<WorkflowResult> StreamChatAsync(string message, IEnumerable<ContextMessage> conversation, CancellationToken cancellationToken)
    {
        var request = new ProcessRequestApiInput
        {
            Message = message,
            Conversation = conversation.ToList()
        };

        return ReadStreamAsync(
            "api/requests/stream",
            request,
            payload => JsonSerializer.Deserialize<WorkflowResult>(payload.GetProperty("result"), StreamJsonOptions)
                ?? throw new InvalidOperationException("The API returned no workflow result."),
            cancellationToken);
    }

    public Task<SummarizationCompletedCallbackPayload> StreamSummarizationAsync(string language, IEnumerable<ContextMessage> conversation, CancellationToken cancellationToken)
    {
        var request = new SummarizationApiInput
        {
            SummarizationLanguage = language,
            Conversation = conversation.ToList()
        };

        return ReadStreamAsync(
            "api/summarize/stream",
            request,
            payload => JsonSerializer.Deserialize<SummarizationCompletedCallbackPayload>(payload, StreamJsonOptions)
                ?? throw new InvalidOperationException("The API returned no summarization result."),
            cancellationToken);
    }

    private async Task<TResult> ReadStreamAsync<TResult>(string route, object request, Func<JsonElement, TResult> parseCompletion, CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, route)
        {
            Content = JsonContent.Create(request, options: StreamJsonOptions)
        };
        using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response);

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);
        string? eventName = null;
        string? data = null;
        var terminalReceived = false;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Length == 0)
            {
                if (eventName is null || data is null)
                {
                    continue;
                }

                JsonElement payload;
                try
                {
                    payload = JsonSerializer.Deserialize<JsonElement>(data, StreamJsonOptions);
                }
                catch (JsonException exception)
                {
                    throw new InvalidOperationException("The API returned malformed stream data.", exception);
                }

                switch (eventName)
                {
                    case "workflowStarted":
                        await progressNotifier.NotifyWorkflowStart();
                        break;
                    case "workflowStepStarted":
                        await progressNotifier.NotifyWorkflowStepStarted(
                            payload.GetProperty("stepName").GetString() ?? string.Empty,
                            payload.GetProperty("inputParameters").Deserialize<IEnumerable<EWDisplayParameterRecord>>(StreamJsonOptions) ?? []);
                        break;
                    case "workflowStepCompleted":
                        await progressNotifier.NotifyWorkflowStepCompleted(
                            payload.GetProperty("stepName").GetString() ?? string.Empty,
                            payload.GetProperty("elapsed").Deserialize<TimeSpan>(StreamJsonOptions),
                            payload.GetProperty("isAgentic").GetBoolean(),
                            payload.GetProperty("parametersDiff").Deserialize<IEnumerable<EWDisplayDiffParameterRecord>>(StreamJsonOptions) ?? []);
                        break;
                    case "workflowCompleted":
                        if (terminalReceived)
                        {
                            throw new InvalidOperationException("The API returned multiple terminal stream events.");
                        }

                        terminalReceived = true;
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

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventName = line[7..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                data = line[6..];
            }
        }

        throw new InvalidOperationException("The API stream ended before a terminal completion event.");
    }

    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);

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
