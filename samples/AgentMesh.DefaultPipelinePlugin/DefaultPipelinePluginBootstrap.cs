using AgentMesh.Application.Configuration;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentMesh.DefaultPipelinePlugin;

public sealed class DefaultPipelinePluginBootstrap : IAgentMeshPluginBootstrap
{
    public void RegisterServices(IServiceCollection services)
    {
        var pluginAssembly = typeof(DefaultPipelinePluginBootstrap).Assembly;

        services.AddKeyedSingleton<IEWParameterSerializer, DisplayValuesEWParameterSerializer>("DisplayParametersSerializer");
        services.AddKeyedSingleton<IEWParameterSerializer, DefaultEWParameterSerializer>("DefaultParametersSerializer");
        services.AddKeyedSingleton<IEWParameterSerializer, OmittedValueEWParameterSerializer>("OmittedValueParametersSerializer");
        services.AddSingleton<IAgentInputSerializer, DefaultAgentInputSerializer>();
        services.AddOptions<CodeModeWorkflowConfiguration>()
            .BindConfiguration(CodeModeWorkflowConfiguration.SectionName)
            .Services
            .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);
        foreach (var parameterType in pluginAssembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(IEWParameterConfiguration).IsAssignableFrom(type)))
        {
            services.AddSingleton(parameterType);
            services.AddSingleton(typeof(IEWParameterConfiguration), serviceProvider => (IEWParameterConfiguration)serviceProvider.GetRequiredService(parameterType));
        }

        foreach (var stepType in pluginAssembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(IEWStep).IsAssignableFrom(type)))
        {
            services.AddSingleton(stepType);
        }

        foreach (var agentType in pluginAssembly.GetTypes().Where(type => type.IsClass && !type.IsAbstract && typeof(IEWAgent).IsAssignableFrom(type)))
        {
            services.AddSingleton(agentType);
            services.AddSingleton(typeof(IEWAgent), serviceProvider => (IEWAgent)serviceProvider.GetRequiredService(agentType));
        }

        services.AddScoped<IChatRequestPipeline, ChatRequestPipeline>();
        services.AddScoped<ISummarizationPipeline, SummarizationPipeline>();
    }
}