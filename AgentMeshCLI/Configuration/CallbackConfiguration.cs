namespace AgentMesh.Configuration;

public sealed class CallbackConfiguration
{
    public const string SectionName = "Callbacks";

    public string BaseUrl { get; set; } = "http://localhost:5249";
    public string ListenUrl { get; set; } = "http://localhost:5249";
}
