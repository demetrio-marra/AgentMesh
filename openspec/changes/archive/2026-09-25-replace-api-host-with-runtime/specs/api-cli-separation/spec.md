## ADDED Requirements

### Requirement: Plugin entry point SHALL be web-only
Each pipeline plugin SHALL be an ASP.NET Core web executable that hosts the existing authenticated HTTP API and executes requests through the stateless application path supplied by the Runtime package.

#### Scenario: Launch a plugin web service
- **WHEN** a plugin web project starts
- **THEN** it exposes the existing API routes and Swagger documentation

#### Scenario: Process an authenticated request
- **WHEN** a client sends a valid API-key-authenticated request to the plugin service
- **THEN** the service returns the existing workflow result contract and preserves the single-pipeline request behavior

### Requirement: Runtime and plugin build boundaries SHALL be independent
`AgentMesh.Runtime` SHALL be a reusable class-library package that references Application at compile time, and each plugin web executable SHALL consume Runtime through its NuGet package without dynamic plugin assembly loading. The Runtime package SHALL provide both the Runtime and Application assemblies transitively to plugin consumers, and `AgentMesh.Framework.Application` SHALL no longer be published.

#### Scenario: Build Runtime independently
- **WHEN** a developer restores and builds the Runtime project
- **THEN** it builds as a class library without an executable entry point

#### Scenario: Restore a plugin dependency graph
- **WHEN** a developer restores a plugin web project
- **THEN** the `AgentMesh.Runtime` package supplies the Runtime and Application assemblies required to compile and run the plugin

#### Scenario: Inspect retired package dependencies
- **WHEN** a developer inspects the plugin's package graph
- **THEN** it contains no dependency on `AgentMesh.Framework.Application`

### Requirement: Runtime configuration SHALL be plugin-owned
Each plugin web executable SHALL own its active runtime configuration and SHALL load required `appsettings.json` and optional `appsettings.{Environment}.json` files before initializing AgentMesh. Runtime SHALL distribute inactive settings and Dockerfile templates as starting samples rather than active host assets.

#### Scenario: Plugin starts from its own configuration
- **WHEN** a plugin service starts in a named environment
- **THEN** it loads its required `appsettings.json`, overlays an available `appsettings.{Environment}.json`, and makes the resulting configuration available to Runtime initialization

#### Scenario: Plugin developer uses packaged templates
- **WHEN** a plugin developer inspects the Runtime package and project documentation
- **THEN** they can use `appsettings.json.template` and `Dockerfile.template` to create plugin-owned active files

#### Scenario: Runtime templates remain inactive
- **WHEN** a plugin consumes the Runtime package without creating active files from the templates
- **THEN** the templates are not automatically treated as application configuration or as the plugin Docker build file

## MODIFIED Requirements

### Requirement: Launch profiles select the host
The development launch configuration SHALL expose `Interactive` for the console project and `Web` for a plugin web executable.

#### Scenario: Select Interactive
- **WHEN** a developer selects `Interactive`
- **THEN** the console project launches as the active target

#### Scenario: Select Web
- **WHEN** a developer selects `Web`
- **THEN** the plugin web project launches as the active target and opens its web documentation entry point

### Requirement: Shared runtime configuration remains compatible
The CLI frontend and each plugin web host SHALL use the configuration required for their own responsibilities, while plugin-owned settings supply the Runtime, infrastructure, pipeline, and API values required to execute the plugin's service.

#### Scenario: Run with existing configuration
- **WHEN** configuration values from an existing deployment are moved into the plugin-owned base and environment settings
- **THEN** the plugin service initializes the same infrastructure and pipeline behavior without requiring Runtime-owned settings

#### Scenario: Run with plugin-owned configuration
- **WHEN** a plugin host starts with valid base and environment configuration
- **THEN** its workflow initializes without requiring settings files from Runtime or another executable project

### Requirement: API and CLI documentation SHALL identify executable roles
The architecture documentation for this capability SHALL identify a plugin web project as the executable API service, `AgentMesh.Runtime` as its reusable HTTP and framework runtime package, and `AgentMeshCLI` as the REST terminal frontend. It SHALL not describe Runtime as an executable host or the CLI as a composition root for pipeline runtime services.

#### Scenario: Contributor reviews host responsibilities
- **WHEN** a contributor reads the API and CLI separation documentation
- **THEN** they can distinguish plugin-host, Runtime-package, and REST-terminal responsibilities

## REMOVED Requirements

### Requirement: API entry point is web-only
**Reason**: The standalone API executable is replaced by plugin-owned web executables that consume the Runtime package.

**Migration**: Move API startup responsibilities to the plugin `Program` and use Runtime initialization methods for framework and HTTP behavior.

### Requirement: Executable hosts are build-independent
**Reason**: Its API-host and dynamically loaded plugin dependency rules are replaced by the Runtime package and compile-time plugin model.

**Migration**: Reference `AgentMesh.Runtime` from each plugin web project and remove API-host plugin discovery and Application package references.

### Requirement: Runtime configuration SHALL be merged into API-owned files
**Reason**: Active runtime configuration is now owned by each executable plugin rather than by a standalone API project.

**Migration**: Create plugin-level base and environment settings from the packaged settings template and move deployment-specific values there.