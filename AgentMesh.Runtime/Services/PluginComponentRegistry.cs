using System.Reflection;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Runtime.Services;

internal static class PluginComponentRegistry
{
    public static void Register(IServiceCollection services, Assembly pluginAssembly)
    {
        var componentTypes = pluginAssembly.DefinedTypes
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .Select(type => type.AsType())
            .ToArray();

        RegisterSinglePipeline<IChatRequestPipeline>(services, componentTypes, ServiceLifetime.Scoped);
        RegisterOptionalPipeline<ISummarizationPipeline>(services, componentTypes, ServiceLifetime.Scoped);
        RegisterImplementations<IEWParameterConfiguration>(services, componentTypes, ServiceLifetime.Singleton, true);
        RegisterImplementations<IEWStep>(services, componentTypes, ServiceLifetime.Singleton, false);
        RegisterImplementations<IEWAgent>(services, componentTypes, ServiceLifetime.Singleton, true);
        RegisterImplementations<IAgentInputSerializer>(services, componentTypes, ServiceLifetime.Singleton, true);
        RegisterImplementations<IEWParameterSerializer>(services, componentTypes, ServiceLifetime.Singleton, true);
        RegisterKeyedParameterSerializers(services, componentTypes);
    }

    private static void RegisterSinglePipeline<TContract>(IServiceCollection services, IEnumerable<Type> componentTypes, ServiceLifetime lifetime)
    {
        var implementations = GetImplementations<TContract>(componentTypes);
        if (implementations.Count != 1)
        {
            throw new InvalidOperationException($"Plugin assembly must contain exactly one {typeof(TContract).Name} implementation; found {implementations.Count}.");
        }

        Register(services, typeof(TContract), implementations[0], lifetime);
    }

    private static void RegisterOptionalPipeline<TContract>(IServiceCollection services, IEnumerable<Type> componentTypes, ServiceLifetime lifetime)
    {
        var implementations = GetImplementations<TContract>(componentTypes);
        if (implementations.Count > 1)
        {
            throw new InvalidOperationException($"Plugin assembly can contain at most one {typeof(TContract).Name} implementation; found {implementations.Count}.");
        }

        if (implementations.Count == 1)
        {
            Register(services, typeof(TContract), implementations[0], lifetime);
        }
    }

    private static void RegisterImplementations<TContract>(IServiceCollection services, IEnumerable<Type> componentTypes, ServiceLifetime lifetime, bool registerContract)
    {
        foreach (var implementation in GetImplementations<TContract>(componentTypes))
        {
            Register(services, implementation, implementation, lifetime);
            if (registerContract)
            {
                Register(services, typeof(TContract), implementation, lifetime);
            }
        }
    }

    private static List<Type> GetImplementations<TContract>(IEnumerable<Type> componentTypes) => componentTypes
        .Where(type => typeof(TContract).IsAssignableFrom(type))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToList();

    private static void RegisterKeyedParameterSerializers(IServiceCollection services, IEnumerable<Type> componentTypes)
    {
        foreach (var serializerType in GetImplementations<IEWParameterSerializer>(componentTypes))
        {
            var serviceKey = serializerType.Name switch
            {
                var name when name.EndsWith("ValuesEWParameterSerializer", StringComparison.Ordinal) => name.Replace("ValuesEWParameterSerializer", "ParametersSerializer", StringComparison.Ordinal),
                var name when name.EndsWith("EWParameterSerializer", StringComparison.Ordinal) => name.Replace("EWParameterSerializer", "ParametersSerializer", StringComparison.Ordinal),
                _ => serializerType.Name
            };

            if (!services.Any(descriptor => descriptor.ServiceType == typeof(IEWParameterSerializer) && Equals(descriptor.ServiceKey, serviceKey)))
            {
                services.AddKeyedSingleton(typeof(IEWParameterSerializer), serviceKey, serializerType);
            }
        }
    }

    private static void Register(IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == serviceType && descriptor.ImplementationType == implementationType))
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        }
    }
}