## Context

The current web-enabled executable combines common dependency registration, plugin loading, console hosting, API hosting, authentication, controllers, and Swagger setup in `AgentMeshCLI/Program.cs`. `AppInstance` is stateful for the console, while `StatelessAppInstance` is already the appropriate execution boundary for HTTP requests. See `proposal.md` for motivation and `specs/api-cli-separation/spec.md` for the observable requirements.

## Goals / Non-Goals

**Goals:**

- Give `AgentMeshCLI` a single console startup path.
- Add `AgentMesh.Api` as the only ASP.NET Core web entry point.
- Reuse the existing common service/plugin composition without duplicating pipeline behavior.
- Keep API-specific authentication, controllers, Swagger, and stateless hosting isolated from the CLI.
- Make solution and launch configuration identify `Interactive` with the CLI and `Web` with the API.

**Non-Goals:**

- Introducing a new API version or changing endpoint contracts.
- Reworking the plugin framework or pipeline registry.
- Changing application-layer state, summarization, or execution semantics.

## Decisions

### Decision 1: Add a dedicated ASP.NET Core project

`AgentMesh.Api` will own the web executable, API controllers, API authentication types/configuration, Swagger configuration, API settings, and web launch profile. This makes the HTTP boundary explicit and prevents web SDK behavior from being the console project's responsibility.

**Alternative considered:** Keep the API in `AgentMeshCLI` and remove only the switch. This would leave the two runtime boundaries coupled and would not provide a distinct API launch target.

### Decision 2: Keep shared host composition reusable

The common configuration loading, infrastructure registration, plugin bootstrap, pipeline initialization, and application service registration will be exposed through a reusable host-composition surface that both entry points call. API-only registrations remain in `AgentMesh.Api`; console-only registrations remain in `AgentMeshCLI`.

**Alternative considered:** Duplicate registration code in both `Program.cs` files. This is simpler initially but risks configuration and plugin behavior diverging between hosts.

### Decision 3: Make the CLI startup unconditional

The CLI will construct the generic host, register console progress notification and `UserConsoleInputService`, and run it without inspecting `--interactive`. The old switch will have no mode-selection role and will not start a web host.

**Alternative considered:** Keep accepting the switch as a compatibility alias. That preserves an obsolete invocation contract and obscures that the executable is console-only.

### Decision 4: Preserve the API processing boundary

The API will retain the existing controller routes, API-key authentication, Swagger documentation, and `StatelessAppInstance` dependency. Existing configuration and content files needed at runtime will be available to the API output alongside the CLI output.

**Alternative considered:** Move request handling into the CLI and proxy from the API project. This adds an unnecessary process boundary and makes API lifecycle behavior harder to reason about.

## Risks / Trade-offs

- **[Risk]** Shared composition may remain coupled to an executable project during extraction. **Mitigation:** Keep the reusable surface free of API and console startup types, and verify both projects build from the solution.
- **[Risk]** Separate output directories can cause missing prompts or configuration at runtime. **Mitigation:** Declare explicit copy-to-output rules for both projects and validate development launch profiles.
- **[Risk]** Existing users may still invoke `AgentMeshCLI --interactive`. **Mitigation:** The CLI remains console-only, so the invocation continues into the console path without mode switching; documented launch configuration uses `Interactive` without the switch.
- **[Trade-off]** Two executables require maintaining two launch profiles and startup configurations. **Mitigation:** Keep API-specific setup isolated and common registration centralized.

## Migration Plan

1. Extract or expose shared host composition and update the CLI to use it as a console host.
2. Create `AgentMesh.Api`, move API-owned source and configuration, and register the shared composition plus stateless web services.
3. Add the API project to the solution and update launch/debug profiles and active profile names.
4. Build the solution and smoke-check both launch targets, including Swagger and an authenticated existing request route.

Rollback is to restore the previous single-project web/console host and launch profile configuration while retaining the unchanged application-layer contracts.

## Open Questions

None.