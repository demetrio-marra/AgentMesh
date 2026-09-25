using AgentMesh.DefaultPipelinePlugin;
using AgentMesh.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services
    .AddOptions<CodeModeWorkflowConfiguration>()
    .Bind(builder.Configuration.GetSection(CodeModeWorkflowConfiguration.SectionName))
    .Services
    .AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<CodeModeWorkflowConfiguration>>().Value);

AgentMeshRuntime.LoadAgentMesh<DefaultPipelinePlugin>(builder);

var app = builder.Build();
await AgentMeshRuntime.StartAgentMesh(app);