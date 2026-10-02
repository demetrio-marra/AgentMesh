## Why

Request completion can occur while a user is looking elsewhere, leaving them unaware that the AgentMeshWeb chat is ready for the next action or has failed. The web client needs layered, browser-native notifications that communicate terminal outcomes without changing the conversation or workflow flow.

## What Changes

- Add dismissible in-app toast notifications for successful and failed request terminals.
- Add a startup desktop-notification permission request and desktop notifications for terminal outcomes when the application is not visible.
- Temporarily annotate the browser tab title with the terminal outcome while the application tab is unfocused, restoring the default title when focus returns.
- Preserve the existing terminal UI, conversation persistence, and SignalR event handling while applying notifications only to the relevant active request.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-chat-frontend`: Notify the browser user when chat or automatic summarization requests complete or fail, according to visibility and focus state.

## Non-goals

- Persisting notification history, preferences, or permission choices in server-side state.
- Delivering push notifications after the browser session or page is closed.
- Changing AgentMesh API, streaming, SignalR, conversation, or workflow contracts.

## Impact

- Affects AgentMeshWeb browser assets, primarily the chat lifecycle script, chat layout, and site styling.
- Uses browser Notification, Page Visibility, and document title APIs; desktop delivery remains subject to browser support and user permission.
- No backend API, SignalR event, or external service dependency changes are required.