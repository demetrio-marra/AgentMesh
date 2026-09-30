## 1. Web Project Foundation

- [x] 1.1 Add a `net8.0` `AgentMeshWeb` ASP.NET Core MVC project to `AgentMesh.sln`, configure controllers with views, conventional routing, static files, and SignalR, and verify `dotnet build AgentMeshWeb/AgentMeshWeb.csproj` succeeds with no AgentMesh project references.
- [x] 1.2 Add validated API, summarization, and in-memory context-lifetime configuration models plus server-side `appsettings.json` and environment-specific settings following the CLI sections; verify the configured API key authenticates server HTTP calls but appears in neither rendered HTML nor browser-served assets.
- [x] 1.3 Add pinned local Microsoft SignalR, `marked`, and DOMPurify browser assets with their manifest/version metadata, and verify the page loads them locally without CDN requests.

## 2. AgentMesh API Transport

- [x] 2.1 Add local request/response models and an authenticated typed HTTP client for `GET /api/configuration`, then verify it deserializes the live runtime's sanitized sandbox and agent summary.
- [x] 2.2 Implement strict incremental SSE parsing for workflow lifecycle, terminal completion, workflow error, malformed data, unexpected EOF, and cancellation, and verify a live chat stream reports progress before its terminal result.
- [x] 2.3 Add chat and summarization stream methods over the shared parser with infinite HTTP timeout and caller cancellation, and verify canceling each method closes its active upstream request without returning a completion.

## 3. Session And Realtime Coordination

- [x] 3.1 Define immutable versioned chat-context snapshots and `IChatContextStore` load, conditional-save, and clear operations, then implement the initial idle-expiring `InMemoryChatContextStore`; verify two chat IDs retain isolated revisions and the implementation can be replaced through DI without changing consumers.
- [x] 3.2 Implement the separate transient operation registry for per-chat synchronization, generation tracking, and cancellation sources, and verify stopping or resetting one chat invalidates its active generation without storing execution details in `IChatContextStore`.
- [x] 3.3 Implement chat orchestration that rejects concurrent submissions, forwards progress, accesses context only through `IChatContextStore`, commits user and assistant messages with the expected revision only on valid terminal success, and reloads authoritative context after failure or cancellation; verify each path against the live stream.
- [x] 3.4 Implement threshold-driven summarization that reads and conditionally replaces context through `IChatContextStore`, preserves trailing messages, and reloads the completed-chat snapshot on failure or cancellation; verify the emitted full-state replacement exactly matches the next request context.
- [x] 3.5 Implement `ChatHub` initialization, chat-group membership, submit, stop, and new-chat commands plus targeted progress/state/error notifications, and verify new chat clears context through `IChatContextStore` while stop and reset invalidate racing late completions only for the initiating chat.
- [x] 3.6 Reload authoritative context through `IChatContextStore` after SignalR reconnect and verify reconnecting does not duplicate messages or expose another chat's progress.

## 4. Browser Chat Experience

- [x] 4.1 Implement the chat MVC controller, presentation-safe view model, shared Razor layout, and single chat view with compact navigation, transcript, progress area, counters, text-only composer, stable send/stop control, new-chat action, and collapsible configuration area; verify a direct page request renders usable HTML with no API credentials.
- [x] 4.2 Add focused page JavaScript for browser session creation, SignalR connection/reconnect, command handling, transient pending messages, full-state replacement, errors, and configuration rendering, and verify controls accurately follow idle, chat, summarizing, canceled, and failed states without client-side routing or SPA state management.
- [x] 4.3 Render every conversation message through `marked` and DOMPurify, apply a compatible restrictive content-security policy, and verify plain text, Markdown tables/code/links, long content, and embedded script markup render safely.
- [x] 4.4 Add the responsive visual design and configuration rail/drawer behavior, and verify desktop and mobile screenshots show no overlapping controls, hidden composer, horizontal page overflow, or text escaping its container.

## 5. Documentation And End-to-End Validation

- [x] 5.1 Document the MVC architecture, server-side `appsettings.json` API credentials, separate Runtime/Web startup commands, API-only boundary, `IChatContextStore` ownership boundary, initial in-memory single-instance limitation, future persistent-adapter path, and text-only scope in `README.md`; verify every documented setting and registered implementation exists.
- [x] 5.2 Build `AgentMesh.sln` and run AgentMesh.Runtime with AgentMeshWeb, then manually verify configuration toggle, multi-turn chat, real-time progress, Markdown output, stop, new chat, session isolation, API failure, and automatic summarization refresh without adding unit tests.