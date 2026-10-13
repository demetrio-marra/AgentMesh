using System.Net;
using System.Text.Json;
using AgentMesh.Contracts.Exceptions;
using AgentMesh.Infrastructure.JSSandbox;
using Xunit;

namespace AgentMesh.Infrastructure.JSSandbox.Tests;

public class SESJSSandboxClientTests
{
    [Fact]
    public async Task RunCodeSendsExpectedPayloadAndMapsSuccessfulResult()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, """
            {"id":"execution-1","isError":false,"executionResult":"2"}
            """));
        var client = CreateClient(handler);

        var result = await client.RunCode("agent-1", "1 + 1");

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/api/Execution/sandbox%20name", handler.Request.RequestUri!.PathAndQuery);
        using var payload = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal("1 + 1", payload.RootElement.GetProperty("codeToRun").GetString());
        Assert.Equal("agent-1", payload.RootElement.GetProperty("userAgentId").GetString());
        Assert.Equal("execution-1", result.ExecutionId);
        Assert.Equal("2", result.Result);
    }

    [Fact]
    public async Task BadRequestMapsSandboxErrorDetails()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.BadRequest, """
            {"errorType":"SyntaxError","error":"Unexpected token"}
            """));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<CodeSandboxCallException>(() => client.RunCode("agent", "invalid"));

        Assert.Equal("SyntaxError", exception.ErrorType);
        Assert.Equal("Unexpected token", exception.Message);
    }

    [Fact]
    public async Task ErrorResultIsRejected()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, """
            {"id":"execution-2","isError":true,"executionResult":"runtime failure"}
            """));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.RunCode("agent", "throw"));

        Assert.Equal("Sandbox execution error: runtime failure", exception.Message);
    }

    [Fact]
    public async Task OtherFailedStatusesArePropagated()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.RunCode("agent", "code"));
    }

    private static SESJSSandboxClient CreateClient(HttpMessageHandler handler) => new(
        new SESJSSandboxConfiguration
        {
            SandboxServiceURL = "https://sandbox.test/",
            SandboxName = "sandbox name"
        },
        handler);

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) => new(statusCode)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> createResponse) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return createResponse(request);
        }
    }
}