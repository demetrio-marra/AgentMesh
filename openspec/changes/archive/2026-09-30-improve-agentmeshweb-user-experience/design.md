## Context

See [proposal.md](proposal.md) for motivation and [the delta spec](specs/web-chat-frontend/spec.md) for behavior. AgentMeshWeb is an MVC page with a browser-side SignalR client. `ChatCoordinator` already starts an operation before returning from `Submit`, then emits `Operation`, forwarded `Progress`, final `State`, and `Error` events. The browser currently creates a pending user message before `Submit` resolves, renders progress and errors outside the transcript, and owns configuration in a left rail.

## Goals / Non-Goals

**Goals:**
- Make accepted-request execution and failures legible at the expected assistant-response position.
- Preserve the existing API-only boundary, session isolation, diagnostic raw events, and context persistence rules.
- Reclaim the chat viewport while retaining configuration visibility in Diagnostics.

**Non-Goals:**
- Change SignalR hub methods, API models, context-store semantics, or workflow event payloads.
- Stream partial model output, add retry controls, or persist transient browser UI state.

## Decisions

### Use a client-only placeholder state keyed to accepted submissions

Create a transient transcript article only after `connection.invoke("Submit", ...)` resolves. Keep its state outside `renderMessages`, including the latest display text, timestamp, and one scheduled counter update. Start as `Processing`, update it when progress arrives, and reset its timestamp at each display-state transition. Remove its timer and replace the placeholder when terminal `State` carries the normal completed conversation or when `Error`/failed operation arrives.

This uses the coordinator's completed submission as the acceptance boundary and avoids a new acknowledgement message or server persistence field. Rendering a pending message before invocation was rejected because a rejected Hub call would visually imply that the pipeline accepted the request.

### Make placeholders presentation-only

Continue using `State` as the source of truth for committed user and assistant messages. On failure, restore or retain the previously received state, then replace only the transient article with the generic reddish error message. A later submission uses only server-held context, so neither the provisional request nor the error can be forwarded as conversation history.

Persisting errors as assistant messages was rejected because it violates the context contract and would expose transport details to later model requests.

### Consolidate configuration into Diagnostics

Render the existing server-provided configuration summary in a `Configuration` tab placed after Raw data in the existing accessible tablist. Generalize the tab-selection and arrow-key navigation logic to include that fourth tab. Remove the rail, toggle, and rail-specific responsive layout; keep the existing non-blocking configuration-error content in the new panel.

Adding a second modal or retaining a collapsible rail was rejected because both preserve the competing workspace controls that this change removes.

### Let modal layout determine Raw data height

Keep the dialog and tab container as constrained grid rows, make every tab panel capable of filling the remaining row, and have the Raw textarea stretch to that panel. This replaces percentage or intrinsic sizing that leaves unused vertical space while retaining overflow inside the textarea.

### Centralize terminal cleanup

Route successful completion, failure, cancellation, and locally rejected submission through shared cleanup that stops the placeholder timer, reconciles the transient article, updates operation controls, and focuses the composer. Keep the browser `beforeunload` listener using the same discard-warning constant as New chat.

## Risks / Trade-offs

- [Out-of-order SignalR messages may leave a stale placeholder] -> Reconcile it on every terminal `State`, `Error`, `Operation` terminal state, New chat, and reconnect initialization; guard timer updates by the active request token.
- [A client timer can continue after the DOM is replaced] -> Store and clear one interval handle whenever placeholder state terminates or the transcript is reset.
- [New tab may weaken keyboard accessibility] -> Extend the existing roving `tabindex`, `aria-selected`, `aria-controls`, and arrow navigation to all four tabs.
- [The generic chat error conceals technical detail] -> Preserve the existing raw diagnostic events and step-detail error display in Diagnostics while keeping the transcript message user-friendly.

## Migration Plan

1. Deploy as a browser-only AgentMeshWeb update with no data migration or API compatibility change.
2. Validate successful, failed, cancelled, and rejected submissions plus responsive diagnostics views.
3. Roll back by restoring the prior static assets and Razor markup; server context and API interactions remain compatible.