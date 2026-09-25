# AgentMesh

AgentMesh is a .NET 8 framework for building AI-powered request pipelines from parameters, steps, agents, and infrastructure adapters. Each deployed pipeline is an ASP.NET Core plugin web service.

## Architecture

| Project | Responsibility |
| --- | --- |
| `AgentMesh` | Domain entities and shared pipeline, parameter, step, agent, and executor abstractions. |
| `AgentMesh.Contracts` | Infrastructure service definitions and integration contracts. |
| `AgentMesh.Infrastructure.*` | Adapters for OpenAI-compatible LLMs, Mem0, LightRAG, Cohere, and the JavaScript sandbox. |
| `AgentMesh.Application` | Internal application framework that implements pipeline execution, steps, agents, and workflow behavior. |
| `AgentMesh.Runtime` | Plugin-facing NuGet package that provides HTTP APIs, authentication, callbacks, Swagger, framework registration, and the bundled Application assembly. |
| Pipeline plugin | Executable ASP.NET Core service that owns one custom chat pipeline, configuration, dependency registration, host construction, and deployment. |
| `AgentMeshCLI` | REST terminal client for a selected plugin service. |

Plugins reference `AgentMesh.Runtime`; they do not reference framework projects directly. Runtime discovers framework extension types from the plugin assembly identified by an empty `IAgentMeshPlugin` marker. It registers concrete, non-generic implementations of `IChatRequestPipeline`, `ISummarizationPipeline`, `IEWParameterConfiguration`, `IEWStep`, `IEWAgent`, `IAgentInputSerializer`, and `IEWParameterSerializer`.

## Plugin Startup

A plugin is the composition root. Its `Program.cs` must load configuration and register plugin-specific dependencies before Runtime registration:

```csharp
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

AgentMeshRuntime.LoadAgentMesh<MyPlugin>(builder);

var app = builder.Build();
await AgentMeshRuntime.StartAgentMesh(app);
```

`LoadAgentMesh` registers framework and HTTP services but does not build a service provider. The plugin owns the only `builder.Build()` call. `StartAgentMesh` configures authentication, authorization, controllers, Swagger, and asynchronous host execution.

Framework-recognized components need no manual service registration. Register only plugin-specific configuration and services before calling `LoadAgentMesh`.

## Configuration

Each plugin owns an active required `appsettings.json` and an optional `appsettings.{Environment}.json` overlay. Environment variables load last and are appropriate for secrets.

The Runtime NuGet package includes `appsettings.json.template` as inactive starter content. Copy it into the plugin project as `appsettings.json`, tailor provider, LLM, agent, infrastructure, API authentication, and workflow sections, then add an environment-specific file for development or deployment overrides. The template is never activated automatically during restore or execution.

System prompt paths are resolved from the plugin output directory. Place prompt files in the plugin project and mark them for copy to output.

## Default Plugin

[`samples/AgentMesh.DefaultPipelinePlugin`](samples/AgentMesh.DefaultPipelinePlugin) is an executable sample plugin. It contains the default chat and summarization pipelines, their parameters, steps, agents, serializers, prompts, active configuration, launch profile, and Dockerfile.

Run it in Development:

```bash
dotnet run --project samples/AgentMesh.DefaultPipelinePlugin
```

Swagger is available at `/swagger`. The API key is read from the plugin configuration, and the existing request, summarization, callback, and authentication HTTP contracts remain unchanged.

## Packaging

Pack the plugin-facing Runtime package and its core dependency:

```bash
dotnet pack AgentMesh/AgentMesh.csproj -c Release
dotnet pack AgentMesh.Runtime/AgentMesh.Runtime.csproj -c Release
```

The Runtime package exposes `AgentMesh.Runtime.dll` and bundles `AgentMesh.Application.dll` plus required framework assemblies under its target-framework assets. Plugin consumers restore Runtime as a NuGet package.

## Docker

Each plugin owns its Dockerfile and produces its own image. Runtime includes an inactive `Dockerfile.template` that demonstrates building a consuming plugin project; copy and adapt it into the plugin project.

Build the default plugin image from the repository root:

```bash
docker build -f samples/AgentMesh.DefaultPipelinePlugin/Dockerfile -t agentmesh-default-pipeline-plugin .
docker run -p 8080:8080 -e ApiAuth__ApiKey=your-api-key agentmesh-default-pipeline-plugin
```

The image entry point is the plugin assembly, not Runtime. Keep deployment-specific secrets outside the image.

## Kubernetes Deployment

Deploy one plugin web workload and Service per chat pipeline. Give each pair a pipeline-specific label and route requests that require that pipeline to its Service. Pipeline identity is the deployment endpoint, not a request field.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: agentmesh-pipeline-a
spec:
  selector:
    matchLabels:
      app: agentmesh-pipeline-a
  template:
    metadata:
      labels:
        app: agentmesh-pipeline-a
    spec:
      containers:
        - name: pipeline
          image: registry.example/agentmesh-pipeline-a:latest
---
apiVersion: v1
kind: Service
metadata:
  name: agentmesh-pipeline-a
spec:
  selector:
    app: agentmesh-pipeline-a
  ports:
    - port: 8080
      targetPort: 8080
```

Runtime does not load DLLs dynamically. Activate a plugin change by building and publishing that plugin executable or image, applying the updated deployment, and verifying rollout completion:

```bash
kubectl rollout status deployment/agentmesh-pipeline-a
```

## Request Flow

1. A client or `AgentMeshCLI` sends an authenticated request to a plugin service.
2. Runtime controllers resolve the plugin's single chat pipeline.
3. `AppInstance` creates a scoped run and initializes workflow parameters.
4. `ChatRequestPipeline` selects step branches; `EWPipeline` commits mutations through `ParameterStore`.
5. The final answer is read from `FinalAnswerParameter` and returned through the existing HTTP contract.
6. A plugin may also expose a summarization pipeline for the standard summarization endpoint.

## External Boundaries

The framework integrates through these contracts:

- `IOpenAIClient` via `OpenAIClientFactory` and `OpenAIClient`
- `IKnowledgeService` via `LightRagKnowledgeService`
- `IAgentMemoryService` via `Mem0AgentMemoryService`
- `IRerankerService` via `CohereV1RerankerService`
- `IJSSandbox` via `SESJSSandboxClient`

## License

See [LICENSE](LICENSE).