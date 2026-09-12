using AgentMesh.Application.Contracts;

namespace AgentMesh
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            HostComposition.ConfigureConfiguration(builder.Configuration, builder.Environment.EnvironmentName);
            HostComposition.RegisterCommonServices(builder.Services, builder.Configuration);

            builder.Services.AddSingleton<IWorkflowProgressNotifier, ConsoleWorkflowProgressNotifier>();
            builder.Services.AddHostedService<UserConsoleInputService>();

            await builder.Build().RunAsync();
        }
    }
}
