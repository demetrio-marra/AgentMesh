## 1. Stabilize Shared Contracts

- [x] 1.1 Move the infrastructure-facing OpenAI and agent-memory contracts and their adapter-consumed chat and memory models from `AgentMesh.Application` to the core framework layer, preserving compatible public namespaces/signatures where possible, and verify every infrastructure project builds without referencing `AgentMesh.Application`.
- [x] 1.2 Update the OpenAI and Mem0 infrastructure projects to reference the core contract/model owner and verify their existing implementations still satisfy the contracts with `dotnet build` for each project.

## 2. Extract Application Composition

- [x] 2.1 Add the required infrastructure project references and configuration/logging/hosting packages to `AgentMesh.Application`, then verify restore resolves a cycle-free project graph.
- [x] 2.2 Move the common configuration setup and service-registration implementation into a public `AgentMesh.Application` composition API, preserving plugin bootstrap, parameter/step/agent discovery, pipeline registration, resilience, and infrastructure adapter registrations; verify `AgentMesh.Application` builds.
- [x] 2.3 Remove the CLI-owned composition implementation and update all affected namespaces/usings; verify no source or project file retains a `HostComposition` reference in `AgentMeshCLI`.

## 3. Relocate Shared Runtime Assets

- [x] 3.1 Move common `appsettings` and prompt content to an application-owned location and configure `AgentMesh.Application` packaging/content metadata; verify the files are present in its build output or package content as intended.
- [x] 3.2 Update CLI and API project content rules to copy the shared application-owned assets into their respective outputs, while retaining API-only configuration in `AgentMesh.Api`; verify neither API content rule references the CLI project.

## 4. Decouple Entry Points

- [x] 4.1 Update `AgentMeshCLI` startup to call the application-layer composition API before registering only console progress notification and interactive input hosting; verify `dotnet build AgentMeshCLI/AgentMeshCLI.csproj` succeeds.
- [x] 4.2 Update `AgentMesh.Api` startup to call the same application-layer composition API before registering only API authentication, controllers, Swagger, and API progress behavior; remove its `AgentMeshCLI` project reference and linked CLI content items, then verify `dotnet build AgentMesh.Api/AgentMesh.Api.csproj` succeeds.

## 5. Verify Independent Hosts

- [x] 5.1 Build `AgentMesh.sln` and the sample plugin projects, verifying there are no circular project references and both executable project graphs exclude the other executable.
- [x] 5.2 Start the console host using valid development configuration and verify its interactive workflow initializes with the shared prompts and settings.
- [x] 5.3 Start the API host using valid development configuration and verify Swagger loads and an existing API-key-authenticated request preserves the current response contract.