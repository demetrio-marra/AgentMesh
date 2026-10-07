## Context

See proposal.md and the `web-chat-frontend` delta specification for motivation and user-facing behavior. `AgentMeshWeb/Views/Chat/Index.cshtml` currently supplies the parent Diagnostics modal, while `wwwroot/js/chat-diagnostics.js` derives the Parameters by Step matrix from browser-session lifecycle events. The existing parent modal intentionally remains open when its backdrop is clicked.

## Goals / Non-Goals

**Goals:**
- Reuse the current event-derived parameter and step ordering for both the matrix and selected-parameter view.
- Keep the detail view layered above, and independently dismissible from, the parent Diagnostics modal.
- Retain the current viewport-bound scrolling treatment for long parameter values and many workflow steps.

**Non-Goals:**
- Persisting a selected parameter between requests or page loads.
- Adding keyboard shortcuts, API endpoints, or server-side state for the detail dialog.
- Changing the parent modal's close behavior or tab navigation.

## Decisions

### Render both tables from one derived matrix model

Extract the current lifecycle-event scan into a helper that returns ordered step metadata and parameter rows. Render the regular matrix from all rows and the detail dialog from the same model filtered by the selected parameter name and its non-empty values, retaining the relative order of the populated steps.

This preserves the current event source of truth and live updates while avoiding duplicated event parsing. The complete matrix retains blank-cell semantics; the detail dialog omits steps where the selected parameter has no value. Duplicating the current matrix construction for the dialog was considered and rejected because future changes could make the two views disagree.

### Use a separate nested dialog overlay in the Razor view

Add a hidden parameter-detail dialog as a sibling to the existing Diagnostics modal. Its backdrop receives a close handler only when the click targets the backdrop itself; clicks within the dialog do not close it. The dialog includes an explicit close button and an accessible dialog label.

Embedding detail content inside the parent Diagnostics dialog was considered and rejected because it would make the parent backdrop and focus/stacking behavior harder to distinguish. Replacing the parent modal was rejected because the specification requires the active Diagnostics tab and matrix to remain available after closing detail.

### Keep dialog state local to the diagnostics controller

The controller maintains the selected parameter name and clears it when a new request resets diagnostics. On each diagnostic append, it rerenders the full matrix and, when open, refreshes the filtered row so live events stay consistent.

Deriving the detail view only once at click time was considered and rejected because late workflow events would leave the selected row stale.

### Add focused responsive styling

Add styles for the stacked detail overlay, centered dialog, and scrollable table container using the existing diagnostics colors, dialog dimensions, and responsive constraints. The overlay gets a higher stacking order than the parent Diagnostics modal and stays bounded by the viewport.

## Risks / Trade-offs

- [A long value or many step columns can overflow the detail dialog] -> Reuse the established horizontally and vertically scrollable table wrapper and cap the dialog to viewport dimensions.
- [Live events can arrive while the detail dialog is open] -> Render from the same current event-derived model whenever diagnostics updates.
- [Nested overlays can accidentally close the parent dialog] -> Scope backdrop checks and close handlers to the detail overlay only.
- [Rows represented as plain table cells are not obviously interactive] -> Use semantic interactive row controls or explicit keyboard-accessible activation behavior and corresponding focus styling.

## Migration Plan

1. Deploy the Razor, JavaScript, and CSS changes together as static web application assets.
2. Verify the complete and live diagnostics paths in desktop and narrow viewports.
3. Roll back by reverting the three frontend asset changes; no schema, API, or persisted-data migration is required.