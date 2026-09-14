using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Configuration;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AgentMesh.Services
{
    public sealed class PluginHostBootstrapLoader(PluginHostConfiguration pluginHostConfiguration, PluginHostState pluginHostState, ILogger<PluginHostBootstrapLoader> logger)
    {
        public void LoadPlugins(IServiceCollection services)
        {
            var pluginsDirectory = Path.IsPathRooted(pluginHostConfiguration.PluginsPath)
                ? pluginHostConfiguration.PluginsPath
                : Path.Combine(AppContext.BaseDirectory, pluginHostConfiguration.PluginsPath);

            if (!Directory.Exists(pluginsDirectory))
            {
                var message = $"Plugins directory '{pluginsDirectory}' does not exist. Startup continues without plugin assemblies.";
                logger.LogInformation(message);
                pluginHostState.AddDiagnostic(message);
                return;
            }

            var pluginFiles = Directory.GetFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
            if (pluginFiles.Count == 0)
            {
                var message = $"No plugin assemblies found in '{pluginsDirectory}'.";
                logger.LogInformation(message);
                pluginHostState.AddDiagnostic(message);
                return;
            }

            foreach (var pluginFile in pluginFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(pluginFile);
                    var bootstrapTypes = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(IAgentMeshPluginBootstrap).IsAssignableFrom(type)).ToList();
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
                catch (Exception exception)
                {
                    pluginHostState.AddStartupError($"Plugin assembly failed to load: '{Path.GetFileName(pluginFile)}'.");
                    logger.LogError(exception, "Plugin assembly failed to load at startup. Plugin: {PluginPath}", pluginFile);
                }
            }
        }
    }
}