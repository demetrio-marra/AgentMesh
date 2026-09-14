## Context

See [proposal.md](proposal.md) for motivation. `ChatRequestPipeline` and `SummarizationPipeline`, their concrete agents, steps, parameters, prompts, and most supporting types currently live in `AgentMesh.Application`. `AgentMeshRuntime.RegisterCommonServices` registers the pipeline implementations directly and uses discovery for parameters, steps, and agents. Plugin assemblies are loaded before that discovery and can explicitly register services through `IAgentMeshPluginBootstrap`.

`AppInstance` retains interactive conversation state and invokes `ISummarizationPipeline` only through the explicit CLI command. CLI startup requires a loaded plugin to register that pipeline. `StatelessAppInstance`, used by the API, accepts caller-provided conversation per request and does not give summarization any special route or behavior. `ISummarizationPipeline`, `IChatRequestPipeline`, step/parameter abstractions, and the memory, knowledge, reranker, and sandbox contracts belong to the framework boundary.

## Goals / Non-Goals

**Goals:**
- Produce a complete, buildable reference plugin that supplies both existing pipeline implementations through plugin bootstrap registration.
- Make plugin, CLI, and API projects consume AgentMesh framework assemblies only as NuGet packages.
- Make interactive context summarization an explicit user command with a required plugin-provided pipeline, while preserving API caller-owned context semantics.
- Keep all infrastructure adapter implementations host-provided.

**Non-Goals:**
- Change pipeline routing, agent prompts, or branch logic.
- Automate copying plugin binaries into either host's `Plugins` directory.
- Add API endpoints for standalone summarization or make API requests stateful.

## Decisions

### Place the complete implementation graph in a new sample plugin project

Create `samples/AgentMesh.DefaultPipelinePlugin` as a class library. Move both pipeline classes; all of their parameters, EW steps, concrete agents (including `ConversationSummarizerAgent`), prompts, and plugin-owned supporting models/configurations/helpers/utilities/exceptions there. Include this project in the solution as the first full plugin example.

Keep abstract/base types and shared framework runtime types, including `AbstractAgent<T>` and its required contracts/model APIs, in `AgentMesh.Framework.Application` so the sample can compile against package APIs. Retain `AppInstance`, `StatelessAppInstance`, plugin loading, parameter store, resilience, configuration loading, and infrastructure registrations in the application framework package.

Alternative considered: keep the concrete pipeline graph in `AgentMesh.Application` and wrap it with a thin plugin. Rejected because it would leave plugin authors dependent on project implementation details and would not provide a standalone example.

### Use explicit bootstrap registration for the plugin graph

The plugin bootstrap will register both pipeline interfaces plus the plugin's parameters, steps, agents, serializers, configurations, and other plugin-owned services with their appropriate DI lifetimes. It will rely on host registrations for `IAgentMemoryService`, `IKnowledgeService`, `IRerankerService`, `IJSSandbox`, and common framework runtime services.

Alternative considered: retain broad host-side discovery as the primary registration mechanism. Rejected because explicit registration is the documented plugin boundary and makes the sample's requirements clear.

### Establish NuGet-only compilation boundaries

Package `AgentMesh.Framework` and `AgentMesh.Framework.Application` and use package references, with aligned versions and an available package source, from the sample plugin, CLI, and API projects. Remove their direct `ProjectReference` items to framework projects. Move application configuration and prompt content ownership to the sample plugin without adding host artifact-copy targets.

Alternative considered: use project references during local development and packages only for release. Rejected because it would fail to exercise the boundary the sample is intended to demonstrate.

### Make summarization an explicit CLI pipeline invocation

`AppInstance` will no longer invoke `ISummarizationPipeline` as a side effect of processing a normal chat request, including when its context token threshold is exceeded. It will expose an explicit summarization operation over its current in-memory context. `UserConsoleInputService` will map `/summarize` to that operation, display its result/status, and retain `/new` as the operation that resets the conversation. CLI composition validates that a loaded plugin registers `ISummarizationPipeline` and fails startup if it does not. The concrete summarization configuration belongs only to CLI; the plugin consumes a framework configuration contract and does not define the concrete settings class.

`StatelessAppInstance` remains the API execution path: it passes caller-supplied messages into the selected chat pipeline, returns the result, and neither retains nor summarizes context. It does not expose or invoke a dedicated summarization endpoint; API clients manage context and any summarization workflow outside the host. This keeps the API surface limited to normal stateless chat request routes.

Alternative considered: have the API host transparently summarize caller context or preserve CLI auto-summarization. Rejected because either option leaves host mode behavior asymmetric and couples normal chat processing to conversation retention policy.

## Risks / Trade-offs

- [Package restore cannot locate locally produced framework packages] -> Document and configure a compatible package source/version before building dependent projects; validate restore from package references.
- [Plugin dependency DLLs are incomplete when manually deployed] -> Keep deployment manual as requested and document that the plugin assembly and all runtime dependencies must be staged together before host startup.
- [Moving shared types breaks host compilation] -> Classify each moved type by its pipeline ownership; retain common abstractions/runtime and update package public APIs before removing implementations.
- [CLI starts without a summarizer] -> Validate CLI startup with and without the sample plugin staged; require a clear startup failure when no plugin registers `ISummarizationPipeline`.

## Migration Plan

1. Create and package the framework assemblies required by host and plugin consumers, then switch CLI/API and plugin references to those packages.
2. Create the sample plugin and move the complete pipeline implementation graph, prompts, and plugin configuration into it; add its bootstrap registrations.
3. Remove duplicate implementations and registrations from `AgentMesh.Application` while retaining shared runtime and abstract types.
4. Build and test with the sample plugin manually staged in each host's configured `Plugins` directory; verify CLI startup requires the plugin summarizer, `/summarize` invokes it, normal CLI requests do not summarize automatically, and API exposes no dedicated summarization route.
5. Roll back by restoring the direct project references and built-in registrations, then removing the staged sample plugin; no data migration is required.