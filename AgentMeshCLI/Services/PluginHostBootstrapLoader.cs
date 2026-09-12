using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Configuration;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AgentMesh.Services
{
    public sealed class PluginHostBootstrapLoader(
        PluginHostConfiguration pluginHostConfiguration,
        PluginHostState pluginHostState,
        ILogger<PluginHostBootstrapLoader> logger)
    {
        public void LoadPlugins(IServiceCollection services)
        {
            var pluginsDirectory = GetPluginsDirectoryPath();

            if (!Directory.Exists(pluginsDirectory))
            {
                var msg = $"Plugins directory '{pluginsDirectory}' does not exist. Startup continues without plugin assemblies.";
                logger.LogInformation(msg);
                pluginHostState.AddDiagnostic(msg);
                return;
            }

            var pluginFiles = Directory
                .GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pluginFiles.Count == 0)
            {
                var msg = $"No plugin assemblies found in '{pluginsDirectory}'.";
                logger.LogInformation(msg);
                pluginHostState.AddDiagnostic(msg);
                return;
            }

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(pluginFile);
                    var bootstrapTypes = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(IAgentMeshPluginBootstrap).IsAssignableFrom(t))
                        .ToList();

                    if (bootstrapTypes.Count == 0)
                    {
                        logger.LogInformation("Plugin assembly discovered with no bootstrap type: {PluginPath}", pluginFile);
                        pluginHostState.AddDiagnostic($"Plugin assembly discovered with no bootstrap: '{Path.GetFileName(pluginFile)}'.");
                        continue;
                    }

                    foreach (var bootstrapType in bootstrapTypes)
                    {
                        if (Activator.CreateInstance(bootstrapType) is not IAgentMeshPluginBootstrap bootstrap)
                        {
                            pluginHostState.AddStartupError($"Plugin bootstrap could not be activated: '{bootstrapType.FullName}'.");
                            logger.LogError("Plugin bootstrap could not be activated. Plugin: {PluginPath}; Type: {BootstrapType}", pluginFile, bootstrapType.FullName);
                            continue;
                        }

                        bootstrap.RegisterServices(services);
                        logger.LogInformation("Plugin bootstrap invoked. Plugin: {PluginPath}; Bootstrap: {BootstrapType}", pluginFile, bootstrapType.FullName);
                        pluginHostState.AddDiagnostic($"Plugin bootstrap invoked for '{Path.GetFileName(pluginFile)}'.");
                    }
                }
                catch (Exception ex)
                {
                    pluginHostState.AddStartupError($"Plugin assembly failed to load: '{Path.GetFileName(pluginFile)}'.");
                    logger.LogError(ex, "Plugin assembly failed to load at startup. Plugin: {PluginPath}", pluginFile);
                }
            }
        }

        private string GetPluginsDirectoryPath()
        {
            if (Path.IsPathRooted(pluginHostConfiguration.PluginsPath))
            {
                return pluginHostConfiguration.PluginsPath;
            }

            return Path.Combine(AppContext.BaseDirectory, pluginHostConfiguration.PluginsPath);
        }
    }
}