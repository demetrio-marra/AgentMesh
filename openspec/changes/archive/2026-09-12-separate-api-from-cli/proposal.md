## Why

`AgentMeshCLI` currently hosts two different application modes selected at runtime by `--interactive`: a stateful console workflow and an ASP.NET Core API. This couples console startup to web concerns, makes the executable's default behavior mode-dependent, and complicates local launch selection. Separating the API into `AgentMesh.Api` gives each executable one clear responsibility while preserving the shared AgentMesh composition and pipeline behavior.

## What Changes

- Add a new `AgentMesh.Api` web project containing the HTTP controllers, API-key authentication, Swagger setup, and stateless API startup.
- Keep `AgentMeshCLI` as a console-only project that starts the interactive workflow directly and no longer branches on or requires `--interactive`.
- Move API-specific configuration and source files out of `AgentMeshCLI`, while retaining shared service and plugin composition where both hosts need it.
- Update the solution, project references, configuration copying, and launch profiles so `Interactive` launches the CLI and `Web` launches `AgentMesh.Api`.
- Preserve existing API routes, authentication behavior, request/response contracts, and stateless execution semantics.

## Capabilities

### New Capabilities

- `api-cli-separation`: Run the console and HTTP entry points as separate projects with explicit launch-profile selection.

### Modified Capabilities

- None.

## Impact

Affected areas include `AgentMeshCLI/Program.cs` and its project/configuration files, a new `AgentMesh.Api` project and API source tree, `AgentMesh.sln`, and launch/debug configuration. The split changes process/project boundaries but should not change the public API contract or pipeline execution behavior.

## Non-goals

- Redesigning API endpoints, authentication semantics, or payload models.
- Changing pipeline, plugin, agent, or infrastructure behavior.
- Adding runtime mode detection, a replacement command-line switch, or a shared web/console executable.