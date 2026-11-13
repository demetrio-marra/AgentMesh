using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Infrastructure.SystemOne.Models;
using Xunit;

namespace AgentMesh.Infrastructure.SystemOne.Tests;

public class SystemOneTransportModelTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void RequestSerializesMixedQuestionsWithoutChangingUserKeysOrJsonShapes()
    {
        using var stateDocument = JsonDocument.Parse("""{"message":["hello",{"count":2}]}""");
        using var instructionsDocument = JsonDocument.Parse("""{"rule":"keep"}""");
        using var nullDocument = JsonDocument.Parse("null");
        var request = new SystemOneRequestDto
        {
            Model = "jev-1.14.0",
            State = stateDocument.RootElement.Clone(),
            Questions = new Dictionary<string, SystemOneQuestionDto>
            {
                ["userNoulKey"] = new SystemOneNoulQuestionDto
                {
                    Instructions = instructionsDocument.RootElement.Clone(),
                    Criteria = new Dictionary<string, JsonElement> { ["true"] = nullDocument.RootElement.Clone() }
                },
                ["userChoiceKey"] = new SystemOneChoiceQuestionDto
                {
                    Criteria = new Dictionary<string, JsonElement> { ["customChoice"] = nullDocument.RootElement.Clone() }
                },
                ["userScoreKey"] = new SystemOneScoreQuestionDto
                {
                    Criteria = [JsonSerializer.SerializeToElement("low"), JsonSerializer.SerializeToElement("high")]
                }
            }
        };

        using var serialized = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));
        var root = serialized.RootElement;
        Assert.Equal("jev-1.14.0", root.GetProperty("model").GetString());
        Assert.Equal(JsonValueKind.Array, root.GetProperty("state").GetProperty("message").ValueKind);
        Assert.Equal(3, root.GetProperty("questions").EnumerateObject().Count());
        Assert.True(root.GetProperty("questions").TryGetProperty("userNoulKey", out _));
        Assert.True(root.GetProperty("questions").TryGetProperty("userChoiceKey", out _));
        Assert.True(root.GetProperty("questions").TryGetProperty("userScoreKey", out _));
        Assert.Equal("noul", root.GetProperty("questions").GetProperty("userNoulKey").GetProperty("type").GetString());
        Assert.True(root.GetProperty("questions").GetProperty("userNoulKey").GetProperty("criteria").GetProperty("true").ValueKind == JsonValueKind.Null);
        Assert.Equal("choice", root.GetProperty("questions").GetProperty("userChoiceKey").GetProperty("type").GetString());
        Assert.True(root.GetProperty("questions").GetProperty("userChoiceKey").GetProperty("criteria").TryGetProperty("customChoice", out _));
        Assert.Equal("score", root.GetProperty("questions").GetProperty("userScoreKey").GetProperty("type").GetString());
    }

    [Fact]
    public void OptionalInstructionsAreOmittedButExplicitNullDescriptionsArePreserved()
    {
        using var nullDocument = JsonDocument.Parse("null");
        var request = new SystemOneRequestDto
        {
            Model = "model",
            State = JsonSerializer.SerializeToElement("text"),
            Questions = new Dictionary<string, SystemOneQuestionDto>
            {
                ["noul"] = new SystemOneNoulQuestionDto(),
                ["choice"] = new SystemOneChoiceQuestionDto
                {
                    Criteria = new Dictionary<string, JsonElement> { ["nullDescription"] = nullDocument.RootElement.Clone() }
                }
            }
        };

        using var serialized = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));
        var noul = serialized.RootElement.GetProperty("questions").GetProperty("noul");
        var choice = serialized.RootElement.GetProperty("questions").GetProperty("choice");

        Assert.False(noul.TryGetProperty("instructions", out _));
        Assert.False(noul.TryGetProperty("criteria", out _));
        Assert.Equal(JsonValueKind.Null, choice.GetProperty("criteria").GetProperty("nullDescription").ValueKind);
    }

    [Fact]
    public void ResponseDeserializesAllDocumentedAnswerDiscriminatorsAndFractionalScore()
    {
        var response = DeserializeResponse("""
            {
              "model":"jev-1.14.0",
              "answers":{
                "noulKey":{"type":"noul","noul":0.98},
                "choiceKey":{"type":"choice","choice":"customChoice","confidence":0.7,"probabilities":{"customChoice":1.0}},
                "scoreKey":{"type":"score","score":1.7,"confidence":0.55,"legend":{"0":"low","1":{"label":"high"}},"probabilities":{"0":0.2,"1":0.8}
              }},
              "usage":{"input_tokens":120,"output_tokens":12}
            }
            """);

        Assert.IsType<SystemOneNoulAnswerDto>(response.Answers["noulKey"]);
        Assert.Equal("customChoice", Assert.IsType<SystemOneChoiceAnswerDto>(response.Answers["choiceKey"]).Choice);
        var score = Assert.IsType<SystemOneScoreAnswerDto>(response.Answers["scoreKey"]);
        Assert.Equal(1.7, score.Score);
        Assert.Equal(JsonValueKind.Object, score.Legend["1"].ValueKind);
        Assert.Equal(120, response.Usage.InputTokens);
        Assert.Equal(12, response.Usage.OutputTokens);
    }

    [Fact]
    public void MissingRequiredUsageAndAnswerFieldsAreRejected()
    {
        Assert.Throws<JsonException>(() => DeserializeResponse("""
            {"model":"model","answers":{"q":{"type":"noul","noul":0.5}},"usage":{"output_tokens":1}}
            """));
        Assert.Throws<JsonException>(() => DeserializeResponse("""
            {"model":"model","answers":{"q":{"type":"noul"}},"usage":{"input_tokens":1,"output_tokens":1}}
            """));
    }

    private static SystemOneResponseDto DeserializeResponse(string json) =>
        JsonSerializer.Deserialize<SystemOneResponseDto>(json, JsonOptions)
        ?? throw new JsonException("Response cannot be null.");
}
