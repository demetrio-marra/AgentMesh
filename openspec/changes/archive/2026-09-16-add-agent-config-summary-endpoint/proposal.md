## Why

`AgentMeshCLI` prints a startup summary of the sandbox and per-agent LLM/temperature configuration to the console, but `AgentMesh.Api` has no equivalent. Operators running the API-only host have no way to inspect which agents, models, and temperatures are configured without console access.

## What Changes

- Add a new `ConfigurationController` (or equivalent) to `AgentMesh.Api` exposing a `GET` endpoint that returns the same configuration data currently printed by the CLI's startup summary: sandbox URL/name, agent id, and per-agent role/model/temperature.
- Introduce output DTOs mirroring the printed fields only — provider API keys and system prompts (already present in `AgentFlatConfigurationRecord`) are never serialized, since they are secrets/large text not meant for an HTTP response.
- Endpoint requires the existing API key authentication scheme, consistent with `RequestsController`.

## Capabilities

### New Capabilities
- `api-configuration-summary`: A new authenticated read-only endpoint on `AgentMesh.Api` returning structured sandbox and agent configuration summary data equivalent to the CLI startup printout.

### Modified Capabilities
(none — no existing capability's requirements change)

## Impact

- Affected code: `AgentMesh.Api/Controllers` (new controller), `AgentMesh.Api/Models/Api` (new output DTOs).
- No changes to `AgentMesh`, `AgentMesh.Application`, or `AgentMeshCLI` — the underlying configuration objects (`SESJSSandboxConfiguration`, `UserConfiguration`, `IEnumerable<AgentFlatConfigurationRecord>`) are already registered as singletons via `AgentMeshRuntime.RegisterCommonServices`, which the API host already calls.
- No new dependencies or breaking changes.
