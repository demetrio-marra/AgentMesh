## Context

See `proposal.md` for motivation. The current README mixes legacy and current architecture descriptions: it identifies the API as the composition layer, while current project metadata shows `AgentMesh.Application` is packaged as `AgentMesh.Framework.Application` for plugin use. The existing OpenSpec capabilities document API, CLI, plugin, and pipeline behavior but do not consistently name the owning project.

The project-reference graph establishes these intended relationships:

| Project | Responsibility | Relationship |
|---|---|---|
| `AgentMesh` | Basic domain entities | Foundation for framework and contract packages |
| `AgentMesh.Contracts` | Infrastructure service definitions | Consumed by infrastructure adapters |
| `AgentMesh.Infrastructure.*` | External-system adapters | Implement or provide integrations for contracts |
| `AgentMesh.Application` | Reusable agentic-pipeline framework | NuGet package consumed by custom class-library plugins |
| `AgentMesh.Api` | Executable host for custom pipelines | Hosts HTTP runtime and plugin loading |
| `AgentMeshCLI` | REST terminal frontend | Calls the API without owning pipeline runtime composition |

## Goals / Non-Goals

**Goals:**
- Establish the preceding table as the shared vocabulary in README and OpenSpec documentation.
- Describe the dependency and runtime direction: plugins use the application framework; the API hosts plugins and pipelines; the CLI uses API endpoints.
- Keep every existing capability specification consistent with its API, CLI, framework, contract, adapter, plugin, or host responsibility.

**Non-Goals:**
- Reconcile physical source placement, namespaces, or current project references with the documented target boundaries.
- Revise endpoint payloads, callback semantics, authentication, configuration values, or runtime behavior.
- Treat the missing archived baseline file as a prerequisite for this change.

## Decisions

### Use the README as the human-facing architecture reference

The README will receive a concise architecture section, an updated project-structure table, and revised packaging/plugin/API/CLI descriptions. Existing sections will be amended in place rather than duplicating architecture text in a new document. This is the repository entry point and is already the primary onboarding document.

Alternative considered: create a standalone architecture document. Rejected because it would leave the README's conflicting descriptions in place and create two sources for contributors to reconcile.

### Make `architecture-documentation` the canonical OpenSpec capability

The architecture capability will define the complete layer contract. The remaining nine capabilities will receive small ownership clarifications only where their current behavior touches API, CLI, host, plugin, or framework boundaries.

Alternative considered: duplicate the complete project table in every capability. Rejected because repeating the authoritative description would drift; detailed architecture remains centralized while companion specs state only the responsibility relevant to their behavior.

### Derive statements from project metadata and current behavior

The implementation will verify descriptions against project files, package metadata, API/CLI startup code, and the sample plugin before editing prose. Documentation will not infer a source-code migration from namespace or folder layout.

Alternative considered: document the previously configured archived baseline as authoritative. Rejected because `openspec/changes/archive/2026-09-10-architecture-baseline-scan/design.md` is absent.

## Risks / Trade-offs

- [Current source layout may not match the target architectural terminology] -> Describe responsibility boundaries without claiming that files or namespaces have already moved; leave code restructuring to a separate change.
- [Repeated terminology can drift across README and ten specs] -> Update the canonical architecture section first, then apply the same project names and role phrases to each affected spec.
- [The archived baseline reference is unavailable] -> Use current project metadata as the evidence source and either restore or remove the stale configuration reference in a separately scoped maintenance change.

## Migration Plan

1. Update the README architecture and project descriptions using the verified project-role table.
2. Update the main `architecture-documentation` spec, then apply focused responsibility wording to the other nine main specs.
3. Review all edited prose for exact project names and role consistency.
4. Validate the OpenSpec change and inspect the documentation diff; no deployment or runtime migration is needed.

Rollback consists of reverting only the documentation and specification changes; no data, package, or deployed-service rollback is required.