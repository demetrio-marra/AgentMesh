# AgentMesh

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![OpenAI](https://img.shields.io/badge/OpenAI_Compatible-API-412991?logo=openai&logoColor=white)](https://platform.openai.com/)
[![GitHub stars](https://img.shields.io/github/stars/demetrio-marra/AgentMeshCodeMode)](https://github.com/demetrio-marra/AgentMeshCodeMode/stargazers)

A flexible, extensible multi-agent AI orchestration framework that executes configurable pipelines composed of steps, where each step can invoke specialized AI agents or static executors to transform business parameters. Steps read from and write to a shared parameter store with atomic updates, enabling rich parameter tracking and cost analysis per step.

---

## :sparkles: Features

- **Pipeline-based orchestration** � two distinct pipeline types (`IChatRequestPipeline` and `ISummarizationPipeline`) composed of reusable steps, each with its own role.
- **Parameter-driven architecture** � all meaningful business state is modeled as parameters; parameter classes define metadata/serialization rules, while runtime values are managed in the parameter store.
- **Step-based processing** � steps are the cornerstone of execution; they bridge parameters and either AI agents (for agentic steps) or static executors (for code steps).
- **AI agent integration** � specialized agents process data via LLM, each with configurable model, temperature, and system prompt.
- **Static executors** � complement agents by running deterministic business logic without AI involvement.
- **Sandboxed code execution** � generated code runs in an isolated JavaScript sandbox ([JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox)), deployed separately for security and isolation.
- **Conversation summarization** � dedicated summarization pipeline compresses conversation history to stay within token limits.
- **Agent memory system** � leverages Mem0 for persistent, context-aware agent memory across conversations.
- **Knowledge base integration** � integrates with LightRAG for accessing knowledge bases and documentation ([LightRAG](https://github.com/HKUDS/LightRAG)).
- **Multi-provider LLM support** � configure different LLM providers (HuggingFace, Together, Fireworks AI, or any OpenAI-compatible endpoint) per agent.
- **Per-agent configuration** � each agent has its own LLM model, temperature, and system prompt, all configurable via `appsettings.json`.
- **Token usage tracking** � tracks input/output token consumption per agent and step for cost monitoring and debugging.
- **Parameter change auditing** � the library tracks which step changed which parameter for easier troubleshooting and analysis.

## :building_construction: Core Architecture

### Pipelines

Pipelines define the sequence of steps to be executed. Two pipeline types exist:

- **`IChatRequestPipeline`** � Executes upon each user request. Takes a user message as input and produces a final response string.
- **`ISummarizationPipeline`** � Executes when conversation token count exceeds configured threshold. Compresses chat history into a manageable summary.

Both pipeline types are scoped to their execution context. The only long-lived object is the `ChatContext`, which holds the entire conversation history between user and assistants.

### Parameters (`IEWParameterConfiguration`)

Each entity meaningful to the business operation must be defined as a parameter. Parameters:
- Define a unique name and serialization behavior.
- Act as configuration/metadata (not mutable singleton state shared across steps).
- Support custom serializers for both AI agent use and GUI/display purposes.
- Can be marked as conversation history, current user request, or response parameters.
- Have runtime values stored in a `ParameterStore`, where updates are applied atomically.
- Are tracked with an auditable trail of changes.

### Steps (`IEWStep`)

Steps are the cornerstone of the pipeline. Each step:
- Reads parameter values from the `ParameterStore` and provides them to agents or executors.
- Writes results back through atomic parameter store updates.
- Is either **agentic** (invokes an AI agent) or a **code step** (invokes a static executor).
- Logs its inputs and outputs for tracking and debugging.

### Agents

Agents are LLM-driven services that process data. Each agent:
- Has its own configurable LLM model, provider, temperature, and system prompt.
- Is invoked by agentic steps.
- Returns structured outputs with token count information.

### Executors

Executors are services that run deterministic business logic (static procedures). They are invoked by code steps and do not involve LLMs.

### Overall Flow

```
???????????????????????????????????????????????????????????????
?                       User Input / Event                     ?
???????????????????????????????????????????????????????????????
                     ?
                     v
         ?????????????????????????????
         ?   ChatContext (scoped)    ?
         ?  - ParameterStore         ?
         ?  - Conversation History   ?
         ?????????????????????????????
                  ?
                  v
      ?????????????????????????????????????
      ?  Pipeline (Chat/Summarization)    ?
      ?????????????????????????????????????
                   ?
        ???????????????????????
        ?                     ?
        v                     v
    ??????????          ??????????
    ? Step 1 ?          ? Step N ?
    ??????????          ??????????
         ?                  ?
    ???????????        ???????????
    ?          ?        ?          ?
    v          v        v          v
?????????? ?????????? ?????????? ??????????
? Agent  ? ?Executor? ? Agent  ? ?Executor?
?????????? ?????????? ?????????? ??????????
    ?          ?        ?          ?
    ????????????        ????????????
         ?                   ?
         v                   v
    ???????????????????????????????
    ? ParameterStore Updated      ?
    ? (Atomic + Audited Changes)  ?
    ???????????????????????????????
               ?
               v
          ??????????????
          ?   Output   ?
          ??????????????
```

## :file_folder: Project Structure

| Project | Description |
|---|---|
| `AgentMesh` | Core framework layer: pipeline, parameter, step, agent, and executor contracts and models |
| `AgentMesh.Application` | Application orchestration layer: agent implementations, executors, pipelines, and runtime configuration |
| `AgentMesh.Api` | HTTP integration layer; exposes the runtime over APIs but is not the composition root of the application |
| `AgentMeshCLI` | Terminal frontend for local/dev usage; a thin UI shell around the same application services |
| `AgentMesh.Infrastructure.OpenAIClient` | OpenAI-compatible API client with multi-provider support |
| `AgentMesh.Infrastructure.JSSandbox` | Client for the external [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) service |
| `AgentMesh.Infrastructure.Mem0` | Mem0 agent memory service integration for persistent context |
| `AgentMesh.Infrastructure.LightRag` | LightRag knowledge graph retrieval engine C# client ([LightRAG](https://github.com/HKUDS/LightRAG)) |

> The composition root belongs to the application host layer, not to the API project itself. The API is one integration surface on top of the framework, while the CLI is intentionally a simple terminal frontend.

## :package: Framework Packaging

The repository separates reusable framework surfaces from the host/application wiring:

- `AgentMesh` contains the framework-level contracts and shared models used by plugins and runtime components, such as `IChatRequestPipeline`, `IAgentMeshPluginBootstrap`, `IKnowledgeService`, `IRerankerService`, `IJSSandbox`, the parameter/step abstractions, and immutable shared models.
- `AgentMesh.Application` contains reusable orchestration and application services built on top of the core framework package.
- The composition root is not the API project. The application host is responsible for DI composition, startup wiring, and plugin registration; the API and CLI are just different frontends that consume the same application services.
- `AgentMeshCLI` is a thin terminal frontend, not the system's architectural entry point or composition layer.

Plugin authors should reference the framework package(s), not a host-specific executable project.

## :electric_plug: Plugin Hosting

At startup, the application host scans the configured `Plugins/` directory for assemblies and looks for implementations of `IAgentMeshPluginBootstrap`. Each bootstrap explicitly registers the services that the plugin wants to expose, including one or more named `IChatRequestPipeline` implementations.

- Plugin loading happens only at startup.
- Replacing DLLs while the service is running has no effect until restart or redeploy.
- Pipeline names are matched case-insensitively.
- `PluginHost:EnableBuiltInChatPipeline` controls whether the built-in chat pipeline is registered alongside plugin pipelines.
- The HTTP API is just one consumer of the composed application; it does not own the composition layer.
- The default route `POST /api/requests` is valid only when exactly one pipeline is loaded.
- The named route `POST /api/pipelines/{pipelineName}/requests` selects a pipeline explicitly.

When plugin configuration is invalid, the host stays up and returns RFC7807 responses instead of crashing.

## :satellite: Synchronous vs. Asynchronous Requests

- `POST /api/requests` and `POST /api/pipelines/{pipelineName}/requests` process the request synchronously and return a `requestId` (a generated GUID) alongside the workflow result once the pipeline finishes.
- `POST /api/requests/async` and `POST /api/pipelines/{pipelineName}/requests/async` return a `requestId` immediately, without waiting for the workflow to finish, and run the workflow in the background.
- The async request body accepts 5 optional callback URLs: `workflowStartedCallbackUrl`, `workflowStepStartedCallbackUrl`, `workflowStepCompletedCallbackUrl`, `workflowCompletedCallbackUrl`, `workflowErrorCallbackUrl`. If any one is supplied, all 5 must be supplied, otherwise the request is rejected with `400 Bad Request`.
- When configured, the host performs an HTTP `POST` to the corresponding callback URL for each event, always including the request's `requestId` in the payload. On a successful run, `workflowCompletedCallbackUrl` receives the final workflow result; on failure, `workflowErrorCallbackUrl` receives the error message instead (never both for the same request).
- Callback delivery is best-effort: failures (network errors, non-2xx responses) are logged and do not affect the workflow execution.

## :rocket: Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A deployed [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) instance for sandboxed code execution
- A [Mem0](https://mem0.ai/) instance for agent memory (optional but recommended)
- A [LightRAG](https://github.com/HKUDS/LightRAG) C# client for knowledge base integration
- API keys for your chosen LLM provider(s) (HuggingFace, Together, Fireworks AI, etc.)

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/demetrio-marra/AgentMeshCodeMode.git
   cd AgentMeshCodeMode
   ```

2. **Deploy the JS Code Sandbox** (separate service)

   Follow the instructions at [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) to deploy the sandbox service. Update the `SESJSSandbox` section in `appsettings.json` with your sandbox URL.

3. **Configure `appsettings.json`**

   Edit `AgentMeshCLI/appsettings.json` to set:
   - **LLM providers** ? endpoints and API keys (via environment variables) under `InferenceProviders`
   - **LLMs** ? model names and providers for each tier under `LLMs`
   - **Agent settings** ? per-agent LLM assignment, temperature, and system prompt files under `Agents`
   - **Embedding** ? model endpoint and name under `Embedding`
   - **Sandbox** ? URL and sandbox name under `SESJSSandbox`
   - **Agent Memory** ? Mem0 service URL under `AgentMemoryService`
   - **LightRag** ? LightRag proxy configuration under `LightRagKnowledgeService`

4. **Set environment variables** for API keys as required by your LLM providers.

5. **Build and run**

   ```bash
   dotnet build
   ```

   Choose the frontend you want to run:

   ```bash
   dotnet run --project AgentMeshCLI
   ```

   or, for the HTTP integration layer:

   ```bash
   dotnet run --project AgentMesh.Api
   ```

   The CLI is a terminal frontend; the API is a hosted adapter over the same application composition and services.

### Packaging

To produce the reusable framework NuGet artifacts:

```bash
dotnet pack AgentMesh/AgentMesh.csproj -c Release
dotnet pack AgentMesh.Application/AgentMesh.Application.csproj -c Release
```

### Container deployment

Build the API image from the repository root. Supply the API key and other deployment-specific settings through environment variables or mounted configuration; do not put secrets in the image.

Docker example:

```bash
docker build -f AgentMesh.Api/Dockerfile -t agentmesh-api .
docker run \
  -p 8080:8080 \
  -e ApiAuth__ApiKey=your-api-key \
  agentmesh-api
```

The API listens on container port `8080` and expects the API key in the `X-Api-Key` request header by default.

Kubernetes example:

```yaml
volumeMounts:
  - name: plugins
    mountPath: /app/Plugins
    readOnly: true
volumes:
  - name: plugins
    configMap:
      name: agentmesh-plugins
```

### Configuration Overview

The system is configured entirely through `appsettings.json`. Key sections:

```jsonc
{
  "InferenceProviders": {
    "HuggingFace": { "Endpoint": "https://router.huggingface.co/v1" },
    "Together":    { "Endpoint": "https://api.together.xyz/v1/" },
    "FireworksAI": { "Endpoint": "https://api.fireworks.ai/inference/v1/" }
  },
  "LLMs": {
    "AnalysisLLM":    { "Model": "...", "Provider": "HuggingFace" },
    "CoderLLM":       { "Model": "...", "Provider": "HuggingFace" },
    "CompletionLLM":  { "Model": "...", "Provider": "HuggingFace" }
  },
  "Agents": {
    "Coder": {
      "LLM": "CoderLLM",
      "ModelTemperature": "0.6",
      "SystemPromptFile": "Prompts/Coder.SystemPrompt.txt"
    },
    "CodeFixer": {
      "LLM": "CoderLLM",
      "ModelTemperature": "0.7",
      "SystemPromptFile": "Prompts/CodeFixer.SystemPrompt.txt"
    }
    // ... other agents
  },
  "AgentMemoryService": {
    "BaseUrl": "http://localhost:8000",
    "TimeoutSeconds": 30
  }
}
```

Each agent can use a different LLM tier, allowing cost optimization by assigning cheaper/smaller models to simpler tasks and more capable models to complex reasoning.

## :robot: Concepts

### Parameters

Parameters are business entities that flow through the pipeline as runtime values stored in the `ParameterStore`. Parameter classes define configuration/metadata for how those values behave. Each parameter:
- Has a serialization strategy for LLM consumption (e.g., JSON, plain text).
- Has a display strategy for console/UI output.
- Can be flagged as part of conversation history, current user request, or response payload.
- Is atomically updated by steps through the `ParameterStore`, in a tracked and auditable manner.

### Agents

Agents are LLM-driven services specialized for specific tasks. The framework includes agents for:
- Intent extraction and canonicalization
- Requirements collection and analysis
- Functional and technical feasibility analysis
- Code generation
- Conversation summarization
- And many more domain-specific roles

### Executors

Executors are non-LLM services that run deterministic business logic:
- JavaScript code sandbox execution
- Knowledge base queries
- Memory persistence
- And other platform services

## :link: External Dependencies

| Service | Purpose | Reference |
|---|---|---|
| **JSCodeSandbox** | Sandboxed JavaScript execution environment | [github.com/demetrio-marra/JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) |
| **Mem0** | Agent memory and context persistence service | [mem0.ai](https://mem0.ai/) |
| **LightRag** | Knowledge graph retrieval engine| [github.com/HKUDS/LightRAG](https://github.com/HKUDS/LightRAG) |

## :gear: Tech Stack

- **.NET 8** / **C# 12**
- **Microsoft.Extensions** (DI, Configuration, Logging, Options, HttpClient)
- **OpenAI SDK** (OpenAI-compatible API client)
- **Polly** for resilience and retry policies
- **Mem0 SDK** for agent memory
- **LightRag** for knowledge base integration

## :page_facing_up: License

See [LICENSE](LICENSE) for details.
