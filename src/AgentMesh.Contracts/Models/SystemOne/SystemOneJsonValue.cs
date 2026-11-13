using System.Text.Json;

namespace AgentMesh.Contracts.Models.SystemOne;

public sealed class SystemOneJsonValue
{
    public SystemOneJsonValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw new ArgumentException("A defined JSON value is required.", nameof(value));
        }

        Value = value.Clone();
    }

    public JsonElement Value { get; }

    public static SystemOneJsonValue Null { get; } = new(JsonSerializer.SerializeToElement<object?>(null));

    public static SystemOneJsonValue Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new SystemOneJsonValue(document.RootElement);
    }

    public static SystemOneJsonValue From<T>(T value, JsonSerializerOptions? options = null) =>
        new(JsonSerializer.SerializeToElement(value, options));
}
