#pragma warning disable OPENAI001
using System.ClientModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using OpenAI.Responses;

namespace AgentMesh.Infrastructure.Responses;

public class ResponsesApiClient : IChatClient
{
    private readonly IResponsesStreamClient _client;
    private readonly string _model;
    private readonly string _systemPrompt;
    private readonly float _temperature;
    private readonly ResponseReasoningEffortLevel _reasoningEffort;
    private string _lastReasoning = string.Empty;

    public ResponsesApiClient(
        string model,
        string apiKey,
        string endpoint,
        string temperature,
        string systemPrompt,
        string reasoningEffort)
        : this(model, apiKey, endpoint, temperature, systemPrompt, reasoningEffort,
            CreateSdkClient(model, apiKey, endpoint, temperature, systemPrompt, reasoningEffort))
    {
    }

    internal ResponsesApiClient(
        string model,
        string apiKey,
        string endpoint,
        string temperature,
        string systemPrompt,
        string reasoningEffort,
        IResponsesStreamClient client)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(client);

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            (endpointUri.Scheme != Uri.UriSchemeHttp && endpointUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Endpoint must be an absolute HTTP or HTTPS URI.", nameof(endpoint));
        }

        if (!float.TryParse(temperature, NumberStyles.Float, CultureInfo.InvariantCulture, out _temperature) ||
            !float.IsFinite(_temperature) || _temperature < 0 || _temperature > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0 and 2.");
        }

        _model = model;
        _reasoningEffort = ParseReasoningEffort(reasoningEffort);
        _systemPrompt = systemPrompt;
        _client = client;
    }

    internal string LastReasoning => Volatile.Read(ref _lastReasoning);

    public Task<ChatClientResponse> GenerateResponseAsync(
        IEnumerable<string> userInput,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userInput);

        return GenerateResponseAsync(userInput.Select(content => new AgentMessage
        {
            Role = AgentMessageRole.User,
            Content = content
        }), cancellationToken);
    }

    public async Task<ChatClientResponse> GenerateResponseAsync(
        IEnumerable<AgentMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        var inputMessages = messages.ToList();
        var options = new CreateResponseOptions
        {
            Model = _model,
            Temperature = _temperature,
            StreamingEnabled = true,
            ReasoningOptions = new ResponseReasoningOptions
            {
                ReasoningEffortLevel = _reasoningEffort
            }
        };

        options.InputItems.Add(ResponseItem.CreateSystemMessageItem(
            string.Join("\n", inputMessages
                .Where(message => message.Role == AgentMessageRole.System)
                .Select(message => message.Content)
                .Prepend(_systemPrompt))));

        foreach (var message in inputMessages)
        {
            if (message.Role == AgentMessageRole.System)
            {
                continue;
            }

            options.InputItems.Add(message.Role == AgentMessageRole.Assistant
                ? ResponseItem.CreateAssistantMessageItem(message.Content)
                : ResponseItem.CreateUserMessageItem(message.Content));
        }

        var outputText = new StringBuilder();
        var reasoningText = new StringBuilder();
        var totalTokenCount = 0;
        var inputTokenCount = 0;
        var outputTokenCount = 0;

        await foreach (var update in _client.CreateResponseStreamingAsync(options, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            if (update.OutputTextDelta is not null)
            {
                outputText.Append(update.OutputTextDelta);
            }

            if (update.ReasoningTextDelta is not null)
            {
                reasoningText.Append(update.ReasoningTextDelta);
            }

            totalTokenCount = update.TotalTokenCount ?? totalTokenCount;
            inputTokenCount = update.InputTokenCount ?? inputTokenCount;
            outputTokenCount = update.OutputTokenCount ?? outputTokenCount;
        }

        Volatile.Write(ref _lastReasoning, reasoningText.ToString());
        var responseText = outputText.ToString();
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new BadStructuredResponseException(string.Empty, "The response text is empty.");
        }

        return new ChatClientResponse
        {
            Text = responseText,
            TotalTokenCount = totalTokenCount,
            InputTokenCount = inputTokenCount,
            OutputTokenCount = outputTokenCount
        };
    }

    private static IResponsesStreamClient CreateSdkClient(
        string model,
        string apiKey,
        string endpoint,
        string temperature,
        string systemPrompt,
        string reasoningEffort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(systemPrompt);

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            (endpointUri.Scheme != Uri.UriSchemeHttp && endpointUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Endpoint must be an absolute HTTP or HTTPS URI.", nameof(endpoint));
        }

        if (!float.TryParse(temperature, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedTemperature) ||
            !float.IsFinite(parsedTemperature) || parsedTemperature < 0 || parsedTemperature > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0 and 2.");
        }

        ParseReasoningEffort(reasoningEffort);
        var client = new ResponsesClient(new ApiKeyCredential(apiKey), new ResponsesClientOptions
        {
            Endpoint = endpointUri
        });
        return new SdkResponsesStreamClient(client);
    }

    private static ResponseReasoningEffortLevel ParseReasoningEffort(string reasoningEffort)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasoningEffort);

        return reasoningEffort.Trim().ToLowerInvariant() switch
        {
            "none" => ResponseReasoningEffortLevel.None,
            "minimal" => ResponseReasoningEffortLevel.Minimal,
            "low" => ResponseReasoningEffortLevel.Low,
            "medium" => ResponseReasoningEffortLevel.Medium,
            "high" => ResponseReasoningEffortLevel.High,
            "xhigh" => ResponseReasoningEffortLevel.ExtraHigh,
            _ => throw new ArgumentException("Unsupported reasoning effort.", nameof(reasoningEffort))
        };
    }
}

