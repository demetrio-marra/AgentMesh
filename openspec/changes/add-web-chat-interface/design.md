## Context

See `proposal.md` for motivation and `specs/web-chat-frontend/spec.md` for observable behavior.

`AgentMeshCLI` is already a thin HTTP client with local DTOs. It fetches `GET /api/configuration`, posts chat and summarization requests with `ResponseHeadersRead`, parses SSE workflow events incrementally, commits conversation state only after a terminal success, and triggers summarization from local thresholds. AgentMesh.Runtime owns pipeline execution and remains stateless with respect to conversation history.

The browser cannot safely receive the AgentMesh API key or directly reproduce the CLI's authenticated POST-based SSE flow. A server-side web boundary is therefore required. The repository targets .NET 8 for the CLI and runtime; the web project should use the same baseline and avoid references to AgentMesh framework projects.

The requested frontend is a conventional server-rendered MVC application, not a SPA. Browser JavaScript is limited to realtime SignalR messages, Markdown rendering, and local interaction state that cannot be handled by a page render alone.

## Goals / Non-Goals

**Goals:**
- Preserve the CLI's API-only boundary and state transition semantics in a web host.
- Make every conversation-context read, write, and reset pass through a replaceable C# interface.
- Keep API credentials and upstream request handling server-side.
- Deliver upstream workflow events to only the initiating browser session in real time.
- Keep the MVC views and focused browser scripts small, readable, responsive, and independent of a JavaScript application framework.
- Make cancellation and new-chat behavior deterministic under racing terminal events.

**Non-Goals:**
- Sharing implementation assemblies or DTO packages with AgentMeshCLI or AgentMesh.Runtime.
- Implementing a database, remote context service, distributed session, account, or saved-chat experience in this release.
- Extending the upstream API to stream partial answer tokens.
- Building an attachment pipeline or manual summarize command.
- Adding unit-test projects.

## Decisions

### Add a standalone `AgentMeshWeb` ASP.NET Core MVC host

Create a `Microsoft.NET.Sdk.Web` project targeting `net8.0`, add it to the solution, and give it no project references to `AgentMesh`, `AgentMesh.Application`, `AgentMesh.Runtime`, or infrastructure projects. Define the small API request/response and stream-event models locally, matching the precedent in `AgentMeshCLI`.

Register controllers with views and conventional routing. A chat controller renders the primary Razor view using a view model containing only presentation-safe initial state; shared structure lives in the MVC layout and focused partials are used only where they improve readability. Static CSS and JavaScript live under `wwwroot`, and one SignalR hub supplies realtime updates after the initial server render.

The host owns the configured `HttpClient`. API base URL, API key, API-key header name, and summarization settings are bound from server-side `appsettings.json` using the same `Api` and `ConversationSummarization` shape as AgentMeshCLI, with normal environment-specific files and environment-variable overrides available. The API key is read only by server services and is never included in view models, Razor output, JavaScript configuration, logs, or browser responses.

Alternative: host the UI in AgentMesh.Runtime. Rejected because it couples presentation and per-user chat state to the stateless pipeline host and violates the direct-client boundary.

Alternative: serve a static single-page application or use Blazor/a JavaScript SPA framework. Rejected because conventional MVC provides the requested server-rendered structure with less client-side state and no frontend build pipeline.

### Use SignalR as the browser command and event channel

The rendered page creates a cryptographically random chat-session ID in `sessionStorage`, connects to `ChatHub`, and joins a group derived from that ID. Focused page JavaScript invokes hub methods to initialize state, submit text, stop the active operation, and start a new chat. Submission starts coordinated background work and returns promptly; progress, state replacement, completion, cancellation, and error notifications arrive through the session group without converting the application into a SPA.

The web server consumes AgentMesh SSE streams with `ResponseHeadersRead` and the same strict terminal-event handling as the CLI. Each parsed lifecycle event is forwarded immediately through `IHubContext<ChatHub>`. The existing stream event names and payloads remain unchanged internally, while browser-facing messages can use concise view models.

Alternative: proxy the upstream SSE response directly to the browser. Rejected because authenticated POST SSE is awkward in browser APIs, exposes transport details to the UI, and does not satisfy the SignalR requirement.

### Access caller-owned context only through `IChatContextStore`

Define an `IChatContextStore` C# interface as the sole boundary for loading, conditionally saving, and clearing a chat context by opaque chat ID. A context is an immutable snapshot containing conversation messages, token count, accumulated cost, and a monotonically increasing revision. Conditional saves take the expected revision so a future remote implementation can reject stale updates without changing orchestration behavior.

Register `InMemoryChatContextStore` as the first implementation. It owns the context dictionary and expires idle contexts after a configured sliding interval. Hubs, coordinators, and API clients must not access its dictionary or retain an independent authoritative copy; they use the interface for every context read, successful transition, and new-chat reset. This preserves the web caller's responsibility for context while allowing a later database or persistent service adapter to replace the in-memory implementation through dependency injection.

