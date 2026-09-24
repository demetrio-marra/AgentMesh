using System.Reflection;
using System.Runtime.Loader;

namespace AgentMesh.Api.Services;

internal static class PluginBootstrapLoader
{
    public static PluginLoadResult LoadPlugin(IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var configuredPath = configuration.GetSection("PluginHost").GetValue<string>("PluginsPath") ?? "Plugins";
        var pluginsDirectory = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

        if (!Directory.Exists(pluginsDirectory))
        {
            logger.LogInformation("Plugins directory '{PluginsDirectory}' does not exist. Startup continues without plugin assemblies.", pluginsDirectory);
            return PluginLoadResult.Missing;
        }

        var pluginFiles = Directory.GetFiles(pluginsDirectory, "*Plugin.dll", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (pluginFiles.Count == 0)
        {
            logger.LogInformation("No plugin assemblies were found in '{PluginsDirectory}'. Startup continues without a pipeline.", pluginsDirectory);
            return PluginLoadResult.Missing;
        }

        if (pluginFiles.Count > 1)
        {
            logger.LogError("Multiple plugin assemblies were found in '{PluginsDirectory}'. Deploy exactly one pipeline plugin.", pluginsDirectory);
            return PluginLoadResult.Invalid;
        }

        var pluginFile = pluginFiles[0];
        try
        {
            var loadContext = new PluginLoadContext(pluginFile);
            var assembly = loadContext.LoadFromAssemblyPath(pluginFile);
            var bootstrapTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && typeof(IAgentMeshPluginBootstrap).IsAssignableFrom(type))
                .ToList();

            if (bootstrapTypes.Count != 1)
            {
                logger.LogError("Plugin assembly '{PluginPath}' must expose exactly one bootstrap type; found {BootstrapCount}.", pluginFile, bootstrapTypes.Count);
                return PluginLoadResult.Invalid;
            }

            if (Activator.CreateInstance(bootstrapTypes[0]) is not IAgentMeshPluginBootstrap bootstrap)
            {
                logger.LogError("Plugin bootstrap could not be activated from '{PluginPath}'.", pluginFile);
                return PluginLoadResult.Invalid;
            }

            bootstrap.RegisterServices(services, configuration);
            logger.LogInformation("Plugin bootstrap invoked. Plugin: {PluginPath}; Bootstrap: {BootstrapType}", pluginFile, bootstrapTypes[0].FullName);
            return PluginLoadResult.Loaded;
        }
        catch (ReflectionTypeLoadException exception)
        {
            var details = string.Join(
                Environment.NewLine,
                exception.LoaderExceptions.Select(error => error?.ToString() ?? "Unknown loader exception."));

            logger.LogError(exception, "Plugin types could not be loaded from '{PluginPath}'. {Details}", pluginFile, details);
            return PluginLoadResult.Invalid;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Plugin assembly could not be loaded or inspected: '{PluginPath}'.", pluginFile);
            return PluginLoadResult.Invalid;
        }
    }

    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Defer to the host's own copy for ANY assembly already loaded
            // in the Default context � this keeps type identity consistent
            // for the contract assembly, DI/logging/config abstractions,
            // and anything else shared between host and plugin, without
            // needing to special-case them individually.
            var existing = AssemblyLoadContext.Default.Assemblies
                .FirstOrDefault(a => string.Equals(
                    a.GetName().Name,
                    assemblyName.Name,
                    StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                return existing;
            }

            string? path = _resolver.ResolveAssemblyToPath(assemblyName);
            return path != null ? LoadFromAssemblyPath(path) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return path != null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
        }
    }
}

internal enum PluginLoadResult
{
    Loaded,
    Missing,
    Invalid
}
