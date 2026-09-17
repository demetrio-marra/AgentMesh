namespace AgentMesh.Configuration;

public sealed class ApiClientConfiguration
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string ApiKey { get; set; } = string.Empty;
    public string HeaderName { get; set; } = "X-Api-Key";
}
