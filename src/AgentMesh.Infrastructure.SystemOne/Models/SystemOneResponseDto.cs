using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SystemOne.Models;

public sealed class SystemOneResponseDto
{
    [JsonRequired]
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonRequired]
    [JsonPropertyName("answers")]
    public required Dictionary<string, SystemOneAnswerDto> Answers { get; init; }

    [JsonRequired]
    [JsonPropertyName("usage")]
    public required SystemOneUsageDto Usage { get; init; }
}

public sealed class SystemOneUsageDto
{
    [JsonRequired]
    [JsonPropertyName("input_tokens")]
    public required int InputTokens { get; init; }

    [JsonRequired]
    [JsonPropertyName("output_tokens")]
    public required int OutputTokens { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SystemOneNoulAnswerDto), "noul")]
[JsonDerivedType(typeof(SystemOneChoiceAnswerDto), "choice")]
[JsonDerivedType(typeof(SystemOneScoreAnswerDto), "score")]
public abstract class SystemOneAnswerDto
{
}

public sealed class SystemOneNoulAnswerDto : SystemOneAnswerDto
{
    [JsonRequired]
    [JsonPropertyName("noul")]
    public required double Noul { get; init; }
}

public sealed class SystemOneChoiceAnswerDto : SystemOneAnswerDto
{
    [JsonRequired]
    [JsonPropertyName("choice")]
    public required string Choice { get; init; }

    [JsonRequired]
    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }

    [JsonRequired]
    [JsonPropertyName("probabilities")]
    public required Dictionary<string, double> Probabilities { get; init; }
}

public sealed class SystemOneScoreAnswerDto : SystemOneAnswerDto
{
    [JsonRequired]
    [JsonPropertyName("score")]
    public required double Score { get; init; }

    [JsonRequired]
    [JsonPropertyName("confidence")]
    public required double Confidence { get; init; }

    [JsonRequired]
    [JsonPropertyName("legend")]
    public required Dictionary<string, JsonElement> Legend { get; init; }

    [JsonRequired]
    [JsonPropertyName("probabilities")]
    public required Dictionary<string, double> Probabilities { get; init; }
}
