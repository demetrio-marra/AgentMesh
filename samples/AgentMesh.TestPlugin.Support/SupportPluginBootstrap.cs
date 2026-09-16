using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.TestPlugin.Support
{
    public sealed class SupportPluginBootstrap : IAgentMeshPluginBootstrap
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IChatRequestPipeline, SupportPipeline>();
        }
    }
}
