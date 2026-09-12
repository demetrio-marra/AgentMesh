namespace AgentMesh.Application.Services.Pipelines
{
    public sealed class PipelineRoutingException(int statusCode, string title, string detail) : Exception(detail)
    {
        public int StatusCode { get; } = statusCode;
        public string Title { get; } = title;
        public string Detail { get; } = detail;

        public static PipelineRoutingException PluginConfigurationInvalid()
        {
            return new PipelineRoutingException(
                503,
                "Plugin configuration error",
                "Pipeline plugins are not in a valid state. Redeploy the service after resolving the plugin issue.");
        }

        public static PipelineRoutingException NoPipelinesLoaded()
        {
            return new PipelineRoutingException(
                503,
                "Service unavailable",
                "No pipelines loaded");
        }

        public static PipelineRoutingException PipelineNameRequired()
        {
            return new PipelineRoutingException(
                400,
                "Bad request",
                "pipeline name is required");
        }

        public static PipelineRoutingException PipelineNotFound()
        {
            return new PipelineRoutingException(
                404,
                "Pipeline not found",
                "The requested pipeline was not found.");
        }
    }
}