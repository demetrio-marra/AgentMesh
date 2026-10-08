#pragma warning disable OPENAI001
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Infrastructure.Responses;
using OpenAI.Responses;
using Xunit;

namespace AgentMesh.Infrastructure.Responses.Tests;

public class ResponsesApiClientTests
{
    [Fact]
    public async Task StringInputsBecomeUserItems()
    {
        var stream = new FakeResponsesStreamClient(new ResponsesStreamUpdate(OutputTextDelta: "answer"));
        var client = CreateClient(stream);

        var result = await client.GenerateResponseAsync(["first", "second"]);

        Assert.Equal("answer", result.Text);
        Assert.Equal(3, stream.Options!.InputItems.Count);
        Assert.Equal("system", ((MessageResponseItem)stream.Options.InputItems[0]).Role.ToString(), ignoreCase: true);
        Assert.Equal("user", ((MessageResponseItem)stream.Options.InputItems[1]).Role.ToString(), ignoreCase: true);
        Assert.Equal("first", ((MessageResponseItem)stream.Options.InputItems[1]).Content[0].Text);
        Assert.Equal("second", ((MessageResponseItem)stream.Options.InputItems[2]).Content[0].Text);
    }

    [Fact]
    public async Task RoleMessagesPreserveAssistantAndMergeSystemPrompts()
    {
        var stream = new FakeResponsesStreamClient(new ResponsesStreamUpdate(OutputTextDelta: "answer"));
        var client = CreateClient(stream);
        AgentMessage[] messages =
        [
            new() { Role = AgentMessageRole.System, Content = "call prompt" },
            new() { Role = AgentMessageRole.User, Content = "question" },
            new() { Role = AgentMessageRole.Assistant, Content = "prior answer" }
        ];

        await client.GenerateResponseAsync(messages);

        var items = stream.Options!.InputItems.Cast<MessageResponseItem>().ToArray();
        Assert.Equal("base prompt\ncall prompt", items[0].Content[0].Text);
        Assert.Equal("user", items[1].Role.ToString(), ignoreCase: true);
        Assert.Equal("assistant", items[2].Role.ToString(), ignoreCase: true);
        Assert.Equal("prior answer", items[2].Content[0].Text);
    }

    [Fact]
    public async Task StreamAggregatesTextAndUsage()
    {
        var stream = new FakeResponsesStreamClient(
            new ResponsesStreamUpdate(OutputTextDelta: "hel"),
            new ResponsesStreamUpdate(OutputTextDelta: "lo"),
            new ResponsesStreamUpdate(TotalTokenCount: 8, InputTokenCount: 3, OutputTokenCount: 5));
        var client = CreateClient(stream);

        var result = await client.GenerateResponseAsync(["question"]);

        Assert.Equal("hello", result.Text);
        Assert.Equal(8, result.TotalTokenCount);
        Assert.Equal(3, result.InputTokenCount);
        Assert.Equal(5, result.OutputTokenCount);
        Assert.True(stream.Options!.StreamingEnabled);
        Assert.Equal(0.7f, stream.Options.Temperature);
        Assert.Equal(ResponseReasoningEffortLevel.High, stream.Options.ReasoningOptions!.ReasoningEffortLevel);
    }

    [Fact]
    public async Task ReasoningUpdatesAreCapturedButNotReturned()
    {
        var stream = new FakeResponsesStreamClient(
            new ResponsesStreamUpdate(ReasoningTextDelta: "private reasoning"),
            new ResponsesStreamUpdate(OutputTextDelta: "public answer"));
        var client = CreateClient(stream);

        var result = await client.GenerateResponseAsync(["question"]);

        Assert.Equal("public answer", result.Text);
        Assert.Equal("private reasoning", client.LastReasoning);
    }

    [Fact]
    public async Task CancellationPropagates()
    {
        var stream = new FakeResponsesStreamClient(new ResponsesStreamUpdate(OutputTextDelta: "answer"));
        var client = CreateClient(stream);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GenerateResponseAsync(["question"], cancellation.Token));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyOutputThrowsStructuredResponseFailure(string output)
    {
        var stream = new FakeResponsesStreamClient(new ResponsesStreamUpdate(OutputTextDelta: output));
        var client = CreateClient(stream);

        await Assert.ThrowsAsync<BadStructuredResponseException>(() => client.GenerateResponseAsync(["question"]));
    }

    [Fact]
    public void UnsupportedReasoningEffortIsRejected()
    {
        var stream = new FakeResponsesStreamClient();

        Assert.Throws<ArgumentException>(() => new ResponsesApiClient(
            "model", "key", "https://example.test/v1", "0.7", "prompt", "unsupported", stream));
    }

    [Theory]
    [InlineData("not-a-uri", "0.7")]
    [InlineData("https://example.test/v1", "2.1")]
    [InlineData("https://example.test/v1", "not-a-number")]
    public void InvalidEndpointOrTemperatureIsRejected(string endpoint, string temperature)
    {
        var stream = new FakeResponsesStreamClient();

        Assert.ThrowsAny<ArgumentException>(() => new ResponsesApiClient(
            "model", "key", endpoint, temperature, "prompt", "high", stream));
    }

    private static ResponsesApiClient CreateClient(FakeResponsesStreamClient stream) => new(
        "model", "key", "https://example.test/v1", "0.7", "base prompt", "high", stream);

    private sealed class FakeResponsesStreamClient(params ResponsesStreamUpdate[] updates) : IResponsesStreamClient
    {
        public CreateResponseOptions? Options { get; private set; }

        public async IAsyncEnumerable<ResponsesStreamUpdate> CreateResponseStreamingAsync(
            CreateResponseOptions options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Options = options;
            foreach (var update in updates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
                await Task.Yield();
            }
        }
    }
}
#pragma warning restore OPENAI001