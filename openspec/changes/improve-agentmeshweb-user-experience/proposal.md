## Why

The AgentMeshWeb chat interface needs predictable viewport scrolling, keyboard composition, and clearer conversation-state handling for longer or active chats. These refinements make the browser client easier to operate without changing its API-only architecture or conversation protocol.

## What Changes

- Constrain the chat workspace to the browser viewport, with independently scrollable conversation and configuration regions.
- Keep the transcript pinned to its latest content whenever new or refreshed messages render.
- Support Enter to send a message and Shift+Enter or Ctrl+Enter to insert a newline.
- Format cumulative conversation cost to two decimal places and change the empty-state text to `Conversation is empty`.
- Require confirmation before discarding a non-empty conversation through New chat, page unload, or refresh; continue the requested action only after confirmation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-chat-frontend`: Define viewport scrolling, transcript positioning, keyboard submission, conversation-display formatting, and confirmation behavior before destructive chat actions.

## Impact

- Affected browser presentation and interaction files: `AgentMeshWeb/Views/Chat/Index.cshtml`, `AgentMeshWeb/wwwroot/css/site.css`, and `AgentMeshWeb/wwwroot/js/chat.js`.
- No AgentMesh API endpoints, SignalR hub contract, server-side conversation model, runtime components, or external dependencies change.

## Non-goals

- Persisting conversation history across browser sessions or process restarts.
- Changing message Markdown rendering, cancellation semantics, API authentication, or automatic summarization behavior.