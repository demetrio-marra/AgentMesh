using System.Net;
using System.Text.Json;
using AgentMesh.Contracts.Configuration;
using AgentMesh.Contracts.Models.ChatMessages;
using AgentMesh.Contracts.Utils;
using AgentMesh.Infrastructure.Mem0;
using AgentMesh.Models;
using Xunit;

namespace AgentMesh.Infrastructure.Mem0.Tests;

public class Mem0AgentMemoryServiceTests
{
    [Fact]
    public async Task AddHistoryTrimsMessagesAndMapsRoles()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        await service.AddConversationHistory("user-1",
        [
            new ContextMessage { Role = ContextMessageRole.User, Text = "  hello  " },
            new ContextMessage { Role = ContextMessageRole.Assistant, Text = "  reply " },
            new ContextMessage { Role = ContextMessageRole.User, Text = "  " }
        ]);

        Assert.Equal(new Uri("https://mem0.test/memories"), handler.Request!.RequestUri);
        using var body = JsonDocument.Parse(handler.RequestBody!);
        var root = body.RootElement;
        Assert.Equal("user-1", root.GetProperty("user_id").GetString());
        Assert.False(root.TryGetProperty("agent_id", out _));
        var messages = root.GetProperty("messages").EnumerateArray().ToArray();
        Assert.Equal(2, messages.Length);
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal("hello", messages[0].GetProperty("content").GetString());
        Assert.Equal("assistant", messages[1].GetProperty("role").GetString());
        Assert.Equal("reply", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task EmptyHistoryDoesNotSendRequest()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("No request expected."));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        await service.AddConversationHistory("user-1", [new ContextMessage { Text = " " }]);

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task SearchRequestAndResultsAreMapped()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, """
            {"results":[{"id":"memory-1","memory":"likes tea","score":0.85}]}
            """));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var result = await service.Query("user-2", "preferences");

        Assert.Equal(new Uri("https://mem0.test/search"), handler.Request!.RequestUri);
        using var body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("preferences", body.RootElement.GetProperty("query").GetString());
        Assert.Equal("user-2", body.RootElement.GetProperty("user_id").GetString());
        var item = Assert.Single(result);
        Assert.Equal("likes tea", item.Memory);
        Assert.Equal(0.85f, (float)item.Confidence!.Value);
    }

    [Fact]
    public async Task SearchWithNullResultsReturnsEmptyCollection()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, "{\"results\":null}"));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var result = await service.Query("user", "query");

        Assert.Empty(result);
    }

    [Fact]
    public async Task FailedHttpStatusIsPropagatedWithoutRetries()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.Query("user", "query"));

        Assert.Equal(1, handler.RequestCount);
    }

    private static Mem0AgentMemoryService CreateService(HttpClient client) => new(
        client,
        new AgentMemoryServiceConfiguration
        {
            BaseUrl = "https://mem0.test/",
            TimeoutSeconds = 4
        },
        new Resilience(new ResilienceConfiguration { RetryCount = 0 }));

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> createResponse) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestCount++;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return createResponse(request);
        }
    }
}