## Context

See `proposal.md` for motivation.

Current architecture already separates contracts (`AgentMesh`), application logic (`AgentMesh.Application`), and host (`AgentMeshCLI`), but the host currently assumes a single `IChatRequestPipeline` and relies on broad assembly discovery. That model is not sufficient for startup-loaded third-party plugin packs and explicit multi-pipeline routing.

This change introduces a framework-plus-host split while preserving the existing immutable API DTO contract (`ProcessRequestApiInput` and `WorkflowResult`) and preserving operational stability in containerized environments.

Note: `openspec/config.yaml` references `openspec/changes/archive/2026-09-10-architecture-baseline-scan/design.md`, but that artifact is not present in the repository; design constraints were derived from the current source and existing architecture artifacts.

## Goals / Non-Goals

**Goals:**
- Define a NuGet-distributed framework surface for plugin authors.
- Support startup-time loading of multiple pipeline plugins from a `Plugins/` folder.
- Enforce deterministic, case-insensitive route-based pipeline selection.
- Keep host process running even when plugin configuration is invalid.
- Return plugin-related issues as generic RFC7807 responses with redeploy guidance.

**Non-Goals:**
- Runtime hot plugin detection/reload.
- Plugin trust model / sandbox isolation design.
- Changes to input/output DTO schema.
- Public disclosure of internal plugin diagnostics.

## Decisions

### Decision 1: Pipeline identity is exposed via `IChatRequestPipeline.Name`
- **Choice:** Replace method-based identity with a property aligned with `IEWStep.Name`.
- **Rationale:** Consistent naming pattern and lower ceremony for route map creation.
- **Alternatives considered:**
  - `GetName()` method: functionally valid but inconsistent with existing naming style.

### Decision 2: Plugin composition uses self-registration bootstrap
- **Choice:** Plugins provide a bootstrap contract (for example an `IAgentMeshPlugin` implementation) to explicitly register services.
- **Rationale:** Avoids expensive and error-prone wide reflection scans; plugin authors retain exact control over registration.
- **Alternatives considered:**
  - Type scanning only: less explicit and more fragile.
  - Manifest-only metadata: explicit but can drift from actual DI composition.

### Decision 3: Plugin discovery occurs at container startup from `Plugins/`
- **Choice:** Load plugin DLLs only during startup, typically from a mounted volume path.
- **Rationale:** Matches Kubernetes volume workflows and avoids runtime lifecycle complexity.
- **Alternatives considered:**
  - Hot-reload watchers: operationally complex and out of scope.

### Decision 4: Request routing model supports named and default routes
- **Choice:**
  - `POST /api/pipelines/{pipelineName}/requests` resolves by case-insensitive pipeline name.
  - `POST /api/requests` is valid only when exactly one pipeline is loaded.
- **Rationale:** Backward compatibility for single-pipeline deployments, explicit selection for multi-pipeline deployments.
- **Alternatives considered:**
  - Named route only: simpler but breaks single-pipeline clients.
  - Silent first/last pipeline fallback: ambiguous and unsafe.

### Decision 5: Host never fails fast for plugin issues
- **Choice:** Keep service alive and surface plugin configuration problems via API as RFC7807 errors.
- **Rationale:** Aligns with DevOps requirement to avoid crash-loop behavior.
- **Alternatives considered:**
  - Startup fail-fast: clearer boot signal but operationally rejected.

### Decision 6: Plugin-related API errors are generic and operationally actionable
- **Choice:** Return generic RFC7807 payloads without internal plugin details, including redeploy guidance when restart is required.
- **Rationale:** Prevents leakage of internal deployment structure while still guiding operations.
- **Alternatives considered:**
  - Detailed internal diagnostics in response: better debuggability but unsafe for callers.

## Risks / Trade-offs

- **[Risk]** Hidden plugin startup issues may be discovered only on first request path hit.  
  **Mitigation:** Emit detailed structured startup logs and health diagnostics for operators.

- **[Risk]** Generic external errors reduce immediate troubleshooting detail for API consumers.  
  **Mitigation:** Keep external messages stable and actionable, with rich internal logs.

- **[Risk]** Multiple plugins increase registration-order and duplication complexity.  
  **Mitigation:** Validate pipeline names in a case-insensitive registry and mark system as plugin-invalid when conflicts exist.

- **[Trade-off]** Startup-only discovery improves stability but requires redeploy for plugin updates.  
  **Mitigation:** Document release/deploy flow clearly and use immutable deployment artifacts.

## Migration Plan

1. Introduce framework packaging boundaries and publish NuGet package(s) for core extension contracts.
2. Add plugin bootstrap contract and startup plugin loader targeting `Plugins/` folder.
3. Build pipeline registry and validation state (case-insensitive names, duplicate detection).
4. Update request API routing to support named and default route semantics.
5. Add RFC7807 error mapping for all plugin-related invalid states, including redeploy guidance.
6. Update documentation and deployment examples for Kubernetes volume-mounted plugin distribution.
7. Validate backward compatibility for single-pipeline deployments.

Rollback strategy: keep previous single-pipeline host path and disable plugin loading feature flags/configuration if needed.

## Open Questions

None.