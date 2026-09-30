## Why

The web chat interface currently reduces workflow progress events to short status messages, hiding the parameter changes, timing, token usage, and costs supplied by the pipeline. Developers and operators need an in-session way to inspect the unmodified progress payloads while diagnosing a request without exposing API credentials or changing the pipeline API.

## What Changes

- Add a Diagnostics control beside Configuration in the web chat top bar.
- Collect each workflow lifecycle payload received from the API stream and relay it to the initiating browser session without reducing it to status-only text.
- Display the accumulated payload chunks in an append-only, scrollable raw-text diagnostics modal centered over the chat workspace.
- Require the modal's explicit close control; backdrop clicks do not close it.
- Keep diagnostics local to the browser chat session and preserve the existing progress indicator, conversation behavior, API boundary, and streaming endpoint contract.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-chat-frontend`: Add session-isolated raw workflow-progress diagnostics and its accessible modal interaction requirements.

## Impact

- Affected code: AgentMeshWeb Razor chat view, chat browser script and styles, API-stream client models/parsing, and SignalR progress relay.
- APIs and dependencies: no AgentMesh Runtime API, SSE event schema, SignalR hub route, or new external dependency changes.

## Non-goals

- Persisting diagnostics across page reloads, chat sessions, or processes.
- Filtering, redacting, formatting, exporting, or editing raw chunks.
- Changing pipeline execution, telemetry calculations, authentication, or API-stream event schemas.