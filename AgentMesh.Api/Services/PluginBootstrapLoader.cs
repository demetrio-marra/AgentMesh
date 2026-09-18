using System.Reflection;
using System.Runtime.Loader;

namespace AgentMesh.Api.Services;

internal static class PluginBootstrapLoader
{
    public static void LoadPlugins(IServiceCollection services, IConfiguration configuration, ILogger logger)
    {
        var configuredPath = configuration.GetSection("PluginHost").GetValue<string>("PluginsPath") ?? "Plugins";
        var pluginsDirectory = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

        if (!Directory.Exists(pluginsDirectory))
        {
            logger.LogInformation("Plugins directory '{PluginsDirectory}' does not exist. Startup continues without plugin assemblies.", pluginsDirectory);
            return;
        }

        var pluginFiles = Directory.GetFiles(pluginsDirectory, "*Plugin.dll", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var pluginFile in pluginFiles)
        {
            List<Type> bootstrapTypes;
            try
            {
                var loadContext = new PluginLoadContext(pluginFile);
                var assembly = loadContext.LoadFromAssemblyPath(pluginFile);
                var founds = assembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(IAgentMeshPluginBootstrap).IsAssignableFrom(type))
                    .ToList();
                bootstrapTypes = founds;
            }
            catch (ReflectionTypeLoadException exception)
            {
                var details = string.Join(
                    Environment.NewLine,
                    exception.LoaderExceptions.Select(error => error?.ToString() ?? "Unknown loader exception."));

                throw new InvalidOperationException(
                    $"Plugin types could not be loaded: '{pluginFile}'.{Environment.NewLine}{details}",
                    exception);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Plugin assembly could not be loaded or inspected: '{pluginFile}'.", exception);
            }

            foreach (var bootstrapType in bootstrapTypes)
            {
                if (Activator.CreateInstance(bootstrapType) is not IAgentMeshPluginBootstrap bootstrap)
                {
                    throw new InvalidOperationException($"Plugin bootstrap could not be activated: '{bootstrapType.FullName}'.");
                }

                bootstrap.RegisterServices(services, configuration);
                logger.LogInformation("Plugin bootstrap invoked. Plugin: {PluginPath}; Bootstrap: {BootstrapType}", pluginFile, bootstrapType.FullName);
            }
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
            // in the Default context — this keeps type identity consistent
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
