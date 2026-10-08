## Why

AgentMesh currently has only a Chat Completions infrastructure adapter, even though the OpenAI Responses API supports reasoning-aware streamed responses. A standalone Responses adapter is needed so the project can adopt that API without changing current runtime composition or breaking callers that consume the existing chat client contract.

## What Changes

- Add an `AgentMesh.Infrastructure.Responses` class library that implements the existing `IChatCompletionsClient` response-generation overloads and returns the existing `ChatClientResponse` shape.
- Construct the Responses client with model, API key, endpoint, temperature, system prompt, and a reasoning-effort setting.
- Use the stable official `OpenAI` .NET SDK Responses API and its streaming response updates rather than a handwritten HTTP client.
- Stream output text into the existing response payload, propagate token usage, and collect reasoning events internally for a later feature without exposing reasoning text now.
- Add focused tests for request translation, streamed text and usage aggregation, reasoning-event capture, cancellation, and empty-response behavior.
- Include the Responses library and focused test project in `AgentMesh.sln` while keeping the library unreferenced by existing runtime projects; do not alter existing Chat Completions registration or runtime wiring.

## Non-goals

- Registering, selecting, or otherwise composing the Responses client into AgentMesh runtime flows.
- Changing `IChatCompletionsClient`, `IOpenAIClientFactory`, or `ChatClientResponse` to expose reasoning text.
- Migrating or modifying the existing Chat Completions adapter.

## Capabilities

### New Capabilities
- `responses-api-infrastructure`: A standalone OpenAI Responses API adapter that preserves the existing chat-client contract while supporting streaming and internally captured reasoning updates.

### Modified Capabilities
- None.

## Impact

- New `AgentMesh.Infrastructure.Responses` project, source files, package reference, and focused tests.
- Reuses contracts and models from `AgentMesh.Contracts` without adding project references from existing solution projects.
- Requires a stable `OpenAI` NuGet dependency version with `OpenAI.Responses.ResponsesClient`, response streaming, and reasoning options support.