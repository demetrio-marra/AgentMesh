## Context

See `proposal.md` for motivation and `specs/web-chat-frontend/spec.md` for the behavior contract. AgentMeshWeb already receives the API's SSE events in `AgentMeshApiClient`, converts them into `WorkflowProgress`, and has `ChatCoordinator` send those records to the initiating SignalR group. Browser code uses the record only for a short status message. The SSE data line already contains the parameter values, elapsed time, token consumption, and cost information needed for diagnostics.

## Goals / Non-Goals

**Goals:**
- Preserve the raw SSE JSON data for every non-terminal workflow progress event while retaining the existing friendly progress message.
- Relay raw data only through the existing chat-session SignalR group.
- Provide an append-only, accessible modal viewer that is visually consistent with the current AgentMeshWeb layout and usable on desktop and mobile.

**Non-Goals:**
- Change the API stream event names, payloads, authentication, or terminal-result parsing.
- Persist, transform, redact, analyze, or export diagnostic chunks.
- Change conversation state, cancellation, or automatic-summarization behavior.

## Decisions

### Extend the existing progress relay record

`AgentMeshApiClient` will retain the data string read from each `workflowStarted`, `workflowStepStarted`, and `workflowStepCompleted` SSE frame as a new raw-data property on `WorkflowProgress`, alongside the current kind and friendly message. `ChatCoordinator` will continue sending that record through its existing `Progress` SignalR event, so group ownership and delivery remain unchanged.

Alternative considered: create a second SignalR event for diagnostics. Rejected because it introduces ordering coordination between progress and raw diagnostics and duplicates the established session routing path.

### Store chunks only in browser memory

The browser `Progress` handler will append each raw-data property to an in-memory array in arrival order. The diagnostic text area's value will be derived from that array, using a stable separator between chunks and no JSON parsing, prettification, or field filtering. The client does not request historic chunks after SignalR reconnection or page refresh.

Alternative considered: retain diagnostics in `ChatCoordinator` or the context store. Rejected because diagnostics are explicitly page-lifetime inspection data and server-side retention would add lifecycle, privacy, and memory-management concerns.

### Build a dedicated modal over the existing workspace

The Razor view will add the Diagnostics trigger beside Configuration and a modal overlay with a dialog container, title, explicit close icon button, and one read-only textarea. The client will own open/close state. The overlay's click handling will intentionally not treat backdrop clicks as dismissal. CSS will use fixed positioning, a shadowed modal that occupies roughly 80% of the viewport with bounded dimensions, and an overflow-enabled textarea to retain stable layout on narrow screens.

Alternative considered: reuse the configuration rail. Rejected because diagnostics need a large independently scrollable inspection surface and the user requested a viewport-centered popup.

### Keep ordinary progress and workflow behavior independent

The existing status text update will remain in the SignalR handler. Modal visibility and text rendering will not invoke hub methods, alter operation state, block submission, or clear diagnostics when a workflow completes or fails. A page reload starts a fresh diagnostics collection.

## Risks / Trade-offs

- [Large or long-running workflows can grow browser-memory and textarea content] -> Diagnostics are page-lifetime only; append raw chunks without duplicate client-side object representations beyond the needed list and rendered text.
- [Raw payloads can be hard to scan] -> Preserve the requested raw content and separate chunks consistently, without falsely presenting transformed data as raw.
- [SignalR reconnect does not replay prior server events] -> Retain the already received browser list and document the intentional page-session scope.
- [Modal controls can conflict with responsive chat controls] -> Bound modal size to viewport dimensions and verify desktop and narrow viewport layouts.

## Migration Plan

1. Extend the internal web progress record and populate it from existing SSE data without modifying API contracts.
2. Add the SignalR-to-browser diagnostics accumulator and modal markup, styling, and interactions.
3. Build and exercise the web project against streamed chat and summarization events; verify raw chunks include step payload fields and remain session isolated.
4. Roll back by reverting the AgentMeshWeb view, script, style, and internal progress-record changes. No data migration, API rollback, or configuration change is required.