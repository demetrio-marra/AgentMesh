## Context

See proposal.md - Why. `AgentMesh.Api` currently routes chat requests through `StatelessAppInstance` and exposes generic synchronous/asynchronous request DTOs. `ISummarizationPipeline` instead exposes language-and-message initialization plus `SummarizedContent` and `SummarizedContentDatetime`; it has no name-selection contract. The existing API callback path uses scoped request context, five all-or-none callback URLs, and best-effort HTTP POST delivery. The configured architecture-baseline file is not present at its documented path, so this design is grounded in the live contracts, API code, and the archived async-callback design.

## Goals / Non-Goals

**Goals:**
- Add dedicated sync and async summarization API contracts and routes.
- Resolve exactly one summarization pipeline without exposing name selection.
- Preserve request correlation, API-key protection, OpenAPI documentation, and callback lifecycle behavior.
- Keep chat endpoints and `AgentMeshCLI` unchanged.

**Non-Goals:**
- Changing `ISummarizationPipeline` or chat pipeline contracts.
- Adding pipeline discovery, selection, persistence, polling, callback authentication, or delivery guarantees.

## Decisions

### Dedicated API models and routes
Add separate summarization input/output models rather than reusing chat request DTOs. The synchronous and asynchronous routes will be fixed summarization routes, with no `{pipelineName}` segment. Inputs will carry the summarization language and conversation messages; the server supplies the request timestamp. Synchronous output will carry request id, summarized content, and summarized-content timestamp. Async output will carry request id only.

Alternative: reuse `ProcessRequestApiInput`/`ProcessRequestAsyncApiInput`. Rejected because their `Message` and chat callback semantics do not represent summarization and would blur API contracts.

### Single-pipeline resolution
Add summarization execution beside the existing chat execution path. It will enumerate `ISummarizationPipeline` registrations and accept exactly one; zero or multiple registrations become the existing generic routing/configuration error category. No name is accepted from route, query, or body.

Alternative: select the first registration or add a summarization name. Rejected because it hides configuration errors or violates the single-pipeline requirement.

### Dedicated callback contracts with shared delivery mechanics
Define summarization-specific callback URL fields and payload DTOs for start, step-start, step-completed, completed, and error events. Reuse the existing scoped callback context/delivery infrastructure through a summarization-specific context or equivalent typed data, while ensuring completion contains summarized content and its timestamp rather than `WorkflowResult`.

Alternative: send chat `WorkflowResult` payloads. Rejected because the summarization result has a different public shape and must remain independently versionable.

### Background lifecycle and errors
Mirror the established async request behavior: validate callback completeness before execution, resolve the single pipeline synchronously, return `202` with a request id, execute with a detached per-request scope, and send completion or error callbacks best-effort. Routing/configuration failures remain synchronous HTTP errors and do not trigger an error callback.

## Risks / Trade-offs

- [Adding routes and DTOs expands the public API surface] -> Generate Swagger documentation and focused contract tests.
- [Detached execution can be lost on process shutdown] -> Keep the existing best-effort semantics and document persistence/status tracking as out of scope.
- [Multiple registered summarization pipelines may expose existing plugin misconfiguration] -> Fail closed with a generic configuration response instead of silently choosing one.
- [Callback payloads may diverge from chat payloads] -> Keep event names, correlation, ordering, and all-or-none validation aligned while using dedicated result fields.

## Migration Plan

Add the API contracts and endpoints as an additive change. Existing chat routes remain available. Deploy the API with the new DTOs and Swagger schema; callers can adopt summarization routes independently. Rollback consists of deploying the prior API version, with no data migration required.
