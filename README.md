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

### Project Boundaries

AgentMesh separates reusable framework packages, external-system adapters, and executable hosts:

| Project | Responsibility |
|---|---|
| `AgentMesh` | Basic domain entities and shared pipeline, parameter, step, agent, and executor abstractions. |
| `AgentMesh.Contracts` | Infrastructure service definitions and shared integration contracts. |
| `AgentMesh.Infrastructure.*` | Adapters for external systems such as OpenAI-compatible LLMs, Mem0, LightRAG, Cohere, and the JavaScript sandbox. |
| `AgentMesh.Application` | The reusable agentic-pipeline framework, packaged as `AgentMesh.Framework.Application` for class-library plugins. |
| `AgentMesh.Api` | The executable HTTP host that loads plugins and runs custom agentic pipelines. |
| `AgentMeshCLI` | The REST terminal frontend that calls `AgentMesh.Api`. |

Custom pipeline plugins reference the reusable framework packages. Each `AgentMesh.Api` deployment loads one pipeline plugin and exposes that pipeline over HTTP; multiple pipelines in the same Kubernetes cluster run in separate API workloads and Services. `AgentMeshCLI` remains a client of the selected API Service and does not host pipeline execution.

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
| `AgentMesh` | Basic domain entities and shared pipeline abstractions |
| `AgentMesh.Contracts` | Infrastructure service definitions and shared integration contracts |
| `AgentMesh.Infrastructure.*` | External-system adapters, including OpenAI-compatible LLMs, [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox), Mem0, LightRAG, and Cohere |
| `AgentMesh.Application` | Reusable agentic-pipeline framework for class-library plugins, packaged as `AgentMesh.Framework.Application` |
| `AgentMesh.Api` | Executable HTTP host that loads plugins and runs custom agentic pipelines |
| `AgentMeshCLI` | REST terminal frontend for `AgentMesh.Api` |

> Reusable packages provide the domain, contract, adapter, and pipeline framework layers. `AgentMesh.Api` is the deployable runtime host, and `AgentMeshCLI` is strictly its REST terminal frontend.

## :package: Framework Packaging

The repository separates reusable framework surfaces, infrastructure contracts and adapters, and deployable hosts:

- `AgentMesh` provides the basic domain entities and shared abstractions used by framework and integration layers.
- `AgentMesh.Contracts` defines the infrastructure service contracts used between the framework and external adapters.
- `AgentMesh.Infrastructure.*` provides the integrations for external systems behind those contracts.
- `AgentMesh.Application` provides the reusable agentic-pipeline framework and is packaged as `AgentMesh.Framework.Application` for custom class-library plugins.
- `AgentMesh.Api` is the deployable HTTP host responsible for configuration, plugin loading, API access, and running custom pipelines.
- `AgentMeshCLI` is the REST terminal frontend that calls the API and manages its local terminal conversation experience.

Plugin authors should reference `AgentMesh.Framework.Application` and its transitive framework dependencies, not the `AgentMesh.Api` host executable.

The [`samples/AgentMesh.DefaultPipelinePlugin`](samples/AgentMesh.DefaultPipelinePlugin) directory contains a ready-to-use plugin DLL implementation based on AgentMesh.Framework.Application NuGet package. Developers should start custom pipelines from it.

## :electric_plug: Plugin Hosting

At startup, the API host (`AgentMesh.Api`) loads one pipeline plugin from the configured `Plugins/` directory and invokes its `IAgentMeshPluginBootstrap`. The bootstrap registers the services that the plugin wants to expose, including the deployment's sole `IChatRequestPipeline` implementation. The official API Docker image does not contain a default pipeline plugin.

When deploying the default pipeline plugin, provide its `pipelineSettings.json` file in the API runtime base directory alongside `appsettings.json`. This file contains the pipeline-owned `InferenceProviders`, `LLMs`, `Agents`, and `CodeModeWorkflow` sections. The API loads it optionally after the host settings and before environment variables, so a host without pipeline settings can still start while the plugin configuration remains unavailable.

- Plugin loading happens only at startup.
- The API does not watch the plugin folder. Dropping or replacing DLLs has no effect on running pods.
- After a plugin is added or replaced, a DevOps engineer must run `kubectl rollout restart deployment/<deployment-name>`; only the replacement pod loads the plugin.
- `AgentMesh.Api` is the executable host that loads one plugin, runs its pipeline, and exposes unnamed request endpoints.
- If no plugin is present, the API still starts and returns a generic `503 Service Unavailable` at request time.
- Multiple pipelines are routed at the Kubernetes Service level, not selected by pipeline name inside the API process.

