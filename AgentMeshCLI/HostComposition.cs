namespace AgentMesh;

public static class HostComposition
{
    public static void ConfigureConfiguration(ConfigurationManager configuration, string environmentName)
    {
        configuration.Sources.Clear();
        configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
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
            var startupLogger = startupLoggerFactory.CreateLogger<PluginHostBootstrapLoader>();
            var pluginLoader = new PluginHostBootstrapLoader(pluginHostConfiguration, pluginHostState, startupLogger);
            pluginLoader.LoadPlugins(services);
        }

        foreach (var ewParameterType in AssemblyDiscoveryHelper.DiscoverEWParameterImplementations())
        {
            services.AddSingleton(ewParameterType);
            services.AddSingleton(typeof(IEWParameterConfiguration), sp => (IEWParameterConfiguration)sp.GetRequiredService(ewParameterType));
        }

        foreach (var ewStepType in AssemblyDiscoveryHelper.DiscoverEWStepImplementations())
        {
            services.AddSingleton(ewStepType);
        }

        services.AddSingleton<IEnumerable<AgentFlatConfigurationRecord>>(AgentConfigurationReadHelper.ReadAgentConfigurations(appSettings, AppContext.BaseDirectory).ToArray());

        services.AddSingleton<IAgentInputSerializer, DefaultAgentInputSerializer>();

        services.AddScoped<IParameterStore, ParameterStore>();
        if (pluginHostConfiguration.EnableBuiltInChatPipeline)
        {
            services.AddScoped<IChatRequestPipeline, ChatRequestPipeline>();
        }
        services.AddScoped<ISummarizationPipeline, SummarizationPipeline>();

        var lightRagConfig = new LightRagServiceConfiguration();
        configuration.GetSection(LightRagServiceConfiguration.SectionName).Bind(lightRagConfig);
        services.AddSingleton(lightRagConfig);
        services.AddHttpClient<IKnowledgeService, LightRagKnowledgeService>();

        var agentMemoryConfig = new AgentMemoryServiceConfiguration();
        configuration.GetSection(AgentMemoryServiceConfiguration.SectionName).Bind(agentMemoryConfig);
        services.AddSingleton(agentMemoryConfig);
        services.AddHttpClient<IAgentMemoryService, Mem0AgentMemoryService>();

        var cohereRerankerConfig = new CohereV1RerankerServiceConfiguration();
        configuration.GetSection(CohereV1RerankerServiceConfiguration.SectionName).Bind(cohereRerankerConfig);
        services.AddSingleton(cohereRerankerConfig);
        services.AddHttpClient<IRerankerService, CohereV1RerankerService>();

        services.AddSingleton<AgentMemoryExecutor>();

        services
            .AddOptions<SESJSSandboxConfiguration>()
            .Bind(configuration.GetSection("SESJSSandbox"))
            .Services
            .AddSingleton(sp => sp.GetRequiredService<IOptions<SESJSSandboxConfiguration>>().Value);

        services
            .AddOptions<ResilienceConfiguration>()
            .Bind(configuration.GetSection(ResilienceConfiguration.SectionName))
            .Services
            .AddSingleton(sp => sp.GetRequiredService<IOptions<ResilienceConfiguration>>().Value);

        services
          .AddOptions<ConversationSummarizationConfiguration>()
          .Bind(configuration.GetSection(ConversationSummarizationConfiguration.SectionName))
          .Services
          .AddSingleton(sp => sp.GetRequiredService<IOptions<ConversationSummarizationConfiguration>>().Value);

        services.AddSingleton<Resilience>();

        foreach (var ewAgentType in AssemblyDiscoveryHelper.DiscoverEWAgentImplementations())
        {
            services.AddSingleton(ewAgentType);
            services.AddSingleton(typeof(IEWAgent), sp => (IEWAgent)sp.GetRequiredService(ewAgentType));
        }

        services
            .AddOptions<CodeModeWorkflowConfiguration>()
            .Bind(configuration.GetSection(CodeModeWorkflowConfiguration.SectionName))
            .Services
            .AddSingleton(sp => sp.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);

        services.AddSingleton<JSSandboxExecutor>();
        services.AddSingleton<IJSSandbox, SESJSSandboxClient>();

        services
            .AddOptions<UserConfiguration>()
            .Bind(configuration.GetSection(UserConfiguration.SectionName))
            .Services
            .AddSingleton(sp => sp.GetRequiredService<IOptions<UserConfiguration>>().Value);

        services.AddSingleton<ConversationContext>();
        services.AddSingleton<AppInstance>();
        services.AddSingleton<StatelessAppInstance>();
        services.AddSingleton<PipelineRegistryInitializer>();
        services.AddHostedService<PipelineRegistryInitializerHostedService>();
    }
}