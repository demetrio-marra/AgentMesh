using AgentMesh.Application.Contracts;
using AgentMesh.Configuration;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AgentMesh
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            AgentMeshRuntime.ConfigureConfiguration(builder.Configuration, builder.Environment.EnvironmentName);
            builder.Services.AddOptions<ConversationSummarizationConfiguration>()
                .Bind(builder.Configuration.GetSection(ConversationSummarizationConfiguration.SectionName))
                .Services
                .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<ConversationSummarizationConfiguration>>().Value)
                .AddSingleton<IConversationSummarizationSettings>(serviceProvider => serviceProvider.GetRequiredService<ConversationSummarizationConfiguration>());
            AgentMeshRuntime.RegisterCommonServices(builder.Services, builder.Configuration);

            builder.Services.AddSingleton<IWorkflowProgressNotifier, ConsoleWorkflowProgressNotifier>();
            builder.Services.AddHostedService<UserConsoleInputService>();

            var host = builder.Build();
            using (var scope = host.Services.CreateScope())
            {
                _ = scope.ServiceProvider.GetRequiredService<ISummarizationPipeline>();
            }

            await host.RunAsync();
        }
    }
}
