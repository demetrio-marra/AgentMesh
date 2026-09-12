using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Services
{
    public interface IAgentMeshPluginBootstrap
    {
        void RegisterServices(IServiceCollection services);
    }
}