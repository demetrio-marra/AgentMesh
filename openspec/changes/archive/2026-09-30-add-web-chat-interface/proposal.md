## Why

AgentMesh currently offers a thin terminal client but no browser-based equivalent for interactive chat. A direct web frontend is needed to provide the same API-backed streaming, cancellation, configuration visibility, and conversation management through an accessible OpenWebUI-like interface without coupling the client to AgentMesh runtime or application projects.

## What Changes

- Add a standalone server-rendered ASP.NET Core MVC project that depends only on web/client infrastructure and calls the existing authenticated AgentMesh HTTP API.
- Provide a responsive chat interface with text-only input, Markdown-rendered messages, a stop control for active requests, and a new-chat control that clears the current conversation.
- Consume the existing chat and summarization SSE endpoints and relay workflow progress to the browser in real time through SignalR.
- Keep conversation messages and counters per browser session behind a C# chat-context interface with an initial in-process implementation, commit chat state only after successful completion, and replace the visible chat when automatic summarization rewrites the context.
- Fetch the sanitized agent configuration summary at startup and show it in a collapsible panel.
- Configure the AgentMesh API base URL, API key, and header name in server-side `appsettings.json`, following the AgentMeshCLI pattern.
- Add deployment configuration and concise documentation for running the web frontend against AgentMesh.Runtime.

## Non-goals

- Adding image, audio, video, or file input.
- Persisting conversations outside the web process or supporting multiple saved chats; the context interface is the extension point for a later persistent service.
- Changing AgentMesh.Runtime endpoints, SSE event contracts, authentication, or pipeline behavior.
- Adding token-by-token model output where the current API only streams workflow lifecycle events and a terminal result.
- Building a SPA; MVC views use only focused browser scripts for SignalR, Markdown rendering, and interactive controls.
- Adding unit tests; validation will use builds and focused manual browser checks.

## Capabilities

### New Capabilities

- `web-chat-frontend`: Browser chat behavior, session context, streamed progress delivery, cancellation, summarization refresh, Markdown rendering, and collapsible configuration display.

### Modified Capabilities

None. The web frontend consumes the existing `api-configuration-summary` and `stream-request-events` contracts without changing their requirements.

## Impact

- Adds a new web frontend project and solution entry, with MVC controllers and Razor views, static browser assets, a SignalR hub, API client/configuration models, a replaceable C# chat-context store, and per-session operation coordination.
- Reuses the existing authenticated configuration, chat-stream, and summarization-stream endpoints; AgentMesh core, application, infrastructure, and runtime projects remain unchanged.
- Adds web-host settings for the AgentMesh API connection and conversation summarization thresholds, plus README guidance for local startup.