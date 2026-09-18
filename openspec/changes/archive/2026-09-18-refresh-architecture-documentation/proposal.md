## Why

The README and OpenSpec capability specifications describe overlapping architecture concerns with inconsistent or incomplete project ownership. Contributors need one current model of the package boundaries so they can choose the correct extension point, deployment component, and dependency direction without inferring them from project references.

## What Changes

- Refresh the README architecture, project structure, packaging, plugin-hosting, request-flow, and setup guidance to describe the six project roles consistently.
- Expand the architecture documentation specification with explicit responsibilities, dependency direction, runtime ownership, and extension/deployment boundaries for `AgentMesh`, `AgentMesh.Contracts`, `AgentMesh.Infrastructure.*`, `AgentMesh.Application`, `AgentMesh.Api`, and `AgentMeshCLI`.
- Align every existing capability specification with the same layer names and ownership terminology where its requirements describe API, CLI, pipeline, plugin, configuration, callback, or runtime behavior.
- Record the framework packaging model: `AgentMesh.Application` is the plugin-facing NuGet package and `AgentMesh.Api` is the executable host for custom pipelines.

## Non-goals

- Move source files, namespaces, contracts, or project references.
- Change endpoint behavior, authentication, pipeline execution, plugin loading, or CLI workflows.
- Introduce a new runtime host, plugin model, or infrastructure adapter.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `architecture-documentation`: Define the complete cross-project architecture and responsibility boundaries.
- `api-cli-separation`: Use consistent API-host and REST-terminal ownership terminology.
- `api-configuration-summary`: Clarify API-owned configuration and CLI consumption responsibilities.
- `api-summarization-pipeline`: Clarify host delegation and CLI frontend responsibilities.
- `async-request-callbacks`: Clarify API callback ownership and CLI callback-consumption role.
- `cli-api-frontend`: Define `AgentMeshCLI` as a REST terminal frontend for `AgentMesh.Api`.
- `default-chat-pipeline-plugin`: Clarify the plugin framework package and host relationship.
- `plugin-pipeline-hosting`: Define framework, host, contracts, and adapter roles for plugin loading.
- `request-access-modes`: Clarify host ownership of REST access modes and application pipeline execution.
- `stateless-pipeline-runner`: Clarify `AgentMesh.Application` ownership of reusable stateless pipeline execution.

## Impact

- Affected documentation: `README.md` and all current OpenSpec specifications.
- Affected project descriptions: `AgentMesh`, `AgentMesh.Contracts`, `AgentMesh.Infrastructure.*`, `AgentMesh.Application`, `AgentMesh.Api`, and `AgentMeshCLI`.
- No runtime code, API contract, dependency, or package-version changes are intended.