## Context

`AgentMesh.Api` already calls `AgentMeshRuntime.RegisterCommonServices`, which registers `SESJSSandboxConfiguration`, `UserConfiguration`, and `IEnumerable<AgentFlatConfigurationRecord>` as singletons — the same objects `AgentMeshCLI`'s `UserConsoleInputService.PrintConfigurations()` reads. No new configuration plumbing is required; this is purely an API surface addition. See proposal.md - Why.

## Goals / Non-Goals

**Goals:**
- Expose a read-only, authenticated `GET` endpoint returning sandbox and per-agent configuration data equivalent to the CLI startup printout.
- Keep the response DTOs free of secrets (`ProviderApiKey`, `SystemPrompt`) and free of internal-only fields (`ProviderEndpoint`, `LLMClass`, cost-per-token figures) that the CLI printout doesn't show either.

**Non-Goals:**
- No changes to how configuration is loaded, bound, or registered.
- No changes to the CLI's console output.
- No write/update capability for configuration — read-only endpoint only.

## Decisions

- **New controller `ConfigurationController` in `AgentMesh.Api/Controllers`**, following the existing `RequestsController` pattern: `[ApiController]`, `[Route("api")]`, `[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]`. Alternative considered: adding an action to `RequestsController` — rejected because configuration inspection is a distinct concern from request execution/pipelines.
- **Route**: `GET api/configuration`. Returns a single JSON object (no route parameters needed since there is one summary per running instance).
- **Output DTOs** in `AgentMesh.Api/Models/Api`, mirroring `AgentMesh.Api`'s existing DTO placement convention:
  - `ConfigurationSummaryApiOutput`: `SandboxServiceUrl`, `SandboxName`, `AgentId`, `Agents` (list of `AgentConfigurationSummaryApiOutput`).
  - `AgentConfigurationSummaryApiOutput`: `AgentRole` (from `AgentUniqueRole`), `Model` (from `ProviderModelName`), `Temperature` (parsed to `double`, mirroring `ConsoleHelper.PrintAgentConfiguration`'s conversion).
- **Field selection mirrors the console printout exactly** — only fields already surfaced via `Console.WriteLine`/`ConsoleHelper.PrintAgentConfiguration` are included. `ProviderApiKey` and `SystemPrompt` are deliberately never mapped onto the output DTO, so there is no risk of accidental serialization regardless of future `AgentFlatConfigurationRecord` changes (explicit allow-list mapping, not exclusion via attribute).
- **DI**: constructor-inject `SESJSSandboxConfiguration`, `UserConfiguration`, and `IEnumerable<AgentFlatConfigurationRecord>` directly into the controller — all three are already registered as singletons for the API host, no new registration needed.

## Risks / Trade-offs

- [Risk: a future field added to `AgentFlatConfigurationRecord` is sensitive but gets mapped by a careless future edit] → Mitigation: explicit allow-list mapping in the DTO constructor/mapper (never `record with` copy-all or reflection-based mapping), and this design doc calls out the excluded fields by name for reviewers.
- [Risk: exposing agent role/model names could aid an attacker profiling the system] → Mitigation: endpoint requires the same API key authentication as all other protected endpoints; no anonymous access.
