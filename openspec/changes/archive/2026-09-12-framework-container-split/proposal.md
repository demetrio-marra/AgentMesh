## Why

AgentMesh currently mixes two concerns in one deployable: a reusable orchestration framework and a concrete host application. This limits adoption by third-party developers who need to build custom pipelines/steps as reusable packages, and it makes container operations harder when multiple business pipelines must coexist.

We need a clean split where:
- framework capabilities are distributed as NuGet packages for plugin authors;
- a dockerized host loads multiple pipeline implementations from plugin DLLs at container startup;
- API behavior remains stable and secure with immutable request/response DTOs and standardized RFC7807 error handling.

## What Changes

- Define a framework/application split with framework-first packaging for extension developers.
- Introduce plugin self-registration at startup through a plugin bootstrap contract instead of broad reflection scanning.
- Support multiple `IChatRequestPipeline` implementations, each exposing a unique `Name`.
- Add route-based pipeline selection and deterministic fallback behavior for the default route.
- Introduce a dedicated stateless application instance for non-interactive mode that accepts caller-provided `IEnumerable<ContextMessage>` per request and bypasses host-side conversation summarization.
- Standardize plugin-related operational errors as generic RFC7807 responses with redeploy guidance, without leaking internal plugin details.

## Capabilities

### New Capabilities
- `plugin-pipeline-hosting`: Startup-time plugin loading from a `Plugins/` folder, plugin self-registration, multi-pipeline registry, and route-based pipeline resolution for the API host.

### Modified Capabilities
- `request-access-modes`: Extend API access behavior to support named pipeline routing and strict default-route behavior when zero/one/multiple pipelines are loaded.

## Non-goals

- No runtime/hot plugin discovery after container startup.
- No plugin trust/isolation model design in this change.
- No server-managed chat context or host-side context summarization in non-interactive mode (conversation state and summarization are managed by API callers).
- No exposure of plugin internal diagnostics in public API error payloads.

## Impact

- Affects host composition and request routing in `AgentMeshCLI` (controllers, startup registration, error mapping).
- Affects framework contracts for pipeline identification (`IChatRequestPipeline.Name`) and plugin bootstrap contract.
- Requires packaging and documentation updates for framework NuGet distribution and plugin authoring model.
- Preserves existing `WorkflowResult` response contract while enabling multi-pipeline extensibility.