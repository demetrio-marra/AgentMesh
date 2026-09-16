## Purpose

Provides a reproducible container image and runtime contract for deploying the AgentMesh.Api HTTP host with its published .NET assets and external configuration supplied at runtime.

## ADDED Requirements

### Requirement: API container image can be built from the repository
The project SHALL provide a Docker build definition that restores, builds, publishes, and packages `AgentMesh.Api` as a deployable .NET 8 container image without requiring locally generated `bin` or `obj` artifacts.

#### Scenario: Build a production image
- **WHEN** a user runs a Docker build using the repository as the build context
- **THEN** Docker produces an image containing the published `AgentMesh.Api` application and its required runtime assets

### Requirement: API container listens on a documented HTTP port
The container image SHALL declare the HTTP port used by the API and SHALL configure the application to listen on that port when started with its default container command.

#### Scenario: Start the container with default settings
- **WHEN** a user starts the image and maps the declared container port to a host port
- **THEN** the API process starts and accepts HTTP requests through the mapped host port

### Requirement: Runtime configuration remains externally supplied
The container SHALL preserve the API's existing configuration model, including API key and dependency settings, so deployment-specific values can be supplied through mounted configuration or environment variables without rebuilding the image.

#### Scenario: Start with deployment configuration
- **WHEN** a user supplies valid runtime configuration through supported ASP.NET Core configuration sources
- **THEN** the container starts using those values and does not require secrets to be embedded in the image definition

### Requirement: Docker build context excludes local-only artifacts
The repository SHALL define Docker build-context exclusions for source-control metadata, local build outputs, temporary files, and development-only artifacts that are not required to build the API image.

#### Scenario: Build with a clean context
- **WHEN** Docker sends the repository as a build context
- **THEN** excluded local-only files are not sent to the Docker daemon and cannot be copied into the resulting image through normal build steps
