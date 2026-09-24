## 1. Simplify Framework Contracts

- [x] 1.1 Remove the routing `Name` member from `IChatRequestPipeline` and the sample `ChatRequestPipeline`, then verify the framework source and refreshed package-based sample build cleanly.
- [x] 1.2 Remove pipeline-name parameters and overloads from `IAppInstance` and `AppInstance`, update synchronous and background callers, and verify the framework project compiles.
- [x] 1.3 Replace named chat-pipeline resolution in `AppInstance` with request-time cardinality handling for zero, one, and multiple registrations; verify the implementation by source inspection and successful framework compilation without adding unit tests.
- [x] 1.4 Remove `PipelineRegistryInitializer`, its hosted service, `PluginHostState`, their application-runtime registrations, and the now-unused name-required/not-found exception factories; verify repository search and framework compilation.

## 2. Enforce One Plugin Per API Deployment

- [x] 2.1 Refactor `PluginBootstrapLoader` to return a host-owned `Loaded`, `Missing`, or `Invalid` result while loading at most one `*Plugin.dll` entry assembly; verify the absent, empty, single-candidate, and multiple-candidate branches by source inspection and API compilation without adding tests.
- [x] 2.2 Add an API-side unavailable `IAppInstance` implementation that maps missing plugins to `No pipelines loaded` and invalid plugin discovery to the generic redeploy error; verify direct service behavior by source inspection and successful API compilation without exposing file or assembly details.
- [x] 2.3 Wire plugin loading and conditional fallback registration in `AgentMesh.Api/Program.cs` so a plugin-provided runner wins and a missing plugin still permits host startup; verify registration flow by source inspection and successful API compilation without adding tests.

## 3. Remove Named API Routing

- [x] 3.1 Delete the synchronous and asynchronous named-pipeline controller actions, remove nullable pipeline-name plumbing from request helpers, and update endpoint comments and response metadata; verify `dotnet build AgentMesh.Api/AgentMesh.Api.csproj` succeeds.
- [x] 3.2 Verify by controller/OpenAPI source inspection and successful API build that only `/api/requests` and `/api/requests/async` remain; no integration-test files are added per repository policy.
- [x] 3.3 Verify by fallback/error-handler source inspection and successful API build that a host without a plugin remains startable and returns request-time generic RFC7807 `503` responses; no integration-test files are added per repository policy.

## 4. Update Plugin and Deployment Documentation

- [x] 4.1 Rewrite README architecture, framework packaging, and plugin-hosting sections to describe one plugin and one chat pipeline per API deployment; verify active source documentation contains no remaining multi-plugin or named-route claims.
- [x] 4.2 Update README endpoint guidance to list only unnamed synchronous and asynchronous chat routes and explain migration from named routes; verify the documented chat routes match the controller endpoint definitions.
- [x] 4.3 Expand the Kubernetes guidance with one workload and Service per pipeline, matching pipeline identity labels/selectors, and upstream same-pipeline routing to the Service; verify the example labels and selector values match by inspection.

## 5. Validate the Complete Change

- [x] 5.1 Run `dotnet build AgentMesh.sln`, resolving only regressions introduced by this change and recording any unrelated pre-existing failures. No automated test suite is added or run because this repository is intentionally kept free of unit-test files.
- [x] 5.2 Run `openspec validate single-pipeline-per-api-deployment --strict` and verify all proposal, design, task, and capability-delta artifacts pass strict validation before implementation is considered complete.