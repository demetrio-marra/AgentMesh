using AgentMesh.Application.Contracts;
using AgentMesh.Application.Services;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Configuration;
using AgentMesh.Helpers;
using AgentMesh.Infrastructure.Cohere;
using AgentMesh.Infrastructure.JSSandbox;
using AgentMesh.Infrastructure.LightRag.Configuration;
using AgentMesh.Infrastructure.LightRag.Services;
using AgentMesh.Infrastructure.Mem0;
using AgentMesh.Infrastructure.OpenAIClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentMesh.Application;

public static class ApplicationRuntime
{
    public static void RegisterCommonServices(IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = new AppSettingsConfigurationDto();
        configuration.Bind(appSettings);
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddConsole();
        });
        services.AddSingleton<IOpenAIClientFactory, OpenAIClientFactory>();

        services.AddSingleton<IEnumerable<AgentFlatConfigurationRecord>>(AgentConfigurationReadHelper.ReadAgentConfigurations(appSettings, AppContext.BaseDirectory).ToArray());
        services.AddScoped<IParameterStore, ParameterStore>();

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
        services.AddSingleton<Resilience>();

        services.AddSingleton<JSSandboxExecutor>();
        services.AddSingleton<IJSSandbox, SESJSSandboxClient>();
        services.AddOptions<UserConfiguration>().Bind(configuration.GetSection(UserConfiguration.SectionName)).Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<UserConfiguration>>().Value);
        services.AddSingleton<AppInstance>();
        services.AddHttpClient(nameof(AppInstance));

        services.AddSingleton<IAppInstance, AppInstance>();

    }
}