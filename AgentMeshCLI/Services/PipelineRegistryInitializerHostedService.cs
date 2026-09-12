using Microsoft.Extensions.Hosting;

namespace AgentMesh.Services
{
    public sealed class PipelineRegistryInitializerHostedService(PipelineRegistryInitializer initializer) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            initializer.Initialize();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}