using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SystemOne.Models;

public sealed class SystemOneRequestDto
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("state")]
    public required JsonElement State { get; init; }

    [JsonPropertyName("questions")]
    public required Dictionary<string, SystemOneQuestionDto> Questions { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SystemOneNoulQuestionDto), "noul")]
[JsonDerivedType(typeof(SystemOneChoiceQuestionDto), "choice")]
[JsonDerivedType(typeof(SystemOneScoreQuestionDto), "score")]
public abstract class SystemOneQuestionDto
{
    [JsonPropertyName("instructions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Instructions { get; init; }
}

public sealed class SystemOneNoulQuestionDto : SystemOneQuestionDto
{
    [JsonPropertyName("criteria")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? Criteria { get; init; }
}

public sealed class SystemOneChoiceQuestionDto : SystemOneQuestionDto
{
    [JsonPropertyName("criteria")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonElement>? Criteria { get; init; }
}

public sealed class SystemOneScoreQuestionDto : SystemOneQuestionDto
{
    [JsonPropertyName("criteria")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<JsonElement>? Criteria { get; init; }
}
