using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Services;
using AgentMesh.Contracts;
using AgentMesh.Contracts.Configuration;
using AgentMesh.Contracts.Exceptions;
using AgentMesh.Contracts.Models.AgentMemory;
using AgentMesh.Contracts.Models.ChatClient;
using AgentMesh.Contracts.Models.ChatMessages;
using AgentMesh.Contracts.Models.CodeSandbox;
using AgentMesh.Contracts.Utils;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentMesh.Application.Tests;

public class AgentAndExecutorTests
{
    [Fact]
    public async Task AgentRetriesMalformedStructuredResponseAndMapsUsage()
    {
        var client = new FakeChatClient("not-an-integer", "42");
        var agent = new IntegerAgent(client, retryCount: 1);

        var response = await agent.ExecuteAsync(new Dictionary<Type, object?>());

        Assert.Equal(42, response.Result);
        Assert.Equal(2, client.CallCount);
        Assert.Equal(12, response.TotalTokenCount);
        Assert.Equal(5, response.InputTokenCount);
        Assert.Equal(7, response.OutputTokenCount);
    }

    [Fact]
    public async Task AgentSurfacesParseFailureWhenRetriesAreExhausted()
    {
        var client = new FakeChatClient("invalid");
        var agent = new IntegerAgent(client, retryCount: 0);

        var exception = await Assert.ThrowsAsync<BadStructuredResponseException>(() =>
            agent.ExecuteAsync(new Dictionary<Type, object?>()));

        Assert.Equal("invalid", exception.RawOutput);
        Assert.Equal(1, client.CallCount);
    }

    private sealed class IntegerAgent(FakeChatClient client, int retryCount)
        : AbstractAgent<int>(
            NullLogger<IntegerAgent>.Instance,
            "integer-agent",
            new FakeChatClientFactory(client),
            new Resilience(new ResilienceConfiguration { RetryCount = retryCount }),
            new EmptyInputSerializer())
    {
        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration() => [];

        protected override int ParseStructuredResponse(string rawResponseText)
        {
            if (int.TryParse(rawResponseText, out var value))
            {
                return value;
            }

            throw new BadStructuredResponseException(rawResponseText, "Expected an integer.");
        }
    }

    private sealed class EmptyInputSerializer : IAgentInputSerializer
    {
        public IEnumerable<AgentMessage> SerializeInput(
            IReadOnlyDictionary<Type, object?> parameters,
            IEnumerable<AgentInputParameterConfiguration> parameterTagsConfiguration) => [];
    }

    private sealed class FakeChatClientFactory(IChatClient client) : IChatClientFactory
    {
        public IChatClient CreateChatClient(string agentUniqueRole) => client;
    }

    private sealed class FakeChatClient(params string[] responses) : IChatClient
    {
        private int _nextResponse;

        public int CallCount => _nextResponse;

        public Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<string> userInput, CancellationToken cancellationToken = default) => NextResponse();

        public Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default) => NextResponse();

        private Task<ChatClientResponse> NextResponse()
        {
            var response = responses[_nextResponse++];
            return Task.FromResult(new ChatClientResponse
            {
                Text = response,
                TotalTokenCount = 12,
                InputTokenCount = 5,
                OutputTokenCount = 7
            });
        }
    }
}

public class ExecutorTests
{
    [Fact]
    public async Task MemoryExecutorUsesUserIdentityAndMapsQueryResults()
    {
        var service = new FakeMemoryService();
        var executor = new AgentMemoryExecutor(
            service,
            NullLogger<AgentMemoryExecutor>.Instance,
            new UserConfiguration { AgentId = "user-1" });

        await executor.SaveAsync([new ContextMessage { Text = "hello" }]);
        var output = await executor.GetAsync(new AgentMemoryRetrieverInput { Query = "find it" });
        var item = Assert.Single(output.Items);

        Assert.Equal("user-1", service.UserId);
        Assert.Equal("find it", service.QueryText);
        Assert.Equal("remembered", item.Memory);
        Assert.Equal(0.8, item.Confidence);
    }

    [Fact]
    public async Task SandboxExecutorForwardsIdentityAndCodeAndReturnsResult()
    {
        var service = new FakeSandbox();
        var executor = new JSSandboxExecutor(
            service,
            new UserConfiguration { AgentId = "user-2" },
            NullLogger<JSSandboxExecutor>.Instance);
        var expected = new CodeSandboxOutput { Result = "done", ExecutionId = "run-1" };

        service.Result = expected;
        var output = await executor.ExecuteAsync(new CodeSandboxInput { Code = "1 + 1" });

        Assert.Equal("user-2", service.AgentId);
        Assert.Equal("1 + 1", service.Code);
        Assert.Same(expected, output);
    }

    private sealed class FakeMemoryService : IAgentMemoryService
    {
        public string? UserId { get; private set; }
        public string? QueryText { get; private set; }

        public Task AddConversationHistory(string userId, IEnumerable<ContextMessage> conversationHistory, CancellationToken cancellationToken = default)
        {
            UserId = userId;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<AgentMemoryQueryResultItem>> Query(string userId, string query, CancellationToken cancellationToken = default)
        {
            UserId = userId;
            QueryText = query;
            return Task.FromResult<IEnumerable<AgentMemoryQueryResultItem>>(
                [new AgentMemoryQueryResultItem { Memory = "remembered", Confidence = 0.8 }]);
        }
    }

    private sealed class FakeSandbox : IJSSandbox
    {
        public string? AgentId { get; private set; }
        public string? Code { get; private set; }
        public CodeSandboxOutput Result { get; set; } = new();

        public Task<CodeSandboxOutput> RunCode(string agentId, string code)
        {
            AgentId = agentId;
            Code = code;
            return Task.FromResult(Result);
        }
    }
}