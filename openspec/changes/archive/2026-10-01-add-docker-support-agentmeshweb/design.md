## Context

See proposal.md for motivation and `specs/web-chat-containerization/spec.md` for the deployment contract. AgentMeshWeb is a standalone ASP.NET Core MVC .NET 8 host with no project references to AgentMesh runtime or infrastructure projects. Its `Program.cs` already uses standard JSON and environment-variable configuration, validates its API settings at startup, and keeps the API key on the server.

The repository has no Docker assets for AgentMeshWeb. Its source is self-contained, but building from the repository root aligns with the existing solution-level packaging convention and permits a consistent command documented in the README.

## Goals / Non-Goals

**Goals:**
- Produce a repeatable, small production image for the web chat host.
- Preserve server-side secret handling and existing configuration binding.
- Make the image build and local run procedure explicit for operators.

**Non-Goals:**
- Modify the web host's application behavior, routes, SignalR hub, or chat-state storage.
- Add compose files, orchestration resources, health endpoints, or deployment automation.
- Embed production configuration or secrets in source control or image layers.

## Decisions

### Decision 1: Use a multi-stage .NET 8 Dockerfile in AgentMeshWeb

- **Choice:** Restore and publish AgentMeshWeb in an SDK stage, then execute its published assembly in the matching ASP.NET Core runtime stage.
- **Rationale:** The project targets .NET 8; this approach removes SDK and restore tooling from the final image while retaining standard framework deployment behavior.
- **Alternatives considered:** A single-stage SDK image is simpler but unnecessarily increases production image size. A self-contained image adds size and runtime maintenance without a stated host requirement.

### Decision 2: Build from the repository root and use root-level context exclusions

- **Choice:** Document a repository-root `docker build` command that references `AgentMeshWeb/Dockerfile` and add or extend a root `.dockerignore` for generated outputs, source-control metadata, and local development artifacts.
- **Rationale:** One consistent build context permits future shared solution inputs without changing the documented command and keeps irrelevant content out of Docker transfer and cache invalidation.
- **Alternatives considered:** A project-directory build context is currently possible but would establish a less flexible command and a separate ignore policy.

### Decision 3: Bind HTTP explicitly and configure upstream access at runtime

- **Choice:** Configure a stable HTTP listening port in the image and document environment variables for `Api__BaseUrl`, `Api__ApiKey`, and `Api__HeaderName` at `docker run` time.
- **Rationale:** A stable port makes host mapping clear, while the existing configuration provider binds secrets after image creation and continues to prevent browser exposure.
- **Alternatives considered:** Copying an environment-specific settings file or passing credentials as Docker build arguments would expose secrets in build context or image history.

### Decision 4: Limit documentation to Docker operation plus readiness statement

- **Choice:** Add a README subsection for building and running AgentMeshWeb in Docker and state that its image can be deployed by Kubernetes.
- **Rationale:** It communicates the requested readiness without expanding this change into platform-specific resources or configuration.
- **Alternatives considered:** Deployment manifests and charts are outside the requested Docker-only scope.

## Risks / Trade-offs

- **[Risk]** A container can start with syntactically present but unreachable upstream API settings. **Mitigation:** Retain the existing startup validation and document that the configured AgentMesh API must be network-reachable from the container.
- **[Risk]** Root build context can grow as the repository grows. **Mitigation:** Maintain targeted root `.dockerignore` exclusions and publish only the web project output in the final stage.
- **[Trade-off]** No readiness endpoint is added. **Mitigation:** Validate local image startup using the existing web page and defer health-check design to a separate change.

## Migration Plan

1. Build the image with the README's repository-root Docker command.
2. Run it with the declared port mapping and runtime API configuration.
3. Verify the chat page loads and that a request reaches the configured upstream API.
4. Roll back by stopping the container and running `dotnet run --project AgentMeshWeb`; no data migration is required because in-memory chat state remains unchanged.

## Open Questions

None.