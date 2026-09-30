## Why

The Diagnostics modal currently presents workflow progress only as concatenated raw JSON, making it slow to understand what a pipeline step received or produced. Operators need a readable per-step view while retaining the complete raw event stream for investigation.

## What Changes

- Enlarge the Diagnostics modal and add an accessible tablist with `Steps details` selected when the modal opens and `Raw data` last.
- Render the accumulated browser diagnostics data as arrival-ordered workflow steps with a selectable summary and detailed inputs, outputs or errors, and elapsed duration.
- Render incomplete steps from their start events without inventing unavailable completion fields.
- Retain the current append-only raw-data textarea, its unmodified raw payloads, session scope, and existing workflow behavior in the Raw data tab.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-chat-frontend`: Provide a readable, live workflow-step diagnostics tab alongside the retained raw-data inspection view.

## Impact

- Affected code: AgentMeshWeb chat view, browser diagnostics accumulator/rendering, and modal CSS; existing workflow progress streaming as needed to relay workflow errors.
- APIs: no new routes, callback payload fields, SignalR event names, credentials, or dependencies.

## Non-goals

- Persisting, exporting, editing, filtering, or redacting diagnostics.
- Changing pipeline execution, workflow ordering, conversation state, or raw event ordering.
- Showing completion-only values for a step that has not completed.