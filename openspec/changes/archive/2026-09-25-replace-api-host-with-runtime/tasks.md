## 1. Runtime Project And Package Boundary

- [x] 1.1 Rename the `AgentMesh.Api` project, assembly, root namespace, solution entry, and remaining project-level references to `AgentMesh.Runtime`; convert it to a non-executable ASP.NET Core-capable class library and verify `dotnet build AgentMesh.Runtime/AgentMesh.Runtime.csproj` succeeds without an entry point.
- [x] 1.2 Add the Runtime-to-Application compile-time project reference and update package metadata for `AgentMesh.Runtime`; verify the generated dependency graph follows Runtime -> Application -> AgentMesh without a source-project cycle.
- [x] 1.3 Stop generating `AgentMesh.Framework.Application` as a NuGet package and configure Runtime packing to place both Runtime and Application assemblies and applicable XML documentation under its target-framework library assets; verify the `.nupkg` contains both assemblies and has no dependency on `AgentMesh.Framework.Application`.
- [x] 1.4 Update local package versions and package-feed consumers needed for the breaking package replacement; verify a clean restore can resolve the new Runtime package from the configured feed.

## 2. Framework Registration And Startup API

- [x] 2.1 Replace `IAgentMeshPluginBootstrap` with an empty public `IAgentMeshPlugin` marker contract available through the Runtime package; verify a plugin marker type compiles without implementing registration members.
- [x] 2.2 Refactor assembly discovery to accept the marker assembly and return only concrete, closed implementations of documented AgentMesh extension contracts; verify inspection of the sample assembly finds its pipelines, parameters, steps, agents, and serializers but excludes unrelated classes.
- [x] 2.3 Implement deterministic service-descriptor registration for each discovered extension category, preserving concrete/interface mappings and established lifetimes while de-duplicating overlapping contracts; verify the built sample provider resolves exactly one chat pipeline, at most one summarization pipeline, and all required step, parameter, agent, and serializer dependencies.
- [x] 2.4 Remove `PluginHostConfiguration` and plugin-directory fallback from Application configuration and prompt resolution; verify prompts resolve from plugin-owned active configuration and output paths without a `PluginsPath` value.
- [x] 2.5 Implement `LoadAgentMesh<TPlugin>(WebApplicationBuilder)` to register Application/infrastructure services, reflected plugin components, API authentication, callbacks, JSON options, Swagger, and Runtime controllers through an explicit MVC application part; verify the method adds descriptors without calling `BuildServiceProvider` or building the application.
- [x] 2.6 Implement `StartAgentMesh(WebApplication, CancellationToken)` to configure authentication, authorization, controllers, Swagger, Swagger UI, and asynchronous host execution; verify Runtime contains no final application-build call.
- [x] 2.7 Remove `PluginBootstrapLoader`, its custom assembly load context, plugin load result handling, unavailable-host fallback, and all dynamic plugin-loading references; verify source search and Runtime build contain no `Plugins/` scanning or runtime assembly loading path.

## 3. Sample Plugin Composition Root

- [x] 3.1 Convert `AgentMesh.DefaultPipelinePlugin` from a class library to an ASP.NET Core web executable and replace its Application package reference with `AgentMesh.Runtime`; verify the project restores without a project reference to Runtime/Application or a package reference to `AgentMesh.Framework.Application`.
- [x] 3.2 Replace the sample bootstrap with an empty marker implementation and remove manual registration of framework-recognized pipelines, parameters, steps, agents, and serializers; verify Runtime reflection discovers the same component set.
- [x] 3.3 Add the sample `Program` with the required order: create builder, load required `appsettings.json`, overlay optional `appsettings.{Environment}.json`, add environment variables, register `CodeModeWorkflowConfiguration`, call `LoadAgentMesh`, call `builder.Build()`, and call `StartAgentMesh`; verify the final build operation exists only in plugin startup and custom configuration resolves in `ChatRequestPipeline`.
- [x] 3.4 Move active base/development settings and the Web launch profile from the former API project into the sample plugin, removing obsolete `PluginHost` configuration; verify launching the `Web` profile applies development overrides and opens Swagger.
- [x] 3.5 Move and adapt the Dockerfile to build, publish, and run the sample plugin executable; verify `docker build` targets the sample project and the resulting image entry point is the plugin assembly rather than Runtime.

## 4. Templates And Documentation

- [x] 4.1 Add `appsettings.json.template` to Runtime with the complete framework/API configuration shape and guidance-compatible base/environment behavior; verify packing includes the template without materializing an active `appsettings.json` in a consuming project.
- [x] 4.2 Add `Dockerfile.template` to Runtime as a consuming-plugin build sample; verify packing includes it without creating or selecting an active `Dockerfile` in a consuming project.
- [x] 4.3 Update the README package, architecture, startup, configuration, plugin-development, Docker, and deployment guidance to describe Runtime, plugin-owned composition, both templates, and base plus environment settings; verify documentation contains no instruction to deploy DLLs into a `Plugins/` folder or reference `AgentMesh.Framework.Application`.
- [x] 4.4 Update remaining solution launch configuration, scripts, Docker commands, package names, namespaces, and architecture references from the executable API model to Runtime plus plugin executables; verify repository search finds old names only in intentional migration/history content.

## 5. Build And Runtime Validation

- [x] 5.1 Build the framework projects, Runtime, CLI, and sample plugin independently and then build the solution; verify all commands succeed without relying on stale `bin` or `obj` outputs.
- [x] 5.2 Pack Runtime, inspect its archive and dependency metadata, then restore and publish the sample plugin against the produced packages; verify Runtime and Application assemblies are present transitively and the retired Application package is absent.
- [x] 5.3 Start the sample plugin in Development and manually verify Swagger, API-key authentication, configuration summary, chat, summarization, asynchronous callbacks, and missing/invalid pipeline behavior preserve their existing HTTP contracts.
- [x] 5.4 Publish or build the sample plugin container and verify it starts from plugin-owned settings without a `Plugins/` directory, dynamic DLL loading, Runtime executable, or API-project deployment asset.