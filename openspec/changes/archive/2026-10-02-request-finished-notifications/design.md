## Context

See [proposal.md](proposal.md) for motivation and [the web chat frontend delta](specs/web-chat-frontend/spec.md) for behavior. AgentMeshWeb is a server-rendered MVC application with vanilla ES modules. `wwwroot/js/chat.js` manages one browser chat session, receives `State`, `Operation`, and `Error` events from SignalR, and renders the existing terminal chat state. It currently has no notification library or test project.

The configured architecture baseline path is absent from this checkout, so this design is based on the current AgentMeshWeb implementation and existing OpenSpec requirements. The change remains browser-only: neither the AgentMesh HTTP API nor the SignalR protocol requires a new event.

## Goals / Non-Goals

**Goals:**
- Convert each active chat or automatic-summarization terminal outcome into one consistent browser notification event.
- Provide an accessible, styled toast with a timer that advances only while the page is both visible and focused.
- Use native browser notifications and title changes only in their required visibility contexts.
- Preserve existing terminal rendering, request cancellation, and reconnection behavior.

**Non-Goals:**
- Notification persistence, a notification center, service-worker push, or backend permission storage.
- Retrying requests, changing API contracts, or notifying for cancellation.

## Decisions

### Create a focused client-side notification controller

Add a small ES module responsible for terminal notification presentation, lifetime accounting, visibility/focus listeners, desktop-notification permission, and title restoration. `chat.js` will call it with a normalized `completed` or `failed` outcome only after it has confirmed the outcome belongs to the active operation.

This isolates browser APIs and prevents duplicated `State`, `Error`, and `Operation` handlers from generating duplicate notifications. Extending each existing handler independently was considered, but would make terminal-event de-duplication fragile as chat and summarization paths evolve.

### Treat a successful committed state and a failed operation as terminal sources

The chat controller will notify completion when the current pending request is committed by a terminal state update, and notify failure when the active operation reaches its failed path. It will clear or mark the active operation terminal before presenting a notification, ensuring late SignalR messages, reconnection cleanup, and cancellation do not emit additional terminal notices. Automatic summarization follows the same existing operation-state mechanism.

Inferring success from raw workflow diagnostics was rejected because diagnostics deliberately preserve lifecycle data and do not own conversation terminal state.

### Render the in-app toast in the page shell

Add a stable, accessible toast region to the chat page and styles for fixed upper-right placement, success/error treatments, and an icon-only accessible close control. The notification controller owns at most the currently presented terminal toast and removes it on explicit close or after 10 seconds of accumulated visible and focused time. It pauses its interval whenever `document.visibilityState` is not `visible` or the window lacks focus, then resumes without resetting elapsed time.

Using a blind 10-second `setTimeout` was rejected because it would dismiss a notification while the user could not see it.

### Gate desktop and title notifications by browser state

At startup, the controller requests native notification permission only when the Notification API exists and permission is undecided. For a terminal event, it creates a native notification only when permission is granted and the document is not visible. For title changes, it snapshots the default title and only appends the terminal suffix when the document is hidden while the browser window remains focused, which distinguishes another selected tab from a minimized/unfocused window. A `focus` or visible-state return restores the default title.

The native Notification API is preferred over a new dependency because the requested behavior maps directly to browser capabilities. Showing native notifications for visible pages was rejected because the in-app toast already serves the user and avoids redundant alerts.

### Validate with browser-focused tests or repeatable manual checks

Because AgentMeshWeb has no existing client test harness, implementation will add focused automated coverage only if a lightweight, established browser test setup can be introduced without disproportionate infrastructure. At minimum, build verification plus repeatable manual checks will cover completion/failure, visibility transitions, permission states, timer pausing, and title restoration.

## Risks / Trade-offs

- [Browsers can reject permission prompts outside a user gesture] -> Request on startup as required, handle promise rejection, and keep the in-app toast independent of permission.
- [Focus and visibility events vary across browsers] -> Require both signals for timer advancement and title changes, and verify the defined focused-tab, hidden-tab, and minimized-window cases manually.
- [SignalR can deliver terminal-related state through multiple events] -> Normalize outcomes at the active-operation boundary and clear terminal eligibility before display.
- [Native notifications cannot be reliably asserted in ordinary unit tests] -> Encapsulate browser API calls and validate with manual browser scenarios where automated assertions are unavailable.

## Migration Plan

1. Deploy the browser asset and MVC markup/style changes with the existing SignalR protocol unchanged.
2. Verify terminal outcomes in a supported browser with permission granted, denied, and unavailable.
3. Roll back by deploying the prior AgentMeshWeb assets; no data migration, configuration rollback, or API compatibility action is needed.