internal interface IResponsesStreamClient
{
    IAsyncEnumerable<ResponsesStreamUpdate> CreateResponseStreamingAsync(
        CreateResponseOptions options,
        CancellationToken cancellationToken);
}

internal sealed record ResponsesStreamUpdate(
    string? OutputTextDelta = null,
    string? ReasoningTextDelta = null,
    int? TotalTokenCount = null,
    int? InputTokenCount = null,
    int? OutputTokenCount = null);

internal sealed class SdkResponsesStreamClient(ResponsesClient client) : IResponsesStreamClient
{
    public async IAsyncEnumerable<ResponsesStreamUpdate> CreateResponseStreamingAsync(
        CreateResponseOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var update in client.CreateResponseStreamingAsync(options, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            switch (update)
            {
                case StreamingResponseOutputItemAddedUpdate
                {
                    Item: ReasoningResponseItem reasoningItem
                }:
                    if (!string.IsNullOrEmpty(reasoningItem.EncryptedContent))
                    {
                        yield return new ResponsesStreamUpdate(ReasoningTextDelta: reasoningItem.EncryptedContent);
                    }

                    foreach (var summaryText in reasoningItem.SummaryParts.OfType<ReasoningSummaryTextPart>())
                    {
                        yield return new ResponsesStreamUpdate(ReasoningTextDelta: summaryText.Text);
                    }
                    break;
                case StreamingResponseOutputTextDeltaUpdate outputText:
                    yield return new ResponsesStreamUpdate(OutputTextDelta: outputText.Delta);
                    break;
                case StreamingResponseReasoningTextDeltaUpdate reasoningText:
                    yield return new ResponsesStreamUpdate(ReasoningTextDelta: reasoningText.Delta);
                    break;
                case StreamingResponseReasoningSummaryTextDeltaUpdate reasoningSummary:
                    yield return new ResponsesStreamUpdate(ReasoningTextDelta: reasoningSummary.Delta);
                    break;
                case StreamingResponseCompletedUpdate completed when completed.Response.Usage is not null:
                    yield return new ResponsesStreamUpdate(
                        TotalTokenCount: completed.Response.Usage.TotalTokenCount,
                        InputTokenCount: completed.Response.Usage.InputTokenCount,
                        OutputTokenCount: completed.Response.Usage.OutputTokenCount);
                    break;
            }
        }
    }
}
#pragma warning restore OPENAI001