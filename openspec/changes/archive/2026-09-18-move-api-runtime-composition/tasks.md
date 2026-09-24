## 1. Plugin configuration composition

- [x] 1.1 Update plugin bootstrap registration so the API host passes its built `IConfiguration` instance and plugins do not independently resolve configuration.
- [x] 1.2 Update Application-backed pipeline bootstrap registration to consume the host-provided configuration while registering its pipeline.

## 2. API-owned plugin host

- [x] 2.1 Move plugin directory discovery, assembly loading, bootstrap activation, and diagnostics from Application composition into API startup, and verify a configured plugin bootstrap is invoked.
- [x] 2.2 Add the API-owned `PluginBootstrapLoader` and custom assembly load context, reusing Default-context assemblies to preserve shared type identity and prevent duplicate loads while retaining plugin startup error handling.
- [x] 2.3 Remove `AgentMesh.Framework.Application` from the API project dependencies and replace Application imports with API/core-facing equivalents, then verify `dotnet build AgentMesh.Api/AgentMesh.Api.csproj` succeeds.

## 3. Runtime compatibility validation

- [x] 3.1 Verify the API's host configuration is forwarded to a discovered plugin bootstrap.
- [x] 3.2 Verify a plugin dependency already loaded by the host is reused through the custom assembly load context.
- [x] 3.3 Verify API routing, authentication, callbacks, duplicate pipeline-name handling, and plugin deployment assets continue to match their existing contracts without an API dependency on the Application package.