When plugin configuration is invalid, the host stays up and returns RFC7807 responses instead of crashing.

## :satellite: Synchronous vs. Asynchronous Requests

- `POST /api/requests` processes the request synchronously and returns a `requestId` (a generated GUID) alongside the workflow result once the deployment's pipeline finishes.
- `POST /api/requests/async` returns a `requestId` immediately, without waiting for the workflow to finish, and runs the deployment's pipeline in the background.
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

3. **Configure host and pipeline settings**

  Edit `AgentMesh.Api/appsettings.json` (the API host configuration) to set:
   - **Sandbox** ? URL and sandbox name under `SESJSSandbox`
   - **Agent Memory** ? Mem0 service URL under `AgentMemoryService`
   - **LightRag** ? LightRag proxy configuration under `LightRagKnowledgeService`

  Copy `pipelineSettings.json` from the `AgentMesh.DefaultPipelinePlugin` build output to the API runtime base directory and configure:
   - **LLM providers** ? endpoints and API keys under `InferenceProviders`
   - **LLMs** ? model names and providers for each tier under `LLMs`
   - **Agent settings** ? per-agent LLM assignment, temperature, and system prompt files under `Agents`
   - **Workflow settings** ? pipeline feature flags and knowledge-base language under `CodeModeWorkflow`

   If using the CLI frontend, configure `AgentMeshCLI/appsettings.json` with the API base URL and authentication header/key.

4. **Set environment variables** for API keys as required by your LLM providers.

5. **Build and run**

   ```bash
   dotnet build
   ```

  Start the API host (which loads one deployment-specific plugin and runs its pipeline):

   ```bash
   dotnet run --project AgentMesh.Api
   ```

   Optionally, start the CLI (terminal frontend for the API) in a separate terminal:

   ```bash
   dotnet run --project AgentMeshCLI
   ```

  `AgentMesh.Api` is the runtime host for custom pipelines, while `AgentMeshCLI` operates as its REST terminal frontend.

### Packaging

To produce the reusable framework NuGet artifacts:

```bash
dotnet pack AgentMesh/AgentMesh.csproj -c Release
dotnet pack AgentMesh.Application/AgentMesh.Application.csproj -c Release
```

### Container deployment

Build the API image from the repository root. The image contains no default pipeline plugin. Supply the API key and other deployment-specific settings through environment variables or mounted configuration; do not put secrets in the image. Mount exactly one pipeline plugin per API deployment.

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
apiVersion: apps/v1
kind: Deployment
metadata:
  name: agentmesh-pipeline-a
  labels:
    app: agentmesh-api
    pipeline: pipeline-a
spec:
  selector:
    matchLabels:
      app: agentmesh-api
      pipeline: pipeline-a
  template:
    metadata:
      labels:
        app: agentmesh-api
        pipeline: pipeline-a
    spec:
      containers:
        - name: api
          image: agentmesh-api:latest
          volumeMounts:
            - name: plugins
              mountPath: /app/Plugins
              readOnly: true
      volumes:
        - name: plugins
          configMap:
            name: agentmesh-pipeline-a-plugin
---
apiVersion: v1
kind: Service
metadata:
  name: agentmesh-pipeline-a
  labels:
    app: agentmesh-api
    pipeline: pipeline-a
spec:
  selector:
    app: agentmesh-api
    pipeline: pipeline-a
  ports:
    - port: 8080
      targetPort: 8080
```

Deploy one Deployment and Service pair per pipeline. Use the same pipeline identity label on each workload and Service; upstream Kubernetes-aware routing sends messages requiring that pipeline to the matching Service, then calls `/api/requests` or `/api/requests/async`.

When a plugin is dropped into or replaced in the mounted folder, nothing happens automatically. Restart the deployment explicitly:

```bash
kubectl rollout restart deployment/agentmesh-pipeline-a
kubectl rollout status deployment/agentmesh-pipeline-a
```

For automatic pipeline delivery, create a deployment chain that builds and publishes the plugin, updates the mounted artifact or image, runs `kubectl rollout restart deployment/<deployment-name>`, and verifies `kubectl rollout status` before marking the deployment successful.

The plugin volume portion alone is:

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

The system is configured through host `appsettings.json` and the pipeline's `pipelineSettings.json`. Key pipeline sections:

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
