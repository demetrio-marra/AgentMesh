using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Infrastructure.SystemOne.Models;

public sealed class SystemOneProviderErrorDto
{
    [JsonPropertyName("detail")]
    public JsonElement? Detail { get; init; }
}
