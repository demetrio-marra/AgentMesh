using System.Text.Json;
using AgentMesh.Contracts.Exceptions;
using AgentMesh.Contracts.Models.Knowledge;
using AgentMesh.Contracts.Models.Rerank;
using Xunit;

namespace AgentMesh.Contracts.Tests;

public class ContractModelTests
{
    [Fact]
    public void KnowledgeQueryResultDefaultsToEmptyCollections()
    {
        var result = new KnowledgeQueryResult();

        Assert.Empty(result.Contents);
        Assert.Empty(result.Entities);
        Assert.Empty(result.Relations);
    }

    [Fact]
    public void RerankQueryRoundTripsThroughJson()
    {
        var query = new RerankInputQuery
        {
            Query = "question",
            CandidateDocuments = ["first", "second"],
            TopN = 1
        };

        var json = JsonSerializer.Serialize(query);
        var restored = JsonSerializer.Deserialize<RerankInputQuery>(json);

        Assert.Equal(query.Query, restored.Query);
        Assert.Equal(query.CandidateDocuments, restored.CandidateDocuments);
        Assert.Equal(query.TopN, restored.TopN);
    }

    [Fact]
    public void RerankResultCalculatesTotalTokens()
    {
        var result = new RerankResult
        {
            RerankedDocuments = [new RerankResultItem(1, "document", 0.9)],
            PromptTokens = 7,
            CompletionTokens = 3
        };

        Assert.Equal(10, result.TotalTokens);
        Assert.Equal("document", result.RerankedDocuments[0].Document);
    }

    [Fact]
    public void StructuredResponseExceptionPreservesRawOutputAndInnerException()
    {
        var innerException = new FormatException("invalid payload");
        var exception = new BadStructuredResponseException("raw output", "invalid response", innerException);

        Assert.IsAssignableFrom<BadAgentResponseException>(exception);
        Assert.Equal("invalid response", exception.Message);
        Assert.Equal("raw output", exception.RawOutput);
        Assert.Same(innerException, exception.InnerException);
    }
}