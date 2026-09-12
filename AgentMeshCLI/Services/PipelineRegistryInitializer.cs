using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Services
{
    public sealed class PipelineRegistryInitializer(
        IServiceProvider serviceProvider,
        PluginHostState pluginHostState,
        ILogger<PipelineRegistryInitializer> logger)
    {
        public void Initialize()
        {
            using var scope = serviceProvider.CreateScope();
            var pipelines = scope.ServiceProvider.GetServices<IChatRequestPipeline>().ToList();

            var duplicateNames = pipelines
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => string.IsNullOrWhiteSpace(g.Key) || g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            pluginHostState.SetPipelineValidationState(pipelines.Count, duplicateNames);

            if (duplicateNames.Count > 0)
            {
                logger.LogError("Duplicate or invalid pipeline names detected at startup: {DuplicateNames}", string.Join(", ", duplicateNames.Select(n => n ?? "<null>")));
                return;
            }

            logger.LogInformation("Pipeline registry initialized with {PipelineCount} pipeline(s): {PipelineNames}", pipelines.Count, string.Join(", ", pipelines.Select(p => p.Name)));
        }
    }
}