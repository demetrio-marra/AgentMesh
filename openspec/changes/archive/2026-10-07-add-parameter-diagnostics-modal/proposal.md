## Why

The Parameters by Step matrix becomes difficult to inspect when a workflow has many parameters or columns. Users need a focused view of one parameter's values across workflow steps without losing the current diagnostics context.

## What Changes

- Make each parameter row in the Diagnostics `Parameters by Step` matrix selectable.
- Add a nested, viewport-centered parameter-detail dialog that shows the same matrix structure filtered to the selected parameter.
- Provide an explicit close control and support closing the detail dialog by clicking its backdrop.
- Keep the parent Diagnostics modal open and retain the existing session-scoped diagnostic events and matrix behavior.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `web-chat-frontend`: Extend parameter-history diagnostics with a selectable, filtered parameter-detail dialog.

## Non-goals

- Changing workflow event payloads, API endpoints, or server-side diagnostics collection.
- Altering the full Parameters by Step matrix ordering, values, or scrolling behavior.
- Changing the close-on-backdrop behavior of the parent Diagnostics modal.

## Impact

- Affects the Chat Razor view, diagnostics browser controller, and diagnostics styling in `AgentMeshWeb`.
- Adds no backend dependencies, API changes, or data persistence.