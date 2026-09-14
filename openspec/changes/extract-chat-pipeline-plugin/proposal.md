## Why

The built-in chat pipeline is still compiled into `AgentMesh.Application`, so the plugin architecture has only minimal echo-style examples and does not demonstrate how a complete production pipeline can be delivered independently. Extracting it into a working sample plugin validates the package boundary and provides plugin authors with a realistic reference implementation.

## What Changes

- Add a new class-library sample plugin containing the current `ChatRequestPipeline` and `SummarizationPipeline`, their parameters, EW steps, concrete agents, prompts, and plugin-owned models, helpers, utilities, configurations, and exceptions.
- Make the sample plugin reference AgentMesh framework packages through NuGet packages only; it must not directly reference `AgentMesh.Application`.
- Register the sample pipeline and all of its plugin-owned dependencies through an `IAgentMeshPluginBootstrap` implementation while continuing to consume framework-provided memory, knowledge, reranker, and sandbox services through their framework contracts.
- Remove the extracted chat and summarization pipeline implementations and their registrations from `AgentMesh.Application`; preserve framework runtime services there.
- Replace automatic interactive-context summarization with an explicit CLI `/summarize` command that invokes the plugin-provided summarization pipeline for the current in-memory conversation.
- **BREAKING** Change CLI and API project references from direct framework project references to NuGet package references only, including their application configuration and prompt content handling.
- Do not add build, copy, or packaging configuration that deploys plugin artifacts into either host's `bin/Plugins` directory; deployment remains a manual host operation.

## Capabilities

### New Capabilities

- `default-chat-pipeline-plugin`: A complete, independently packaged sample plugin that provides the existing default chat request and conversation-summarization pipelines.

### Modified Capabilities

- `plugin-pipeline-hosting`: Define that the built-in default pipeline can be supplied as a separately packaged plugin and that hosts consume framework dependencies through NuGet packages rather than direct project references.

## Impact

- Affected projects: `AgentMesh.Application`, `AgentMeshCLI`, `AgentMesh.Api`, solution/project package configuration, and a new sample plugin project.
- The new plugin will consume `AgentMesh.Framework` and `AgentMesh.Framework.Application` packages plus required package dependencies; framework adapters for memory, knowledge, reranking, and sandbox remain host-provided.
- Host deployments must manually place the built plugin and its dependencies in the configured `Plugins` directory before startup.

## Non-goals

- Do not change chat-pipeline decision logic or user-facing request behavior.
- Do not automate plugin deployment, hot reload, or copying artifacts into host plugin folders.
- Do not change API mode's caller-owned conversation context or replace framework-provided infrastructure adapters.