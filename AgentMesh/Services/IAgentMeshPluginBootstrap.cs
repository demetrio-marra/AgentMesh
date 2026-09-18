using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMesh.Services
{
    public interface IAgentMeshPluginBootstrap
    {
        void RegisterServices(IServiceCollection services, IConfiguration configuration);
    }
}