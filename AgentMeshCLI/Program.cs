using AgentMesh.Configuration;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AgentMesh;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        builder.Services.AddOptions<ConversationSummarizationConfiguration>()
            .Bind(builder.Configuration.GetSection(ConversationSummarizationConfiguration.SectionName))
            .Services
            .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<ConversationSummarizationConfiguration>>().Value);
        builder.Services.AddOptions<ApiClientConfiguration>()
            .Bind(builder.Configuration.GetSection(ApiClientConfiguration.SectionName))
            .Services
            .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<ApiClientConfiguration>>().Value);
        builder.Services.AddOptions<CallbackConfiguration>()
            .Bind(builder.Configuration.GetSection(CallbackConfiguration.SectionName))
            .Services
            .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<CallbackConfiguration>>().Value);
        builder.Services.AddHttpClient<AgentMeshApiClient>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<ApiClientConfiguration>();
            client.BaseAddress = new Uri(configuration.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add(configuration.HeaderName, configuration.ApiKey);
        });
        builder.Services.AddSingleton<CallbackUrlFactory>();
        builder.Services.AddSingleton<PendingRequestRegistry>();
        builder.Services.AddSingleton<ConversationState>();
        builder.Services.AddSingleton<ConsoleWorkflowProgressNotifier>();
        builder.Services.AddSingleton<CallbackListenerService>();
        builder.Services.AddHostedService<UserConsoleInputService>();
        builder.Services.AddHostedService<CallbackListenerService>();

        await builder.Build().RunAsync();
    }
}
