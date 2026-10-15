using System.Runtime.CompilerServices;

namespace AgentMesh.Infrastructure.ChatCompletions
{
    public class ChatCompletionsClient : IChatClient
    {
        private readonly string _systemPrompt;
        private readonly float _temperature;
        private readonly IChatCompletionsStreamClient _client;


        public ChatCompletionsClient(string model, string apikey, string endpoint, string temperature, string systemPrompt)
            : this(model, apikey, endpoint, temperature, systemPrompt,
                CreateSdkClient(model, apikey, endpoint))
        {
        }

        internal ChatCompletionsClient(
            string model,
            string apikey,
            string endpoint,
            string temperature,
            string systemPrompt,
            IChatCompletionsStreamClient client)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (apikey == null) throw new ArgumentNullException(nameof(apikey));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (systemPrompt == null) throw new ArgumentNullException(nameof(systemPrompt));
            ArgumentNullException.ThrowIfNull(client);

            _client = client;
            _systemPrompt = systemPrompt;
            _temperature = float.Parse(temperature, CultureInfo.InvariantCulture);
        }


        public async Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<string> userInput, CancellationToken cancellationToken = default)
        {
            var messages = userInput.Select(input => new AgentMessage
            {
                Role = AgentMessageRole.User,
                Content = input
            });

            return await GenerateResponseAsync(messages, cancellationToken);
        }


        public async Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default)
        {
            var chatMessages = new List<ChatMessage>();

            var mergedPrompt = CreateMergedSystemPrompt(new List<AgentMessage>(messages).Prepend(new AgentMessage
            {
                Role = AgentMessageRole.System,
                Content = _systemPrompt
            }));
            chatMessages.Add(mergedPrompt);

            foreach (var message in messages)
            {
                ChatMessage chatMessage;

                switch (message.Role)
                {
                    case AgentMessageRole.System:
                        continue;   // skip
                    case AgentMessageRole.Assistant:
                        chatMessage = ChatMessage.CreateAssistantMessage(message.Content);
                        break;
                    default:
                        chatMessage = ChatMessage.CreateUserMessage(message.Content);
                        break;
                }

                chatMessages.Add(chatMessage);
            }

            var chatCompletionOptions = new ChatCompletionOptions
            {
                Temperature = _temperature,
                // ToolChoice = ChatToolChoice.CreateNoneChoice(),
                ResponseFormat = ChatResponseFormat.CreateTextFormat(),
            };

            // use ExecuteRetryAsync to handle transient errors

            var responseTextBuilder = new System.Text.StringBuilder();
            int totalTokenCount = 0, inputTokenCount = 0, outputTokenCount = 0;

            try
            {
                await foreach (var update in _client.CompleteChatStreamingAsync(chatMessages, chatCompletionOptions, cancellationToken)
                    .WithCancellation(cancellationToken))
                {
                    if (update.TextDelta is not null)
                    {
                        responseTextBuilder.Append(update.TextDelta);
                    }

                    totalTokenCount = update.TotalTokenCount ?? totalTokenCount;
                    inputTokenCount = update.InputTokenCount ?? inputTokenCount;
                    outputTokenCount = update.OutputTokenCount ?? outputTokenCount;
                }
            }
            catch (Exception ex) when (ex.GetType().Name == "ClientResultException" && ex.Message.Contains("Tool choice is none, but model called a tool"))
            {
                throw new BadStructuredResponseException("", ex.Message, ex);
            }

            var responseText = responseTextBuilder.ToString();
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new BadStructuredResponseException("", "The response text is empty.");
            }

            return new ChatClientResponse
            {
                Text = responseText,
                TotalTokenCount = totalTokenCount,
                InputTokenCount = inputTokenCount,
                OutputTokenCount = outputTokenCount
            };
        }

        private static ChatMessage CreateMergedSystemPrompt(IEnumerable<AgentMessage> messages)
        {
            var systemMessages = messages
                .Where(m => m.Role == AgentMessageRole.System)
                .Select(m => m.Content);
            var mergedPrompt = string.Join("\n", systemMessages);
            return ChatMessage.CreateSystemMessage(mergedPrompt);
        }

        private static IChatCompletionsStreamClient CreateSdkClient(string model, string apikey, string endpoint)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (apikey == null) throw new ArgumentNullException(nameof(apikey));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            var client = new ChatClient(
                model: model,
                credential: new ApiKeyCredential(apikey),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(endpoint)
                });

            return new SdkChatCompletionsStreamClient(client);
        }
    }

    internal interface IChatCompletionsStreamClient
    {
        IAsyncEnumerable<ChatCompletionsStreamUpdate> CompleteChatStreamingAsync(
            IEnumerable<ChatMessage> messages,
            ChatCompletionOptions options,
            CancellationToken cancellationToken);
    }

    internal sealed record ChatCompletionsStreamUpdate(
        string? TextDelta = null,
        int? TotalTokenCount = null,
        int? InputTokenCount = null,
        int? OutputTokenCount = null);

    internal sealed class SdkChatCompletionsStreamClient(ChatClient client) : IChatCompletionsStreamClient
    {
        public async IAsyncEnumerable<ChatCompletionsStreamUpdate> CompleteChatStreamingAsync(
            IEnumerable<ChatMessage> messages,
            ChatCompletionOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var update in client.CompleteChatStreamingAsync(messages, options, cancellationToken)
                .WithCancellation(cancellationToken))
            {
                foreach (var part in update.ContentUpdate)
                {
                    yield return new ChatCompletionsStreamUpdate(TextDelta: part.Text);
                }

                if (update.Usage is not null)
                {
                    yield return new ChatCompletionsStreamUpdate(
                        TotalTokenCount: update.Usage.TotalTokenCount,
                        InputTokenCount: update.Usage.InputTokenCount,
                        OutputTokenCount: update.Usage.OutputTokenCount);
                }
            }
        }
    }
}