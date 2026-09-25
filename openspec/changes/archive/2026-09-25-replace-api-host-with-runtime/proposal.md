## Why

The current executable API host and dynamically loaded plugin model split composition across deployment artifacts and force plugin developers to target a separate Application package. Making each plugin the executable composition root while publishing the HTTP and framework runtime as one package gives plugin developers direct control over configuration, custom dependencies, hosting, and deployment.

## What Changes

- **BREAKING** Rename `AgentMesh.Api` to `AgentMesh.Runtime`, convert it from a web executable to a class library, and publish it as the `AgentMesh.Runtime` NuGet package.
- **BREAKING** Stop publishing `AgentMesh.Framework.Application`; include both Runtime and Application assemblies in the Runtime package while preserving the compile-time chain `AgentMesh.Runtime -> AgentMesh.Application -> AgentMesh`.
- **BREAKING** Convert pipeline plugins from dynamically loaded class libraries into ASP.NET Core web executables that reference `AgentMesh.Runtime` and own the composition root.
- Remove the `Plugins/` directory lifecycle, startup DLL discovery, custom assembly load context, and plugin bootstrap registration contract.
- Add an empty `IAgentMeshPlugin` marker used by Runtime to locate the plugin assembly and reflection-register all recognized pipeline, summarization pipeline, step, parameter, agent, serializer, and other documented framework extension types.
- Split Runtime startup into `LoadAgentMesh`, which registers framework and HTTP services without building the provider, and `StartAgentMesh`, which configures and starts the built web application.
- Require plugin startup to create the builder, load `appsettings.json` and optional environment-specific settings, register plugin-owned dependencies, call `LoadAgentMesh`, build the application, and call `StartAgentMesh` in that order.
- Move concrete settings, launch, and Docker ownership to plugin projects. Package inactive `appsettings.json.template` and `Dockerfile.template` starting samples with Runtime and document their use.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `api-cli-separation`: Replace the executable API host with a Runtime library consumed by plugin-owned web hosts.
- `plugin-pipeline-hosting`: Replace dynamic plugin loading with compile-time Runtime references, reflection registration, and plugin-owned composition.
- `default-chat-pipeline-plugin`: Convert the sample plugin into the executable web service and make it own settings and deployment assets.
- `architecture-documentation`: Document the new package, composition, startup, and deployment boundaries.

## Impact

- Affected projects: `AgentMesh.Api`, `AgentMesh.Application`, `AgentMesh`, the sample plugin, the solution, and documentation.
- Affected packaging: Runtime package contents and dependencies, retirement of the Application package, and plugin package references.
- Affected deployment: plugin projects become the only web executables and own Dockerfiles, settings, launch profiles, and final service-provider construction.
- Existing HTTP routes, API authentication, callbacks, Swagger behavior, pipeline execution, and CLI-as-REST-client behavior remain supported.

## Non-goals

- Changing pipeline execution semantics, HTTP contracts, callback contracts, authentication behavior, or CLI frontend behavior.
- Automatically registering arbitrary plugin-specific services or configuration types that do not implement recognized AgentMesh extension contracts.
- Creating unit tests or test projects.