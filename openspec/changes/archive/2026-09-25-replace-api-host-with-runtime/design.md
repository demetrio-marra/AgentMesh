## Context

See `proposal.md` for motivation. Today `AgentMesh.Api` is an ASP.NET Core executable that owns configuration, API registration, middleware, Swagger, Docker packaging, and startup-time loading of one plugin DLL from a configured directory. The plugin bootstrap calls `ApplicationRuntime.RegisterCommonServices` and manually registers its concrete pipelines and supporting types.

The target reverses that relationship: each plugin is the executable web composition root, while the renamed `AgentMesh.Runtime` is a reusable class library containing the existing HTTP surface and framework bootstrap. Plugin implementations remain discoverable by reflection, but their assembly is already loaded through a compile-time package dependency rather than a custom load context. The configured architecture-baseline path is absent; this design is grounded in the current implementation, durable specs, and available archived host/plugin designs.

## Goals / Non-Goals

**Goals:**

- Preserve the existing HTTP routes, authentication, callbacks, Swagger, pipelines, and stateless request behavior.
- Give each plugin explicit ownership of configuration loading, custom dependency registration, final application construction, executable startup, and deployment.
- Keep registration of recognized AgentMesh components framework-driven and reflection-based.
- Publish one plugin-facing Runtime package that provides both Runtime and Application assemblies.
- Make startup ordering explicit and prevent Runtime from building an intermediate service provider.

**Non-Goals:**

- Supporting dynamic DLL discovery, a `Plugins/` directory, hot loading, or multiple plugin assemblies in one process.
- Automatically registering arbitrary plugin-specific types or binding arbitrary plugin configuration classes.
- Renaming all existing Application namespaces or merging Application source into Runtime.
- Changing public HTTP contracts or pipeline execution behavior.
- Creating unit tests or test projects.

## Decisions

### Use an empty marker as the plugin assembly anchor

Add an empty `IAgentMeshPlugin` contract. Every executable plugin provides one concrete marker implementation and calls Runtime through a generic API such as:

```csharp
AgentMeshRuntime.LoadAgentMesh<DefaultPipelinePlugin>(builder);
```

Runtime obtains the plugin assembly from `typeof(TPlugin).Assembly`. The generic constraint guarantees that callers deliberately identify a plugin without exposing registration or configuration methods on the marker.

Alternative: infer the entry assembly. Rejected because test hosts, hosting tools, and indirect startup can make the entry assembly ambiguous. Alternative: pass an `Assembly` directly. Rejected because a typed marker is harder to misconfigure and gives plugin authors a stable contract.

### Reflection-register only recognized AgentMesh extension contracts

Move assembly discovery into the Runtime/Application initialization path and scope it to the marker assembly. Registration uses an explicit contract-to-registration policy that preserves existing lifetimes and service mappings for framework-recognized categories, including:

- `IChatRequestPipeline` and `ISummarizationPipeline` implementations as scoped pipeline services.
- `IEWParameterConfiguration` implementations as their concrete types and as parameter configuration services.
- `IEWStep` and `IEWAgenticStep` implementations as their concrete types using the established step lifetime.
- `IEWAgent` implementations as their concrete types and as agent services using the established agent lifetime.
- `IAgentInputSerializer`, `IEWParameterSerializer`, and other documented framework extension contracts according to their existing resolution semantics.

Discovery excludes abstract types and open generics, de-duplicates types implementing overlapping contracts, and validates singleton-role constraints such as one chat pipeline and at most one summarization pipeline. It does not traverse arbitrary referenced assemblies or register unrelated classes.

Alternative: retain plugin bootstrap registration. Rejected because it duplicates framework conventions in every plugin and conflicts with the requirement that Runtime instantiate framework entities. Alternative: scan every loaded assembly. Rejected because it creates accidental registrations and makes plugin boundaries unclear.

### Keep plugin-specific dependencies explicit and ordered before Runtime

The plugin loads configuration and registers only dependencies outside recognized AgentMesh contracts before calling `LoadAgentMesh`. For the sample this includes binding and registering `CodeModeWorkflowConfiguration`; Runtime does not need a generic configuration marker. Reflected components are descriptors at this stage and are instantiated only after the plugin builds the application, so their constructors resolve the earlier plugin registrations normally.

Runtime registration must not call `BuildServiceProvider`, `builder.Build`, or otherwise instantiate the final graph during registration.

Alternative: reflect and auto-bind configuration classes. Rejected because section names and intended lifetimes cannot be inferred safely and configuration ownership belongs to the plugin.

### Split registration from application startup

Expose two Runtime operations with separate responsibilities:

1. `LoadAgentMesh<TPlugin>(WebApplicationBuilder builder)` registers Application services, infrastructure adapters, API authentication, callbacks, controllers, JSON options, Swagger, and marker-assembly components. Controller registration explicitly adds the Runtime assembly as an MVC application part because Runtime is no longer the entry assembly.
2. `StartAgentMesh(WebApplication app, CancellationToken cancellationToken = default)` configures authentication, authorization, controller endpoints, Swagger, and Swagger UI, then starts the host asynchronously.

