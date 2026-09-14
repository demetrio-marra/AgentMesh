using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Services;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Services.Pipelines;
using AgentMesh.Application.Utils;
using AgentMesh.Configuration;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.Cohere;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Infrastructure.LightRag.Configuration;
using AgentMesh.Infrastructure.LightRag.Services;
using AgentMesh.Infrastructure.Mem0;
using AgentMesh.Infrastructure.OpenAIClient;
using AgentMesh.Models;
using AgentMesh.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentMesh;

public static class AgentMeshRuntime
{
    public static void ConfigureConfiguration(ConfigurationManager configuration, string environmentName)
    {
        configuration.Sources.Clear();
        configuration.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();
    }

    public static void RegisterCommonServices(IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = new AppSettingsConfigurationDto();
        configuration.Bind(appSettings);
        var pluginHostConfiguration = new PluginHostConfiguration();
        configuration.GetSection(PluginHostConfiguration.SectionName).Bind(pluginHostConfiguration);
        var pluginHostState = new PluginHostState();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddConsole();
        });
        services.AddSingleton(pluginHostConfiguration);
        services.AddSingleton(pluginHostState);
        services.AddKeyedSingleton<IEWParameterSerializer, DisplayValuesEWParameterSerializer>("DisplayParametersSerializer");
        services.AddKeyedSingleton<IEWParameterSerializer, DefaultEWParameterSerializer>("DefaultParametersSerializer");
        services.AddKeyedSingleton<IEWParameterSerializer, OmittedValueEWParameterSerializer>("OmittedValueParametersSerializer");
        services.AddSingleton<IOpenAIClientFactory, OpenAIClientFactory>();

        using (var startupLoggerFactory = LoggerFactory.Create(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddConsole();
        }))
        {
            new PluginHostBootstrapLoader(pluginHostConfiguration, pluginHostState, startupLoggerFactory.CreateLogger<PluginHostBootstrapLoader>()).LoadPlugins(services);
        }

        foreach (var parameterType in AssemblyDiscoveryHelper.DiscoverEWParameterImplementations())
        {
            services.AddSingleton(parameterType);
            services.AddSingleton(typeof(IEWParameterConfiguration), serviceProvider => (IEWParameterConfiguration)serviceProvider.GetRequiredService(parameterType));
        }
        foreach (var stepType in AssemblyDiscoveryHelper.DiscoverEWStepImplementations())
        {
            services.AddSingleton(stepType);
        }

        services.AddSingleton<IEnumerable<AgentFlatConfigurationRecord>>(AgentConfigurationReadHelper.ReadAgentConfigurations(appSettings, AppContext.BaseDirectory).ToArray());
        services.AddSingleton<IAgentInputSerializer, DefaultAgentInputSerializer>();
        services.AddScoped<IParameterStore, ParameterStore>();
        if (pluginHostConfiguration.EnableBuiltInChatPipeline)
        {
            services.AddScoped<IChatRequestPipeline, ChatRequestPipeline>();
        }
        services.AddScoped<ISummarizationPipeline, SummarizationPipeline>();

        var lightRagConfiguration = new LightRagServiceConfiguration();
        configuration.GetSection(LightRagServiceConfiguration.SectionName).Bind(lightRagConfiguration);
        services.AddSingleton(lightRagConfiguration);
        services.AddHttpClient<IKnowledgeService, LightRagKnowledgeService>();
        var agentMemoryConfiguration = new AgentMemoryServiceConfiguration();
        configuration.GetSection(AgentMemoryServiceConfiguration.SectionName).Bind(agentMemoryConfiguration);
        services.AddSingleton(agentMemoryConfiguration);
        services.AddHttpClient<IAgentMemoryService, Mem0AgentMemoryService>();
        var cohereConfiguration = new CohereV1RerankerServiceConfiguration();
        configuration.GetSection(CohereV1RerankerServiceConfiguration.SectionName).Bind(cohereConfiguration);
        services.AddSingleton(cohereConfiguration);
        services.AddHttpClient<IRerankerService, CohereV1RerankerService>();
        services.AddSingleton<AgentMemoryExecutor>();
        services.AddOptions<SESJSSandboxConfiguration>().Bind(configuration.GetSection("SESJSSandbox")).Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<SESJSSandboxConfiguration>>().Value);
        services.AddOptions<ResilienceConfiguration>().Bind(configuration.GetSection(ResilienceConfiguration.SectionName)).Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<ResilienceConfiguration>>().Value);
        services.AddOptions<ConversationSummarizationConfiguration>().Bind(configuration.GetSection(ConversationSummarizationConfiguration.SectionName)).Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<ConversationSummarizationConfiguration>>().Value);
        services.AddSingleton<Resilience>();

        foreach (var agentType in AssemblyDiscoveryHelper.DiscoverEWAgentImplementations())
        {
            services.AddSingleton(agentType);
            services.AddSingleton(typeof(IEWAgent), serviceProvider => (IEWAgent)serviceProvider.GetRequiredService(agentType));
        }

        services.AddOptions<CodeModeWorkflowConfiguration>().Bind(configuration.GetSection(CodeModeWorkflowConfiguration.SectionName)).Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);
        services.AddSingleton<JSSandboxExecutor>();
        services.AddSingleton<IJSSandbox, SESJSSandboxClient>();
        services.AddOptions<UserConfiguration>().Bind(configuration.GetSection(UserConfiguration.SectionName)).Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<UserConfiguration>>().Value);
        services.AddSingleton<ConversationContext>();
        services.AddSingleton<AppInstance>();
        services.AddSingleton<StatelessAppInstance>();
        services.AddSingleton<PipelineRegistryInitializer>();
        services.AddHostedService<PipelineRegistryInitializerHostedService>();
    }
}