namespace AgentMesh.Exceptions;

public sealed class PipelineRoutingException(int statusCode, string title, string detail) : Exception(detail)
{
    public int StatusCode { get; } = statusCode;

    public string Title { get; } = title;

    public string Detail { get; } = detail;

    public static PipelineRoutingException PluginConfigurationInvalid() => new(
        503,
        "Plugin configuration error",
        "Pipeline plugins are not in a valid state. Redeploy the service after resolving the plugin issue.");

    public static PipelineRoutingException NoPipelinesLoaded() => new(
        503,
        "Service unavailable",
        "No pipelines loaded");

}