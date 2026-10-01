## Why

`AgentMeshWeb` currently requires a local .NET runtime and manual host setup, which makes its deployment less repeatable than the container-ready AgentMesh Runtime plugin pattern. A production Docker image gives operators a consistent way to run the standalone web chat client while keeping its upstream API credentials supplied at deployment time.

## What Changes

- Add a multi-stage .NET 8 Docker build for `AgentMeshWeb`, with an explicit HTTP listening port and the published web host as its entry point.
- Add Docker build-context exclusions for local artifacts and source-control metadata.
- Document building, configuring, and running the `AgentMeshWeb` container in the README, including a concise statement that the image can be deployed by Kubernetes without adding Kubernetes resources to this repository.

## Non-goals

- Adding Kubernetes manifests, Helm charts, or Kubernetes-specific application configuration.
- Adding Docker Compose services or containerizing the AgentMesh Runtime API and external services.
- Changing `AgentMeshWeb` MVC, SignalR, API-client, conversation-state, or authentication behavior.
- Embedding API credentials or other deployment secrets in the Docker image.

## Capabilities

### New Capabilities
- `web-chat-containerization`: Docker build, runtime configuration, and operational contract for the standalone web chat host.

### Modified Capabilities

- None.

## Impact

- `AgentMeshWeb/Dockerfile`: New multi-stage container build and runtime entry point.
- `.dockerignore`: Docker build-context exclusions, if the repository does not already provide the required root-level exclusions.
- `README.md`: Docker usage and Kubernetes-readiness documentation for the web chat host.