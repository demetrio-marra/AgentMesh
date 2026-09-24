## Context

See proposal.md - Why. Runtime execution is currently divided across `AgentMesh.Application`'s stateful `AppInstance` and `StatelessAppInstance`, plus `AgentMesh.Api`'s `SummarizationAppInstance`. All create a per-execution DI scope, resolve a pipeline, execute it, and translate result data; the chat runners duplicate pipeline routing and execution-cost calculation. The stateful runner additionally owns `ConversationContext`, initializes and mutates it, and tracks cumulative cost. The API is already the caller that supplies chat and summarization conversation data, with asynchronous execution using scoped callback state and best-effort HTTP callbacks. The baseline path named in `openspec/config.yaml` is absent; this design is grounded in the available architecture, API-summarization, and async-callback archived designs plus current source.

## Goals / Non-Goals

**Goals:**
- Make one `AgentMesh.Application` `AppInstance` the only pipeline runner for both pipeline interfaces.
- Keep every execution stateless and scope-owned, including detached asynchronous work.
- Move application-owned shared execution/callback dependencies out of the API runner while preserving existing API contracts.
- Remove all obsolete runner classes and registrations.

**Non-Goals:**
- No server conversation/session persistence or automatic context reduction.
- No route, request/response DTO, callback URL, or callback payload redesign.
- No durable queue, retry policy, or lifecycle tracking for background jobs.

## Decisions

### A single stateless AppInstance owns both execution paths
Replace the current `AppInstance` implementation with the stateless chat behavior and add summarization execution to it. Its synchronous methods accept caller-owned chat message plus optional conversation and caller-owned summarization language plus conversation. It returns application-layer results that let API controllers build their existing specialized output DTOs.

The runner creates and disposes a scope per synchronous execution. It resolves chat pipelines by the existing optional name rules and summarization pipelines only when exactly one is registered. It preserves current `PipelineRoutingException` semantics and uses one shared execution-cost calculation helper. It does not resolve, inject, or mutate `ConversationContext` and exposes no conversation count, token count, cumulative-cost, initialization, or host-side summarization API.

Alternative: retain a stateful CLI-only runner and extract shared helpers. Rejected because it leaves two execution authorities and contradicts the requirement that there be one AppInstance.

### Asynchronous work remains detached but is generalized by pipeline kind
The unified runner accepts the existing callback details and resolves the target pipeline synchronously before returning a generated request ID. It starts detached work using the created scope, executes with `CancellationToken.None`, posts exactly one terminal completion/error callback, and disposes that scope in `finally`. Chat completion retains `WorkflowResult`; summarization completion retains content and timestamp.

The API controller remains responsible for model validation and translating routing errors to its current RFC7807 responses. This preserves current endpoint behavior while moving execution ownership to the application layer.

Alternative: add a queue/hosted worker. Rejected because durable execution and tracking are outside scope.

### Callback state and terminal payload dependencies move to the application package
The unified application runner must configure progress notifications and post terminal callbacks for both kinds of work. Consolidate the duplicate chat and summarization callback contexts into application-owned scoped state, and place any callback contracts required by the runner in an application-accessible package/namespace. Adapt `CallbackWorkflowProgressNotifier` to use that unified context while retaining no-op behavior where callback URLs are absent.

The API project keeps HTTP endpoint DTOs and controller mapping; it no longer contains a pipeline runner or a runner-specific callback context. Because it consumes `AgentMesh.Framework.Application` as a package, release the changed application package and update the API package reference together.

Alternative: keep summarization callback context and runner support in the API project. Rejected because an application runner cannot depend on its host layer.

### Migration preserves HTTP compatibility while breaking direct legacy-runner use
`RequestsController` will receive one `AppInstance` and map existing chat/summarization endpoints to its appropriate methods. Existing routes, API-key auth, validation, response DTOs, callback payloads, request IDs, and pipeline-selection behavior stay unchanged. Direct consumers of `StatelessAppInstance`, `SummarizationAppInstance`, or stateful `AppInstance` members must move to the new stateless methods.

## Risks / Trade-offs

- [Application-layer callback types may be consumed by API clients] -> Keep API endpoint DTOs in the API layer and expose only runner-support contracts needed across the package boundary.
- [Detached tasks are lost during process shutdown] -> Preserve current best-effort semantics; durable processing is a future change.
- [Scope or callback-context reuse could leak request data] -> Configure callback context only in a newly created scope and dispose it only after background execution terminates.
- [Application package/API package versions can drift] -> Build, pack, and reference the same incremented application version during the migration.

## Migration Plan

1. Add the stateless dual-pipeline behavior to `AgentMesh.Application` `AppInstance` and move shared callback support into that project.
2. Update application DI to register only the unified runner and remove `ConversationContext` and obsolete runner registrations.
3. Update API composition and `RequestsController` to use the unified runner; remove API-local runner/context files and registrations.
4. Increment, pack, and publish/copy the application package to the local package feed; update `AgentMesh.Api` to reference that version.
5. Build the affected projects and exercise sync/async chat and summarization success, routing failure, and callback behavior.

Rollback is a deployment rollback to the prior application package and API build; the change has no data migration.