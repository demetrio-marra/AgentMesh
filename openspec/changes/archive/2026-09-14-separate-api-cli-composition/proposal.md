## Why

`AgentMesh.Api` currently references `AgentMeshCLI` to reuse host composition and runtime assets. This reverses the intended dependency direction between two independent executable hosts, forcing the API to build against the CLI and making host ownership unclear.

## What Changes

- Move reusable configuration loading and common dependency-registration logic from `AgentMeshCLI` into `AgentMesh.Application`.
- Update both executable hosts to consume the application-layer composition surface without referencing one another.
- Move or otherwise provide the shared runtime configuration and prompt assets from an application-owned location so each host copies the same assets into its own output.
- Remove `AgentMesh.Api` project references and linked content items that target `AgentMeshCLI`.
- **BREAKING**: Applications or extensions that directly call the CLI-owned `HostComposition` type must use its application-layer replacement.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `api-cli-separation`: Require the API and CLI executable projects to build and run independently, sharing runtime composition solely through non-executable layers.

## Impact

Affected projects include `AgentMesh.Application`, `AgentMeshCLI`, and `AgentMesh.Api`; the solution project graph; host configuration and prompt content locations; and development/runtime build validation. API routes, authentication behavior, pipeline contracts, and console workflow behavior remain unchanged.

## Non-goals

- Changing API endpoint, authentication, or workflow execution contracts.
- Redesigning pipeline registration, plugin discovery, or infrastructure adapters.
- Consolidating the API and CLI into a single executable.