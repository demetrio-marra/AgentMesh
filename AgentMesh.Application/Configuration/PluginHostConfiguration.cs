namespace AgentMesh.Configuration
{
    public sealed class PluginHostConfiguration
    {
        public const string SectionName = "PluginHost";

        public string PluginsPath { get; set; } = "Plugins";
    }
}