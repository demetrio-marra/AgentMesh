#pragma warning disable OPENAI001
using AgentMesh.Contracts.Exceptions;
using AgentMesh.Contracts.Models.ChatMessages;
using AgentMesh.Infrastructure.ChatCompletions;
using OpenAI.Chat;
using Xunit;

namespace AgentMesh.Infrastructure.ChatCompletions.Tests;

public class ChatCompletionsClientTests
{
    [Fact]
    public async Task StringInputsBecomeUserMessages()
    {
        var stream = new FakeChatCompletionsStreamClient(new ChatCompletionsStreamUpdate(TextDelta: "answer"));
        var client = CreateClient(stream);

        var result = await client.GenerateResponseAsync(["first", "second"]);

        Assert.Equal("answer", result.Text);
        Assert.Equal(3, stream.Messages!.Length);
        Assert.IsType<SystemChatMessage>(stream.Messages[0]);
        Assert.Equal("base prompt", stream.Messages[0].Content[0].Text);
        Assert.IsType<UserChatMessage>(stream.Messages[1]);
        Assert.Equal("first", stream.Messages[1].Content[0].Text);
        Assert.IsType<UserChatMessage>(stream.Messages[2]);
        Assert.Equal("second", stream.Messages[2].Content[0].Text);
    }

    [Fact]
    public async Task RoleMessagesMergeSystemPromptsAndPreserveAssistant()
    {
        var stream = new FakeChatCompletionsStreamClient(new ChatCompletionsStreamUpdate(TextDelta: "answer"));
        var client = CreateClient(stream);
        AgentMessage[] messages =
        [
            new() { Role = AgentMessageRole.System, Content = "call prompt" },
            new() { Role = AgentMessageRole.User, Content = "question" },
            new() { Role = AgentMessageRole.Assistant, Content = "prior answer" }
        ];

        await client.GenerateResponseAsync(messages);

        Assert.Equal(3, stream.Messages!.Length);
        Assert.Equal("base prompt\ncall prompt", stream.Messages[0].Content[0].Text);
        Assert.IsType<UserChatMessage>(stream.Messages[1]);
        Assert.Equal("question", stream.Messages[1].Content[0].Text);
        Assert.IsType<AssistantChatMessage>(stream.Messages[2]);
        Assert.Equal("prior answer", stream.Messages[2].Content[0].Text);
    }

    [Fact]
    public async Task StreamAggregatesTextAndUsageAndUsesConfiguredTemperature()
    {
        var stream = new FakeChatCompletionsStreamClient(
            new ChatCompletionsStreamUpdate(TextDelta: "hel"),
            new ChatCompletionsStreamUpdate(TextDelta: "lo"),
            new ChatCompletionsStreamUpdate(TotalTokenCount: 8, InputTokenCount: 3, OutputTokenCount: 5));
        var client = CreateClient(stream);

        var result = await client.GenerateResponseAsync(["question"]);

        Assert.Equal("hello", result.Text);
        Assert.Equal(8, result.TotalTokenCount);
        Assert.Equal(3, result.InputTokenCount);
        Assert.Equal(5, result.OutputTokenCount);
        Assert.Equal(0.7f, stream.Options!.Temperature);
    }

    [Fact]
    public async Task EmptyStreamThrowsStructuredResponseFailure()
    {
        var client = CreateClient(new FakeChatCompletionsStreamClient(Array.Empty<ChatCompletionsStreamUpdate>()));

        await Assert.ThrowsAsync<BadStructuredResponseException>(() => client.GenerateResponseAsync(["question"]));
    }

    [Fact]
    public async Task CancellationPropagatesToStream()
    {
        var client = CreateClient(new FakeChatCompletionsStreamClient(new ChatCompletionsStreamUpdate(TextDelta: "answer")));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GenerateResponseAsync(["question"], cancellation.Token));
    }

    [Fact]
    public async Task ToolChoiceProviderErrorBecomesStructuredResponseFailure()
    {
        var providerError = new ClientResultException("Tool choice is none, but model called a tool");
        var client = CreateClient(new FakeChatCompletionsStreamClient(Array.Empty<ChatCompletionsStreamUpdate>(), providerError));

        var exception = await Assert.ThrowsAsync<BadStructuredResponseException>(() =>
            client.GenerateResponseAsync(["question"]));

        Assert.Equal(providerError.Message, exception.Message);
        Assert.Same(providerError, exception.InnerException);
    }

    private static ChatCompletionsClient CreateClient(FakeChatCompletionsStreamClient stream) => new(
        "model", "key", "https://example.test/v1", "0.7", "base prompt", stream);

    private sealed class FakeChatCompletionsStreamClient(
        ChatCompletionsStreamUpdate[] updates,
        Exception? failure = null) : IChatCompletionsStreamClient
    {
        public FakeChatCompletionsStreamClient(params ChatCompletionsStreamUpdate[] updates)
            : this(updates, null)
        {
        }

        public ChatMessage[]? Messages { get; private set; }
        public ChatCompletionOptions? Options { get; private set; }

        public async IAsyncEnumerable<ChatCompletionsStreamUpdate> CompleteChatStreamingAsync(
            IEnumerable<ChatMessage> messages,
            ChatCompletionOptions options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Messages = messages.ToArray();
            Options = options;

            foreach (var update in updates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
                await Task.Yield();
            }

            if (failure is not null)
            {
                throw failure;
            }
        }
    }

    private sealed class ClientResultException(string message) : Exception(message);
}
#pragma warning restore OPENAI001