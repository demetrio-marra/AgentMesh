## Why
AgentMesh.Api currently exposes request-processing endpoints for the chat pipeline, but it has no dedicated REST contract for invoking the single summarization pipeline. API consumers cannot request summarization directly or receive its result and lifecycle callbacks without coupling to chat-request semantics.

## What Changes
- Add dedicated synchronous API endpoints for the single `ISummarizationPipeline` implementation.
- Add dedicated asynchronous API endpoints that return a request identifier immediately and execute summarization in the background.
- Add specialized summarization input/output DTOs for synchronous and asynchronous requests.
- Add specialized async callback input/configuration and callback output payloads for summarization lifecycle events.
- Reuse the existing API callback delivery and error-handling conventions where appropriate, while keeping summarization requests independent from chat pipeline name selection.
- Preserve the existing chat request endpoints and contracts.

## Capabilities
### New Capabilities
- `api-summarization-pipeline`: Direct synchronous and asynchronous REST access to the single summarization pipeline, including dedicated DTOs and async callbacks.

### Modified Capabilities
- `request-access-modes`: Extend REST access modes with summarization endpoints and their API authentication, validation, and response documentation requirements.

## Impact
The primary changes will be in `AgentMesh.Api` controllers, API models, callback handling, and service/application integration needed to execute `ISummarizationPipeline`. The implementation must respect the existing `ISummarizationPipeline.SetParameterInitialValues` contract and return `SummarizedContent` with `SummarizedContentDatetime`. It must not add pipeline name selection for summarization and must not modify `AgentMeshCLI`. OpenAPI documentation and focused API tests will need updates.

## Non-goals
- No changes to `AgentMeshCLI`, interactive mode, or chat pipeline behavior.
- No multiple summarization-pipeline routing or summarization name-selection contract.
- No new persistence, polling/status endpoint, callback delivery guarantees, or callback authentication policy.
