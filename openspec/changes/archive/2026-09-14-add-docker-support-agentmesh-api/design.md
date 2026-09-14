## Context

See `proposal.md` for motivation and `specs/docker-api-support/spec.md` for the externally visible container contract. `AgentMesh.Api` is an ASP.NET Core .NET 8 executable project with linked shared configuration from `AgentMeshCLI`, API-specific JSON settings, and runtime dependencies resolved through the application framework.

The Docker build context must include the solution and referenced project sources because the API project depends on repository projects and package assets. The final image should contain only published output and the ASP.NET Core runtime, while configuration and secrets remain deployment concerns.

## Goals / Non-Goals

**Goals:**

- Produce a reproducible, small production image through a multi-stage .NET build.
- Keep the API's existing configuration loading and authentication behavior intact.
- Make the default container port and startup behavior explicit and testable.
- Keep build context and final image free of local development artifacts.

**Non-Goals:**

- Change controllers, authentication contracts, or application pipeline behavior.
- Bundle external infrastructure services or introduce Docker Compose.
- Move secrets into source control or the image.

## Decisions

### Decision 1: Use a repository-root multi-stage Dockerfile

- **Choice:** Place the Dockerfile under `AgentMesh.Api` and build from the repository root, copying the project/solution sources needed for restore and publish.
- **Rationale:** The API depends on sibling projects and linked configuration, so a project-local context would be incomplete. Multi-stage builds keep SDK tooling out of the runtime image.
- **Alternative considered:** A Dockerfile that builds only from `AgentMesh.Api` would fail to restore or publish repository dependencies consistently.

### Decision 2: Target the ASP.NET Core .NET 8 runtime image

- **Choice:** Publish the application in an SDK stage and run it in the matching ASP.NET Core runtime stage.
- **Rationale:** This matches the project's target framework and avoids shipping the compiler and restore toolchain.
- **Alternative considered:** A self-contained deployment would increase image size and duplicate runtime maintenance without a requirement for non-.NET hosts.

### Decision 3: Use standard ASP.NET Core environment configuration

- **Choice:** Leave API key and service configuration outside the image, relying on the existing JSON and environment-variable configuration providers at runtime.
- **Rationale:** This preserves current behavior and prevents deployment secrets from becoming image layers.
- **Alternative considered:** Copying environment-specific secrets into the image is not portable and creates an avoidable security risk.

### Decision 4: Declare one stable HTTP container port

- **Choice:** Declare the port used by the API's default container startup and set the corresponding ASP.NET Core URL through the container environment.
- **Rationale:** Operators need a predictable port mapping while retaining the ability to override host-side mapping.
- **Alternative considered:** Relying on an implicit framework default would make the deployment contract less discoverable.

## Risks / Trade-offs

- **[Risk]** Linked configuration and package restore may change as the solution evolves. **Mitigation:** Build from the repository root and validate the image with the current solution/project publish path.
- **[Risk]** A container can start without valid external dependency credentials but fail on the first request. **Mitigation:** Keep the existing startup validation and document runtime configuration as a deployment prerequisite.
- **[Trade-off]** No health endpoint is introduced by this change. **Mitigation:** Validate process startup and the existing API/Swagger route; defer dedicated health checks to a separate capability.

## Migration Plan

1. Build the image from the repository root using the new Dockerfile.
2. Supply API key and external dependency configuration through the deployment environment.
3. Run the image with the declared container port mapped to the desired host or orchestrator port.
4. Roll back by stopping the container and continuing to run the existing API project directly; no persistent data migration is required.

## Open Questions

None.
