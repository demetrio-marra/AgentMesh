using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.TestPlugin.Echo
{
    public sealed class EchoPluginBootstrap : IAgentMeshPluginBootstrap
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IChatRequestPipeline, EchoPipeline>();
        }
    }
}