Keep non-persistable execution details in a separate transient operation registry: the current operation generation, cancellation source, and per-chat synchronization. Only one chat or summarization operation can run per chat ID. Starting an operation captures its generation and context revision. Stop cancels its token. New chat cancels it, increments the generation, and clears context through `IChatContextStore`. Every asynchronous commit verifies the active generation and uses a conditional context save, preventing late or concurrent results from resurrecting canceled or cleared content.

The browser retains only the opaque chat ID in `sessionStorage` and renders context snapshots received from the server. A submitted user message may appear as a transient pending item, but it is saved through `IChatContextStore` only with a successful chat terminal result. On error or cancellation, the coordinator reloads the authoritative snapshot through the interface and sends it to the browser.

Alternative: let the coordinator depend directly on a singleton dictionary. Rejected because storage behavior would leak into orchestration and make a later persistent service require invasive changes.

Alternative: keep authoritative context in browser JavaScript. Rejected because server-owned cancellation, automatic summarization, reconnect recovery, and eventual persistent storage are clearer behind one C# context boundary.

### Treat automatic summarization as a second state transition

After chat success, save the user and assistant messages and returned counters through `IChatContextStore`. If both configured thresholds are exceeded, retain that saved post-chat revision, keep the operation active, and stream a summarization request for the summarizable prefix. On success, conditionally save a replacement containing the returned assistant summary and preserved trailing messages, reset the token estimate consistently with CLI behavior, and send the resulting full context snapshot to the browser. On failure or cancellation, reload the post-chat snapshot rather than rolling back the completed chat.

This makes the full refresh explicit and ensures the rendered transcript exactly matches the context sent with the next request.

### Use local browser libraries for realtime and safe Markdown rendering

Keep pinned local static copies of the Microsoft SignalR JavaScript client, `marked`, and DOMPurify under `wwwroot/lib`, with their versions recorded by the chosen client-library manifest. Render every user, assistant, and summary message through `marked`, then sanitize the generated HTML with DOMPurify before insertion. Do not render progress payloads as Markdown or inject unsanitized HTML.

Alternative: load libraries from a CDN. Rejected to keep deployment self-contained and avoid runtime third-party availability and integrity concerns.

### Render one work-focused responsive MVC view

Render an unframed application shell from the chat Razor view and MVC layout, with a compact top bar, scrollable transcript, restrained progress region, and anchored composer. New chat is a clear command, send and stop occupy the same stable control slot according to operation state, and the composer is text-only. A collapsible configuration rail shows sandbox identity and agent details; on narrow viewports it becomes an overlay/drawer so the transcript and composer retain usable space. The rail's collapsed state is browser-local UI state and does not refetch or discard configuration.

Messages use role distinction and readable Markdown typography without treating every section as a decorative card. Stable layout dimensions and responsive constraints prevent controls, long code blocks, and long words from overlapping or expanding the page horizontally.

## Risks / Trade-offs

- **[Risk] The initial in-memory context implementation loses conversations on restart and does not span replicas.** -> Keep this explicit as the intended first adapter, document single-instance deployment expectations, and preserve `IChatContextStore` as the replacement boundary for a persistent service.
- **[Risk] A leaked session ID could expose that session through the hub.** -> Generate high-entropy IDs in the browser, validate their format, keep them in `sessionStorage`, restrict CORS to the same origin, and never include API credentials in hub payloads.
- **[Risk] Stop, new-chat, or concurrent work can race an upstream terminal event.** -> Validate operation generation immediately before every commit and require the context store's expected revision to match before saving.
- **[Risk] SignalR reconnect can miss transient progress events.** -> Treat progress as ephemeral, resend authoritative conversation and operation state after reconnect, and do not use progress events as state commits.
- **[Risk] Markdown can introduce scriptable HTML.** -> Sanitize all rendered Markdown and keep a restrictive content-security policy compatible with local assets.
- **[Risk] Secrets bound from appsettings can accidentally leak through view models or diagnostics.** -> Keep API configuration types server-internal, pass only sanitized API summary data to views, and avoid logging bound secret values.
- **[Trade-off] Local copies of browser libraries require deliberate upgrades.** -> Pin versions in a manifest and keep the dependency set limited to SignalR, Markdown parsing, and sanitization.
- **[Trade-off] The duplicated transport DTOs can drift from the API.** -> Keep models limited to consumed fields and validate the flow manually against a running AgentMesh.Runtime instance whenever either side changes.

## Migration Plan

1. Add `AgentMeshWeb` to the solution without changing or replacing AgentMeshCLI.
2. Register MVC, SignalR, and `InMemoryChatContextStore` for `IChatContextStore`; configure its lifetime alongside the AgentMesh API and summarization settings through server-side `appsettings.json` or environment overrides.
3. Run AgentMesh.Runtime and AgentMeshWeb as separate processes; expose only the web host to browser users when desired.
4. Verify configuration loading, chat progress, Markdown output, stop/new-chat races, automatic summarization refresh, and desktop/mobile layout against a live runtime.

Rollback consists of stopping/removing the standalone web project; no API, stored data, or existing client migration is required.