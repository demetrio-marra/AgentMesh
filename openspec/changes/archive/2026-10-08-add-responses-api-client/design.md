## Context

See `proposal.md` for motivation and `specs/responses-api-infrastructure/spec.md` for the behavior contract. The existing Chat Completions adapter implements `IChatCompletionsClient`, merges configured and call-provided system prompts, converts `AgentMessage` roles, consumes streaming updates, and maps text plus three token counters to `ChatClientResponse`. It currently depends on `OpenAI` 2.5.0. The latest stable NuGet release observed during planning is `OpenAI` 2.14.0, whose documented `OpenAI.Responses.ResponsesClient` supports streaming updates and reasoning effort.

The Responses library and isolated test project are included in `AgentMesh.sln`, but the library remains unreferenced by existing runtime projects. `AgentMesh.Infrastructure.Responses` currently has no source project file, and the repository has no established test-project convention.

## Goals / Non-Goals

**Goals:**
- Create an independently buildable .NET 8 Responses adapter that conforms to the current chat-client abstraction.
- Preserve the caller-visible text, token, cancellation, and structured-empty-response behavior of the Chat Completions adapter.
- Make reasoning effort configurable and process streamed reasoning events without publishing them.
- Establish test seams for deterministic validation of streaming translation and aggregation.

**Non-Goals:**
- Update existing application, CLI, web, runtime, or composition projects to consume the new adapter.
- Expand the shared response model or factory contract to return reasoning data.
- Implement tools, structured-output controls, multi-turn response IDs, or a migration from Chat Completions.

## Decisions

### Decision 1: Use the official stable Responses SDK
- **Choice:** Reference stable `OpenAI` 2.14.0 (or the newest stable compatible release at implementation time) and use `ResponsesClient`, `CreateResponseOptions`, and `CreateResponseStreamingAsync`.
- **Rationale:** The documented stable SDK supplies the required custom endpoint, reasoning-effort, and streamed text/reasoning events. It avoids duplicating authentication, serialization, protocol streaming, and retries in an `HttpClient` implementation.
- **Alternatives considered:** Retain `OpenAI` 2.5.0, which predates the verified stable Responses surface; write a raw `HttpClient` adapter, which would create a lower-level transport to maintain and test. Use raw HTTP only if the selected stable SDK cannot compile against .NET 8 or does not provide the required Responses API surface.

### Decision 2: Preserve the interface with an adapter-specific constructor
- **Choice:** Implement `IChatCompletionsClient` with the same two generation methods and `ChatClientResponse` output. Give the concrete Responses client a constructor accepting model, API key, endpoint, temperature, system prompt, and reasoning effort.
- **Rationale:** Existing consumers can accept the new adapter through the established contract when composition is added later, while the new API-specific option remains local to the concrete infrastructure client.
- **Alternatives considered:** Extend `IChatCompletionsClient` or `ChatClientResponse` now, which would introduce a breaking cross-project change before any caller needs reasoning output.

### Decision 3: Translate messages into one streamed Responses request
- **Choice:** Convert string inputs to user messages; combine configured and per-call system prompts according to current behavior; map user and assistant messages to equivalent Responses input items; then consume the async stream until completion.
- **Rationale:** This retains the observable input behavior of `ChatCompletionsClient` while using the native streaming model needed for Responses.
- **Alternatives considered:** Send only flattened user text, which would lose assistant context and prompt semantics; use a non-streaming Responses call, which would not exercise or support the requested streaming path.

### Decision 4: Separate output and reasoning accumulation internally
- **Choice:** Maintain distinct internal accumulators while enumerating updates: one for output-text deltas, one for reasoning items/deltas or summaries available from the SDK, and one for final usage. Return only output text and usage in `ChatClientResponse`.
- **Rationale:** This gives future reasoning support a clear capture point while retaining binary and source compatibility for current callers.
- **Alternatives considered:** Discard reasoning events, which would require reworking streaming later; add reasoning to the shared response DTO now, which violates the required compatibility boundary.

### Decision 5: Test the adapter without runtime composition
- **Choice:** Add an isolated test project and introduce a small internal or injectable seam around the SDK stream so tests can simulate text, reasoning, usage, cancellation, exceptions, and empty output without network calls.
- **Rationale:** The repository has no existing test project, and adapter behavior must be verified before the project is wired into production composition.
- **Alternatives considered:** Integration-only tests, which require API credentials and produce unreliable coverage of stream edge cases; defer tests, which leaves the compatibility claims unverified.

## Risks / Trade-offs

- **[Risk]** The official SDK's exact public types can vary between stable versions. **Mitigation:** Pin a verified stable release, compile the standalone library and tests, and adjust only the adapter mapping while retaining the specified external contract.
- **[Risk]** Responses API models may not accept every Chat Completions role or setting identically. **Mitigation:** Test system/user/assistant translation and document unsupported role behavior if the stable SDK constrains it.
- **[Risk]** Reasoning content may be redacted, summarized, or unavailable for some models. **Mitigation:** Capture only updates exposed by the SDK, keep the accumulator internal, and do not promise reasoning text to callers.
- **[Trade-off]** An unreferenced project is not exercised by the full solution build. **Mitigation:** build and test the new project directly during implementation.

## Migration Plan

1. Create the standalone library and test project, include both in the solution, and avoid adding project references from existing runtime projects or changing registrations.
2. Restore, build, and run the new project's tests directly.
3. Verify existing Chat Completions project build remains unchanged.
4. A later change may add configuration, factory selection, and dependency-injection wiring after the Responses adapter is accepted.

Rollback is removal of the standalone project and its test project; existing runtime behavior is unchanged because no current project references the adapter.

## Open Questions

- The exact constructor parameter type and validation mapping for reasoning effort should follow the stable SDK's supported enum/value set at implementation time; this does not change the required configurability or compatibility surface.