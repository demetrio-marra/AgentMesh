## Context

See [proposal.md](proposal.md) for motivation. `AgentMesh.Api` from `AgentMesh.Framework.Application` to configure runtime services and load plugins. The `AgentMesh` DLL is the shared library consumed by the API and Application DLLs. Plugin DLLs reference the Application DLL through its NuGet package. Plugin assemblies use the core `IAgentMeshPluginBootstrap` contract, while the API host owns plugin discovery, bootstrap invocation, configuration forwarding, and assembly loading.

## Goals / Non-Goals

**Goals:**

- Let the API host load plugin assemblies using only core framework contracts.
- Provide the API host configuration to plugin bootstrap registration.
- Load plugin assemblies in a way that preserves host and plugin type identity while avoiding duplicate assembly loads.

**Non-Goals:**

- Add a runtime bridge project or a dedicated runtime plugin.
- Change endpoint, authentication, callback, or pipeline-routing behavior.
- Change endpoint, authentication, callback, or pipeline-routing behavior.

## Decisions

### API owns plugin discovery

Move plugin directory resolution, assembly loading, bootstrap discovery, and failure reporting into the API startup path. This removes the dependency that currently enters through `AgentMeshRuntime` and lets API depend only on `AgentMesh.Framework`.

Alternative considered: retain discovery in Application. Rejected because API must invoke discovery before an Application-backed plugin can load, recreating the direct dependency.

### API forwards configuration to plugin bootstraps

The API invokes each discovered `IAgentMeshPluginBootstrap` with its already-built `IConfiguration` instance. Plugin bootstraps consume this host-provided configuration during service registration and do not independently construct or resolve configuration. This keeps configuration source selection and environment precedence owned by the executable host.

Alternative considered: let each plugin resolve its own configuration. Rejected because independently built configuration can diverge from the API host's sources and values.

### API-owned assembly load context prevents duplicate plugin loads

`PluginBootstrapLoader` loads each plugin through a custom `AssemblyLoadContext` backed by `AssemblyDependencyResolver`. Before resolving a plugin dependency, the context returns the host's existing Default-context assembly when the assembly name is already loaded. This preserves type identity for shared contracts and host abstractions and ensures dependencies are not loaded again under a plugin-specific context.

The prior service-registration guard is unnecessary under this design and is removed. Each bootstrap remains responsible for registering its own services and pipelines; existing duplicate pipeline-name validation remains effective.

Alternative considered: special-case contract and framework assembly names. Rejected because reusing any matching host-loaded assembly provides the required type consistency without maintaining a fragile exception list.

### API-visible failure and callback contracts remain outside Application

The API cannot consume Application-only types after the package reference is removed. API-facing pipeline-routing errors and callback context contracts will be placed in an API or core-facing boundary selected during implementation, without moving them into generic domain models solely for convenience.

## Risks / Trade-offs

- [Plugin discovery fails after partial registrations] -> Load each assembly defensively, surface an actionable startup error, and preserve current invalid-plugin routing behavior where applicable.
- [A plugin dependency differs from the host copy] -> Reusing an already loaded host assembly makes the host version authoritative for shared dependencies; plugin deployment must remain compatible with that version.
- [Application package assets are not deployed with plugins] -> Verify published API output includes plugin dependencies and required prompt assets.

## Migration Plan

1. Move plugin discovery into API startup and pass the host configuration to each bootstrap.
2. Load plugin assemblies through `PluginBootstrapLoader` and its custom assembly load context.
3. Remove the Application package reference and imports from API, replacing any API-consumed Application types with an appropriate host-facing contract.
4. Build and run the API with plugin deployment assets; verify configuration forwarding, shared type identity, and existing request, callback, and duplicate-pipeline behavior.
5. Roll back by restoring the existing Application package reference and startup composition if deployment validation fails.