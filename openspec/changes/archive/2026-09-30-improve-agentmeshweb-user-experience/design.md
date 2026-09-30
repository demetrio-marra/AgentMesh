## Context

See [proposal.md](proposal.md) for motivation and [the specification delta](specs/web-chat-frontend/spec.md) for behavior. The existing MVC chat page uses a grid layout in `site.css` and browser-side state rendering in `chat.js`. The transcript already scrolls after full and pending message rendering, but related rendering paths are not centralized. The page presently uses a minimum viewport height, displays the old empty label, formats cost to four decimals, and invokes New chat without a discard check.

The configured architecture-baseline document is unavailable at the referenced archive path; this design is therefore based on the current AgentMeshWeb sources and the existing `web-chat-frontend` specification.

## Goals / Non-Goals

**Goals:**
- Keep the application shell within the viewport while allowing transcript and configuration details to scroll independently.
- Make keyboard submission, transcript placement, formatting, and discard confirmation consistent across browser interactions.
- Preserve AgentMeshWeb's API-only client boundary and existing SignalR event contract.

**Non-Goals:**
- Introduce server-side persistence, change conversation state ownership, or alter HTTP and SignalR contracts.
- Replace the native browser confirmation mechanism or guarantee custom text where browsers suppress it.

## Decisions

### Use bounded CSS grid regions instead of document-height growth

Set the document and application shell to the viewport height and propagate `min-height: 0` through grid children so overflow is resolved by the transcript and configuration rail. On narrow screens, retain the configuration overlay but size its scrollable region against the viewport below the top bar.

This matches the existing grid and responsive layout without adding a new layout component. Using page-level scrolling would leave the composer and top bar outside the intended fixed workspace.

### Centralize transcript positioning after message rendering

Introduce one browser helper that scrolls the transcript to its scroll height and call it after every code path that adds or replaces visible conversation messages. This preserves the existing pending-message behavior and extends it to state refreshes and summary rewrites.

Using MutationObserver was considered but rejected because rendering is already controlled by a small set of explicit functions; an observer would make user-driven scroll behavior and future cleanup less predictable.

### Handle composing keys with a textarea keydown handler

Intercept Enter only when neither Shift nor Ctrl is held and composition is not active; prevent the textarea's default newline and route through the existing submit operation. Leave Shift+Enter and Ctrl+Enter untouched so the textarea creates a newline, while preserving the existing active-operation and empty-message guards.

Changing the textarea to a single-line input was rejected because multi-line messages remain supported.

### Use one conversation-content predicate for destructive confirmations

Track whether the visible/session conversation has content from state updates and pending rendering. Before `NewChat`, call `window.confirm` with the specified text only when that predicate is true; invoke the hub action only after acceptance. Register `beforeunload` with the same predicate and warning text so refresh and navigation receive the browser's standard leave confirmation.

Native `beforeunload` is chosen over a custom modal because only the former can interrupt browser navigation. Modern browsers may replace or omit custom confirmation text; the New chat confirmation can display the exact requested text, while unload behavior follows browser policy.

### Keep display-only changes in the existing browser assets

Update the empty-state literal in the view/client renderer and format the received cumulative cost using two decimal places. No controller, coordinator, hub, or API changes are needed because the state payload already supplies messages and cumulative cost.

## Risks / Trade-offs

- [Browser-specific unload messaging] -> Register the standard leave handler with the requested text and document that browsers control the rendered prompt.
- [Nested grid overflow can accidentally expand the page] -> Validate fixed-height behavior in desktop and narrow viewport layouts with long transcript and configuration data.
- [Key handling can interfere with IME composition] -> Ignore composition events and retain the submit handler as the shared send path.
- [Pending messages may not be represented in the latest server state] -> Include pending visible content in the discard predicate until state reconciliation occurs.

## Migration Plan

1. Deploy the static view, CSS, and JavaScript updates with the existing AgentMeshWeb application.
2. Verify keyboard, confirmation, scrollbar, and formatting scenarios in supported browsers after deployment.
3. Roll back by restoring the previous static assets; no stored data, API contract, or service migration is involved.