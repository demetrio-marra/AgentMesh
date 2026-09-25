# AgentMesh

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) [![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/aspnet/core/) [![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/) [![Kubernetes](https://img.shields.io/badge/Kubernetes-ready-326CE5?logo=kubernetes&logoColor=white)](https://kubernetes.io/) [![NuGet stable version](https://badgen.net/nuget/v/agentmesh.runtime)](https://nuget.org/packages/agentmesh.runtime)

[![OpenAI-compatible](https://img.shields.io/badge/OpenAI-compatible-412991?logo=openai&logoColor=white)](https://platform.openai.com/docs/api-reference/chat) [![Mem0](https://img.shields.io/badge/Mem0-memory-0b7285)](https://mem0.ai/) [![LightRAG](https://img.shields.io/github/stars/hkuds/lightrag?style=flat&logo=github&label=LightRAG)](https://github.com/hkuds/lightrag) [![Cohere](https://img.shields.io/badge/Cohere-reranking-39594d)](https://cohere.com/) [![JSCodeSandbox](https://img.shields.io/github/stars/demetrio-marra/JSCodeSandbox?style=flat&logo=github&label=JSCodeSandbox)](https://github.com/demetrio-marra/JSCodeSandbox)

AgentMesh is a .NET 8 framework for building AI-powered request pipelines from parameters, steps, agents, and infrastructure adapters. Each deployed pipeline is an ASP.NET Core plugin web service.

## Why AgentMesh

AgentMesh puts the **pipeline**, not an individual agent or code step, at the center of an application. Steps exchange data through a parameter store, so the workflow remains explicit, observable, and adaptable as the pipeline grows.

- **Pipeline-centric orchestration**: Steps declare their input and output parameters, while the pipeline controls execution and the parameter store mediates data exchange between steps.
- **Automatic change tracking**: When a step changes a parameter, AgentMesh records the before and after values and reports the mutation as part of the pipeline run.
- **Mostly declarative steps**: Many steps can be described by their parameter dependencies and execution behavior without bespoke wiring for every data transfer.
- **Flexible serialization**: Agent inputs can use a custom serializer instead of assuming JSON, and parameter display serializers can truncate, summarize, or omit large values from progress and diagnostic output.
- **Token and cost accounting**: Agent execution statistics include input and output tokens. Configure per-million-token prices for token-based costs or an optional hourly rate for time-based pricing, and AgentMesh calculates execution totals.
- **Lean REST API**: Expose a plugin pipeline through authenticated `POST /api/requests` and `POST /api/requests/async` endpoints, with optional workflow callbacks. `POST /api/summarize` and `POST /api/summarize/async` can condense conversation context when it becomes too large.
- **Service connectors for custom pipelines**: Developers can use connectors for [OpenAI ChatCompletions-compatible endpoints](https://platform.openai.com/docs/api-reference/chat), [Mem0](https://mem0.ai/) , [LightRAG](https://github.com/hkuds/lightrag) , reranker-compatible endpoints such as [Cohere](https://cohere.com/) , and [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) for JavaScript code execution .
- **Container and Kubernetes ready**: Plugin applications own their Dockerfiles and deployment manifests, while AgentMesh Runtime provides the plugin-facing host model and deployment guidance for running one pipeline service per deployment.

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

[AMCodePipeline](https://github.com/demetrio-marra/AMCodePipeline) is the externally maintained executable reference plugin. It demonstrates the default chat and summarization pipelines, their parameters, steps, agents, serializers, prompts, active configuration, launch profile, and Dockerfile.

See the [AMCodePipeline README](https://github.com/demetrio-marra/AMCodePipeline#run-locally) for current local development and startup instructions.

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

Build and run the reference plugin by following the [AMCodePipeline Docker instructions](https://github.com/demetrio-marra/AMCodePipeline#run-with-docker).

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