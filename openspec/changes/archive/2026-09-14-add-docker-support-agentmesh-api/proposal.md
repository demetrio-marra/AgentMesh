## Why

AgentMesh.Api currently runs directly as a host application, requiring local runtime setup and manual environment configuration. Adding Docker support enables containerized deployment, consistent runtime dependencies, easy container orchestration, and seamless cloud/microservice deployment for the AgentMesh HTTP API.

## What Changes

- Add a multi-stage `Dockerfile` tailored for `AgentMesh.Api` (.NET 8 SDK build stage, runtime stage).
- Add `.dockerignore` to exclude build artifacts, git repositories, and temporary files from Docker build contexts.
- Update solution/project launch and configuration guidance for containerized execution of `AgentMesh.Api`.

## Non-goals

- Containerizing the CLI application (`AgentMeshCLI`).
- Setting up a multi-container Docker Compose file with external services like LightRag or Mem0 databases.
- Modifying the API endpoint contracts or internal application logic.

## Capabilities

### New Capabilities
- `docker-api-support`: Docker containerization and build configuration for `AgentMesh.Api`.

### Modified Capabilities

## Impact

- `AgentMesh.Api/Dockerfile`: Multi-stage build file for containerizing the API project.
- `.dockerignore`: Root ignore file for Docker context optimization.
