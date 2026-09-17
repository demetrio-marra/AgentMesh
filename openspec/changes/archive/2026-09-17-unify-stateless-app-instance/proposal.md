## Why

Pipeline execution is split among a stateful `AppInstance`, `StatelessAppInstance`, and API-local `SummarizationAppInstance`. This duplicates lifecycle, scope, callback, routing, and cost behavior while leaving an obsolete framework-owned conversation state. A single stateless runner will give both chat and summarization pipelines one consistent execution boundary and leave conversation ownership with callers.

## What Changes

- Replace the stateful and specialized application runners with one stateless `AppInstance` in `AgentMesh.Application`.
- Make the consolidated runner execute both `IChatRequestPipeline` and `ISummarizationPipeline`, including synchronous and asynchronous API flows.
- **BREAKING** Remove framework-owned conversation initialization, mutation, token accumulation, and cumulative-cost state; each chat or summarization execution receives its conversation input from the calling layer.
- Remove obsolete AppInstance registrations, services, callback context plumbing, and API dependencies after their responsibilities move to the shared runner.
- Preserve existing API routes, dedicated chat/summarization DTOs, authentication, pipeline-selection rules, request identifiers, and callback result contracts.

## Capabilities

### New Capabilities
- `stateless-pipeline-runner`: A unified, stateless application-layer execution service for chat and summarization pipelines.

### Modified Capabilities
- `request-access-modes`: API request execution is performed by the unified `AppInstance`, not `StatelessAppInstance`, and no framework runner retains conversation state.
- `api-summarization-pipeline`: Dedicated summarization routes use the unified application-layer runner rather than an API-local summarization runner.
- `async-request-callbacks`: Asynchronous chat and summarization execution retain callback behavior through the unified runner.
- `default-chat-pipeline-plugin`: Interactive and API integration assumptions no longer require a stateful host-owned conversation runner.

## Impact

- Affected projects: `AgentMesh.Application`, `AgentMesh.Api`, and any remaining composition or caller references to legacy runner types.
- Affected services: runner DI registration, callback contexts/notifier use, scoped pipeline resolution, and chat/summarization execution methods.
- Public API routes and payloads are intended to remain compatible; application-level types and registrations are intentionally consolidated and breaking for direct consumers of the removed runner classes.

## Non-goals

- Changing chat pipeline naming or summarization single-pipeline selection rules.
- Adding persistence, server-side conversation storage, durable background jobs, or request-status polling.
- Redesigning callback URLs, callback payloads, authentication, or plugin contracts beyond what the runner consolidation requires.