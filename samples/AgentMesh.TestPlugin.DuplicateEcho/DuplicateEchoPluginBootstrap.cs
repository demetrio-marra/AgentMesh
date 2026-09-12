using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.TestPlugin.DuplicateEcho
{
    public sealed class DuplicateEchoPluginBootstrap : IAgentMeshPluginBootstrap
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IChatRequestPipeline, DuplicateEchoPipeline>();
        }
    }
}
