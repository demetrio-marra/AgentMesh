## 1. Consolidate Application Runner

- [x] 1.1 Replace the stateful `AppInstance` contract with caller-supplied synchronous chat execution, preserve routing and cost output behavior, and verify `dotnet build AgentMesh.Application/AgentMesh.Application.csproj` succeeds.
- [x] 1.2 Add caller-supplied synchronous summarization execution and single-pipeline validation to `AppInstance`, returning application-layer summary data, and verify zero/one/multiple summarization pipeline resolution paths are covered by focused tests or an executable integration check.
- [x] 1.3 Add asynchronous chat and summarization execution to `AppInstance`, including synchronous routing validation, detached scoped execution, and terminal success/error delivery, and verify accepted async executions return their request IDs before completion while routing errors remain synchronous.

## 2. Move Shared Callback Support

- [x] 2.1 Consolidate chat and summarization callback scope data into application-owned types that can safely distinguish each execution kind, and verify concurrent scopes cannot share request IDs or callback URLs.
- [x] 2.2 Move or expose terminal callback payload dependencies needed by the runner without making `AgentMesh.Application` depend on `AgentMesh.Api`, and verify the application project compiles independently.
- [x] 2.3 Update `CallbackWorkflowProgressNotifier` and related DI registrations to use the unified scoped callback data while preserving no-op behavior without callback URLs, and verify all start/step callbacks retain their existing payload shapes.

## 3. Simplify Hosts And Remove Legacy Runners

- [x] 3.1 Register only the unified stateless `AppInstance` in `AgentMesh.Application` composition; remove `ConversationContext`, stateful conversation APIs, and `StatelessAppInstance`, then verify there are no remaining application references to the removed types.
- [x] 3.2 Update `RequestsController` and API composition to inject the unified runner for all synchronous and asynchronous chat and summarization routes, and verify existing route, validation, authentication, RFC7807, and response DTO behavior remains unchanged.
- [x] 3.3 Remove `SummarizationAppInstance`, its API-local callback context, and their registrations after callers use the unified runner, and verify a workspace search finds no references to either removed type.

## 4. Package And Validate Integration

- [x] 4.1 Increment the `AgentMesh.Framework.Application` package version, pack it into the configured local package feed, update `AgentMesh.Api` to the same version, and verify package restore resolves the new version.
- [x] 4.2 Build `AgentMesh.Application` and `AgentMesh.Api` with `dotnet build`, resolving only regressions introduced by this consolidation.
- [x] 4.3 Exercise synchronous and asynchronous chat and summarization API flows with caller-supplied conversations, including callback completion/failure and pipeline-selection errors, and verify no execution persists conversation or cumulative cost across requests.