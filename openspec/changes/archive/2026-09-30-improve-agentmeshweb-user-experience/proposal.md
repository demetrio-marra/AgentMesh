## Why

The current AgentMeshWeb chat separates live workflow status from the conversation, shows a pending user message before the server accepts it, and leaves configuration controls competing with the chat workspace. The interface should communicate accepted-request progress and failures directly where users expect an assistant response, while making diagnostics and configuration easier to inspect.

## What Changes

- Replace the standalone live-progress label with a transient assistant-answer placeholder that appears only after the server accepts a request, shows `Processing` until progress arrives, then shows the active step and elapsed time.
- Convert the placeholder into the completed assistant message on success, or a distinct red error message on failure; keep failed requests out of persisted conversation context and offer no retry control.
- Restore focus to the composer after every terminal request outcome.
- Move the message copy action to the lower-right of each rendered message.
- Move the existing configuration summary into a rightmost `Configuration` tab in Diagnostics and remove the header configuration control and left configuration rail.
- Size the Raw data textarea to occupy the Diagnostics modal's available tab-panel height.
- Use the same discard-warning text for browser leave confirmation as New chat.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-chat-frontend`: Define accepted-request placeholders, chat-only failure rendering, revised diagnostics configuration placement, and related layout and focus behavior.

## Impact

- Affects AgentMeshWeb Razor markup, client-side chat state and SignalR event handling, and responsive CSS.
- Reuses the existing SignalR and HTTP API contract; no AgentMesh API, pipeline, or persistence contract changes are expected.

## Non-goals

- Adding retries, streaming partial assistant text, new diagnostics data, or persistent browser-side chat history.
- Changing the underlying AgentMesh request, workflow, or configuration-summary APIs.