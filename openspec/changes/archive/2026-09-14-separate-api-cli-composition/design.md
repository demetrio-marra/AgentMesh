## Context

See `proposal.md` and `specs/api-cli-separation/spec.md`. `AgentMesh.Api` currently references `AgentMeshCLI` for `HostComposition`, `appsettings` files, and prompts. That composition registers concrete infrastructure adapters, but each infrastructure project currently references `AgentMesh.Application`; copying `HostComposition` unchanged into the application project would create circular project references.

## Goals / Non-Goals

**Goals:**

- Make `AgentMesh.Application` the reusable owner of shared configuration, service composition, plugin bootstrap, and shared runtime assets.
- Preserve the established core -> application -> infrastructure dependency direction where possible while eliminating executable-host coupling.
- Keep each executable responsible only for its host-specific setup.

**Non-Goals:**

- Altering API routes, API-key authentication, console input handling, workflow behavior, or plugin semantics.
- Duplicating common registration in the two entry points.

## Decisions

### Decision 1: Move shared host composition into `AgentMesh.Application`

`AgentMesh.Application` will expose public composition APIs for configuration setup and common service registration. Both `Program` entry points will call this API, then add only their host-owned registrations: console progress/input hosting in `AgentMeshCLI`, and authentication, controllers, Swagger, and stateless API behavior in `AgentMesh.Api`.

**Rationale:** Both hosts consume the same pipeline runtime and configuration rules without depending on another executable.

**Alternative considered:** Keeping composition in the CLI while treating it as a shared library. This retains the invalid host dependency.

### Decision 2: Break the application/infrastructure reference cycle at contracts

Infrastructure-facing contracts and models required to register concrete adapters will be moved or defined in the core `AgentMesh` framework layer. Infrastructure projects will depend on that stable layer instead of `AgentMesh.Application`, allowing `AgentMesh.Application` to reference the infrastructure projects needed by its common composition API.

**Rationale:** It allows the requested application-layer composition ownership without a circular MSBuild graph and keeps adapter contracts independent of a host.

**Alternative considered:** Have each executable register adapters independently. This avoids a graph change but duplicates behavior and invites host drift.

### Decision 3: Make shared runtime assets application-owned content

Shared `appsettings` and prompt files will be owned by `AgentMesh.Application` and explicitly copied to the output of both executable projects. API-only configuration remains in the API project; no API project content item will link to the CLI project.

**Rationale:** Both executables retain self-contained output directories while sharing one authoritative set of common runtime assets.

**Alternative considered:** Keep assets in the CLI and link them into the API. This preserves the same host ownership leak even after the assembly reference is removed.

## Risks / Trade-offs

- **[Risk]** Moving contracts can affect public package compatibility. **Mitigation:** Preserve contract namespaces and signatures where practical, update project references atomically, and build all projects and sample plugins.
- **[Risk]** Output-content relocation can leave a host without settings or prompts. **Mitigation:** Add explicit content-copy rules to both executables and smoke-test startup using existing environment configuration.
- **[Trade-off]** `AgentMesh.Application` gains infrastructure composition knowledge. **Mitigation:** Limit this to registration orchestration; concrete adapter implementations remain in their dedicated infrastructure projects.

## Migration Plan

1. Identify the contracts/models that infrastructure adapters consume from `AgentMesh.Application` and move them to the core framework layer with compatible namespaces or coordinated reference updates.
2. Update infrastructure project references, then add the required infrastructure references to `AgentMesh.Application`.
3. Move `HostComposition` and common assets to `AgentMesh.Application`; preserve plugin loading, pipeline initialization, and common registrations.
4. Update API and CLI project files and programs to use the application-layer composition and host-owned content only; remove the API-to-CLI reference.
5. Restore, build, and run each executable independently; verify API Swagger/authenticated workflow behavior and console startup.

Rollback: revert the coordinated project, composition, and content ownership changes as a single unit to restore the prior graph.