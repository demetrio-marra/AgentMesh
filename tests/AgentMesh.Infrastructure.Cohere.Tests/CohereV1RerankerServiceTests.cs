using System.Net;
using System.Text.Json;
using AgentMesh.Contracts.Models.Rerank;
using AgentMesh.Infrastructure.Cohere;
using Xunit;

namespace AgentMesh.Infrastructure.Cohere.Tests;

public class CohereV1RerankerServiceTests
{
    [Fact]
    public async Task RequestFiltersBlankDocumentsAndMapsRankingAndUsage()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"data":[{"index":1,"document":"second","relevance_score":0.92}],"usage":{"completion_tokens":2,"prompt_tokens":5,"total_tokens":7}}
                """, System.Text.Encoding.UTF8, "application/json")
        });
        using var client = new HttpClient(handler);
        var service = new CohereV1RerankerService(client, CreateConfiguration());

        var result = await service.RerankAsync(new RerankInputQuery
        {
            Query = "query",
            CandidateDocuments = ["first", " ", "second"],
            TopN = 1
        });

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(new Uri("https://cohere.test/v1/rerank"), handler.Request.RequestUri);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("secret", handler.Request.Headers.Authorization.Parameter);

        using var requestJson = JsonDocument.Parse(handler.RequestBody!);
        var requestRoot = requestJson.RootElement;
        Assert.Equal("query", requestRoot.GetProperty("query").GetString());
        Assert.Equal("rerank-model", requestRoot.GetProperty("model").GetString());
        Assert.Equal(1, requestRoot.GetProperty("top_n").GetInt32());
        Assert.True(requestRoot.GetProperty("return_documents").GetBoolean());
        Assert.Equal(new[] { "first", "second" }, requestRoot.GetProperty("documents").EnumerateArray().Select(item => item.GetString()));

        var item = Assert.Single(result.RerankedDocuments);
        Assert.Equal(1, item.DocumentIndex);
        Assert.Equal("second", item.Document);
        Assert.Equal(0.92, item.Score);
        Assert.Equal(2, result.CompletionTokens);
        Assert.Equal(5, result.PromptTokens);
        Assert.Equal(7, result.TotalTokens);
    }

    [Fact]
    public async Task EmptyDocumentListReturnsWithoutSendingRequest()
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("No request expected."));
        using var client = new HttpClient(handler);
        var service = new CohereV1RerankerService(client, CreateConfiguration());

        var result = await service.RerankAsync(new RerankInputQuery
        {
            Query = "query",
            CandidateDocuments = ["", "  "]
        });

        Assert.Null(handler.Request);
        Assert.Empty(result.RerankedDocuments);
        Assert.Equal(0, result.TotalTokens);
    }

    [Fact]
    public async Task FailedHttpStatusIsPropagated()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        using var client = new HttpClient(handler);
        var service = new CohereV1RerankerService(client, CreateConfiguration());

        await Assert.ThrowsAsync<HttpRequestException>(() => service.RerankAsync(new RerankInputQuery
        {
            Query = "query",
            CandidateDocuments = ["document"]
        }));
    }

    private static CohereV1RerankerServiceConfiguration CreateConfiguration() => new()
    {
        BaseUrl = "https://cohere.test/",
        ApiKey = "secret",
        Model = "rerank-model",
        TimeoutSeconds = 5
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