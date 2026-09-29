## Why

Synchronous API calls hide workflow progress until completion, while asynchronous callbacks cannot reliably reach desktop clients behind firewalls or NAT. CLI users need progress and a final result on the same outbound HTTP connection.

## What Changes

- Add authenticated `POST /api/requests/stream` and `POST /api/summarize/stream` endpoints that stream workflow lifecycle events as they occur, followed by exactly one terminal result or error event.
- Route chat and summarization through the existing stateless runner and progress hooks, using request-scoped stream delivery without changing plugin-facing pipeline or notifier interfaces.
- Switch AgentMeshCLI chat, explicit summarization, and automatic summarization exclusively to streamed requests; render progress locally and apply conversation changes only after successful terminal events.
- Retain existing synchronous and asynchronous API routes, responses, and callback behavior for other clients.

## Capabilities

### New Capabilities

- `stream-request-events`: Defines the chat/summarization stream endpoints, event envelopes, ordering, authentication, errors, and disconnect behavior.

### Modified Capabilities

- `cli-api-frontend`: Replace callback reception, pending-request tracking, and callback-specific cancellation with outgoing stream consumption for CLI chat and summarization.
- `api-summarization-pipeline`: Update CLI context-reduction requirements to use the streamed summarization endpoint while preserving the synchronous and asynchronous API contracts.

## Impact

- Affects AgentMesh.Runtime controllers and scoped progress delivery, AgentMesh.Application stateless runner integration, existing framework contracts only if an additive extension is necessary, and AgentMeshCLI HTTP transport, console workflow, configuration, and documentation.
- The plugin-facing contracts used by third-party pipeline packages remain compatible. No changes to AMCodePipeline or AMCodePipeline-Helm.

## Non-goals

- Token-by-token model output, replay/resume, persistent request state, or server-side conversation ownership.
- Refactoring or optimizing existing synchronous/asynchronous execution; removing those routes; changing plugin implementation code; adding unit tests.