The plugin owns `builder.Build()` between these calls. `StartAgentMesh` accepts only the built application and cannot add service registrations.

The required plugin startup order is:

```text
Create WebApplicationBuilder
        |
        v
Load appsettings.json
        |
        v
Overlay appsettings.{Environment}.json when present
        |
        v
Load environment variables and register plugin-specific dependencies
        |
        v
LoadAgentMesh<TPlugin>(builder)
        |
        v
builder.Build()                 <-- plugin-owned final build
        |
        v
StartAgentMesh(app)
```

Alternative: return a built application from `LoadAgentMesh`. Rejected because it removes plugin control of the composition boundary. Alternative: expose one callback-based bootstrap method. Rejected because it obscures ordering and still gives Runtime effective ownership of the build.

### Make each plugin the deployable web project

Convert the sample plugin to the Web SDK with an executable `Program`, launch profile, active base and development settings, and Dockerfile. Remove `Program`, launch settings, active settings, and Dockerfile ownership from Runtime. Remove `PluginBootstrapLoader`, its custom `AssemblyLoadContext`, plugin-path configuration, unavailable-host fallback, and the old bootstrap contract.

The CLI remains a REST client and targets the selected plugin service without taking on pipeline composition.

Alternative: keep a thin generic API executable alongside plugin executables. Rejected because it preserves two composition models and the dynamic-loading deployment path being retired.

### Bundle Application inside the Runtime NuGet package

The source dependency remains `AgentMesh.Runtime -> AgentMesh.Application -> AgentMesh`. The Runtime package places both `AgentMesh.Runtime.dll` and `AgentMesh.Application.dll`, with their XML documentation as applicable, in the package's target-framework library assets. Application is bundled as an implementation assembly rather than represented as a dependency on `AgentMesh.Framework.Application`; generation of that retired package stops. Remaining framework and infrastructure dependencies continue through package metadata or bundled assets according to their existing packaging boundaries.

The sample plugin references only published AgentMesh packages, led by `AgentMesh.Runtime`, and has no direct Runtime/Application project reference.

Alternative: merge Application code into Runtime. Rejected because it creates unnecessary namespace and source churn. Alternative: keep Application as a package dependency. Rejected because the Application package is explicitly retired.

### Package inactive starter templates

Include `appsettings.json.template` and `Dockerfile.template` in the Runtime NuGet package as discoverable starter content. They must not be renamed, copied, or interpreted automatically as active plugin files during restore, build, or execution. The README explains that plugin developers create and maintain plugin-level `appsettings.json`, optional `appsettings.{Environment}.json`, and `Dockerfile` files from these samples.

The settings template demonstrates explicit loading of required base settings and an optional environment overlay. The Dockerfile template builds and runs the consuming plugin executable rather than Runtime.

Alternative: use NuGet content files that automatically materialize active files. Rejected because restore could overwrite or silently activate deployment-specific configuration.

## Risks / Trade-offs

- [Bundled Application assembly is omitted or emitted as a retired package dependency] -> Inspect the produced Runtime package and restore a plugin using only published packages.
- [Runtime controllers are not discovered from a class library] -> Register the Runtime assembly explicitly as an MVC application part and manually exercise Swagger and API routes.
- [Reflection registration changes lifetimes or duplicates descriptors] -> Centralize a deterministic contract-to-lifetime table, de-duplicate overlapping implementations, and inspect the built descriptor set.
- [A reflected component requires an unregistered plugin-specific dependency] -> Fail through normal dependency-injection validation/resolution and document that custom dependencies must be registered before `LoadAgentMesh`.
- [Plugin calls startup methods out of order] -> Keep the method signatures phase-specific: registration accepts the builder, startup accepts only a built application.
- [Environment settings are skipped] -> Put the complete base-plus-environment loading sequence in the sample `Program`, settings template guidance, and README.
- [Removing dynamic loading breaks existing deployments] -> Treat this as a breaking release and provide migration instructions from mounted DLLs to independently built plugin services.

## Migration Plan

1. Rename the API project and assembly to Runtime, convert it to a class library, add the Application project dependency, and establish Runtime package metadata and bundled Application assets.
2. Introduce the marker contract and Runtime registration/startup APIs; move common Application registration behind Runtime and replace broad assembly traversal with marker-assembly discovery.
3. Remove dynamic plugin loading, plugin-path configuration, the old bootstrap contract, and Runtime-owned executable/deployment assets.
4. Convert the sample plugin to a web executable, add its marker and `Program`, register `CodeModeWorkflowConfiguration` before Runtime, and move active settings, launch, and Docker assets into it.
5. Add inactive settings and Dockerfile templates to the Runtime package and update README and architecture documentation.
6. Build package projects, inspect Runtime package contents/dependencies, restore and publish the sample plugin from packages, and manually exercise startup, Swagger, authentication, chat, summarization, and callbacks. No unit-test creation is included.

Rollback requires restoring the prior API executable, Application package, plugin bootstrap, mounted plugin directory, and API-owned deployment configuration. No persisted data migration is involved.