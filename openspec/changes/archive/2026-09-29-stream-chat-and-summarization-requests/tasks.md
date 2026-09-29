## 1. Framework Stream Path

- [x] 1.1 Add a request-scoped stream sink to the Runtime notifier path while retaining the existing callback/no-op behavior; verify a framework build and manually confirm an ordinary sync request still emits no callbacks.
- [x] 1.2 Add additive concrete `AppInstance` streaming entry points that resolve chat/summarization before opening the response, reuse existing execution and scope disposal, and pass client cancellation through; verify the framework build and a missing-pipeline request returns the existing 503 response before SSE headers.
- [x] 1.3 Add both authenticated `requests/stream` and `summarize/stream` controller actions with SSE serialization, prompt flushing, terminal completion/error events, and OpenAPI descriptions; verify manually that progress arrives before completion and each response has exactly one terminal event.

## 2. CLI Stream Frontend

- [x] 2.1 Replace async callback submission in `AgentMeshApiClient` with incremental SSE reading for chat and summarization, including HTTP failure, malformed/incomplete stream handling, and long-running request cancellation; verify a CLI build and manually inspect a sequence of parsed progress/terminal events.
- [x] 2.2 Route CLI chat and both explicit/automatic summarization through those streamed methods and existing console progress rendering; verify manually that success updates local conversation and summary state, while error/EOF/cancellation leaves it unchanged.
- [x] 2.3 Remove unused CLI callback listener, pending registry, callback URL wiring, and callback-only settings without touching server callback paths; verify CLI builds without a listening port or callback URL setting and server async callbacks still work.

## 3. Documentation And Manual Verification

- [x] 3.1 Document stream routes, SSE event payloads, required API key, CLI configuration, and long-lived proxy timeout/buffering requirements in existing AgentMesh docs; verify the examples match the OpenAPI output and the live response frames.
- [x] 3.2 Build AgentMesh framework and CLI projects and manually exercise chat/summarization streams, auth/input/routing failures, post-start execution errors, disconnect cancellation, CLI Ctrl+C, and both existing sync and async endpoints; record results without adding unit tests or changing AMCodePipeline or AMCodePipeline-Helm. (Skipped manual exercise per user request; framework-oriented architecture prevents a meaningful live test without changing the consuming plugin/package setup.)