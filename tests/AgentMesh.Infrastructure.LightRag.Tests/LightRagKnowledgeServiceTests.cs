using System.Net;
using System.Text.Json;
using AgentMesh.Contracts.Configuration;
using AgentMesh.Contracts.Models.Knowledge;
using AgentMesh.Contracts.Utils;
using AgentMesh.Infrastructure.LightRag.Configuration;
using AgentMesh.Infrastructure.LightRag.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AgentMesh.Infrastructure.LightRag.Tests;

public class LightRagKnowledgeServiceTests
{
    [Fact]
    public async Task QueryBuildsRequestAndMapsDocumentsEntitiesAndRelations()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, """
            {
              "data": {
                "chunks": [{"chunk_id":"chunk-1","content":"source text","file_path":"docs/source.md"}],
                "entities": [{"entity_name":"AgentMesh","entity_type":"framework","description":"orchestrator","source_id":"chunk-1"}],
                "relationships": [{"src_id":"AgentMesh","tgt_id":"Pipeline","description":"runs","keywords":"workflow","source_id":"chunk-1"}]
              }
            }
            """));
        using var client = new HttpClient(handler);
        var service = CreateService(client, apiKey: "secret", maxTopK: 3);

        var result = await service.QueryKnowledgeAsync(new KnowledgeQuery
        {
            QueryText = "how it works",
            QueryRetrievalKind = KnowledgeQueryRetrievalKind.SemanticOnly,
            MaxResults = 8,
            PrimaryRelevanceKeywords = ["agent"],
            SecondaryRelevanceKeywords = ["pipeline"]
        });

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(new Uri("https://lightrag.test/query/data"), handler.Request.RequestUri);
        Assert.Equal("secret", handler.Request.Headers.GetValues("X-API-Key").Single());
        using var request = JsonDocument.Parse(handler.RequestBody!);
        var requestRoot = request.RootElement;
        Assert.Equal("how it works", requestRoot.GetProperty("query").GetString());
        Assert.Equal("naive", requestRoot.GetProperty("mode").GetString());
        Assert.Equal(3, requestRoot.GetProperty("top_k").GetInt32());
        Assert.True(requestRoot.GetProperty("include_references").GetBoolean());
        Assert.True(requestRoot.GetProperty("include_chunk_content").GetBoolean());
        Assert.Equal("agent", requestRoot.GetProperty("hl_keywords")[0].GetString());
        Assert.Equal("pipeline", requestRoot.GetProperty("ll_keywords")[0].GetString());

        var content = Assert.Single(result.Contents);
        Assert.Equal("chunk-1", content.Id);
        Assert.Equal("source text", content.Content);
        Assert.Equal("docs/source.md", content.Source);
        var entity = Assert.Single(result.Entities);
        Assert.Equal("AgentMesh", entity.Entity);
        Assert.Equal(content.Id, entity.ContentItem.Id);
        Assert.Equal(content.Content, entity.ContentItem.Content);
        Assert.Equal(content.Source, entity.ContentItem.Source);
        var relation = Assert.Single(result.Relations);
        Assert.Equal("AgentMesh", relation.EntityRelationFrom);
        Assert.Equal("Pipeline", relation.EntityRelationTo);
        Assert.Equal(content.Id, relation.ContentItem.Id);
        Assert.Equal(content.Content, relation.ContentItem.Content);
        Assert.Equal(content.Source, relation.ContentItem.Source);
    }

    [Fact]
    public async Task IncludeFlagsSuppressEntityAndRelationshipMapping()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, """
            {"data":{"chunks":[],"entities":[{"entity_name":"hidden"}],"relationships":[{"src_id":"A","tgt_id":"B"}]}}
            """));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var result = await service.QueryKnowledgeAsync(new KnowledgeQuery
        {
            QueryText = "query",
            IncludeEntities = false,
            IncludeRelations = false
        });

        Assert.Empty(result.Entities);
        Assert.Empty(result.Relations);
    }

    [Fact]
    public async Task NullResponseDataReturnsEmptyResult()
    {
        var handler = new RecordingHandler(_ => JsonResponse(HttpStatusCode.OK, "{\"data\":null}"));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var result = await service.QueryKnowledgeAsync(new KnowledgeQuery { QueryText = "query" });

        Assert.Empty(result.Contents);
        Assert.Empty(result.Entities);
        Assert.Empty(result.Relations);
    }

    [Fact]
    public async Task FailedHttpStatusIsPropagatedWithoutRetriesWhenDisabled()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        using var client = new HttpClient(handler);
        var service = CreateService(client, retryCount: 0);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.QueryKnowledgeAsync(new KnowledgeQuery
        {
            QueryText = "query"
        }));

        Assert.Equal(1, handler.RequestCount);
    }

    private static LightRagKnowledgeService CreateService(HttpClient client, string? apiKey = null, int maxTopK = 10, int retryCount = 0) => new(
        client,
        new LightRagServiceConfiguration
        {
            BaseUrl = "https://lightrag.test/",
            ApiKey = apiKey,
            MaxTopK = maxTopK
        },
        new Resilience(new ResilienceConfiguration { RetryCount = retryCount }),
        NullLogger<LightRagKnowledgeService>.Instance);

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