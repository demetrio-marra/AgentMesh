## Context

See `proposal.md` for motivation and `specs/web-chat-frontend/spec.md` for the behavior contract. AgentMeshWeb currently appends each relayed lifecycle payload to `diagnosticsChunks` and displays it as text. Step-start payloads already contain display-safe input parameters; completion payloads contain elapsed time, agentic status, and output diffs.

## Goals / Non-Goals

**Goals:**
- Maintain one request-scoped raw event history that supports both untouched raw inspection and live, structured step inspection.
- Preserve progressive rendering when a workflow is active and show failures without claiming a specific failed step where concurrent execution makes that unknowable.

**Non-Goals:**
- Store diagnostics beyond the page lifetime or expose provider configuration or credentials to the browser.
- Change event ordering, pipeline execution, terminal workflow behavior, or conversation state.

## Decisions

### Preserve request-scoped raw chunks and derive ordered step records in the browser

Keep request-scoped raw diagnostics events as the sole source for Raw data and Step details. On every SignalR progress event, append its existing event wrapper to that collection. Rebuild ordered step records from the raw event collection when rendering: a start event creates an ordered step record; a completion event updates the first matching incomplete record; an error event marks remaining incomplete records with the supplied message. The summary order comes only from start-event arrival order. The first available step becomes selected unless the user has selected another. Clear the raw event collection and selected step before submitting a new request.

The event parser will tolerate unknown or incomplete payload fields and use text-node rendering for parameter names and values. Completion output rows use the payload's changed-parameter name and new value. The stream client will relay `workflowError` as a progress record with its original raw data before raising the existing operation error. This lets the browser retain the error and associate it with unfinished diagnostics without changing the terminal error behavior.

Alternative considered: retain a separately mutable step view model. Rejected because it can diverge from the raw inspection data. Derived step records are rebuilt from canonical raw events rather than reparsing the textarea's presentation text.

### Use accessible tabs and bounded two-panel diagnostics layout

The Razor modal will contain tab buttons with tablist, tab, tabpanel, selected-state, and control relationships. `Steps details` will contain a selectable step summary panel and a detail panel. Parameter grids will use stable two-column layouts inside independently horizontal and vertical scrollable containers. The Raw data panel retains the existing textarea. CSS will enlarge the dialog within viewport bounds, preserve its mobile constraints, and stack or constrain the summary/detail areas at narrow widths.

Alternative considered: show details as expandable summary rows. Rejected because long input and output values need a dedicated, independently scrollable area and the requested interaction explicitly separates summary selection from detail inspection.

### Format display-only values at the UI boundary

The browser will format `TimeSpan` payload strings into `<1s` or nonzero hour, minute, and second units. This keeps raw payload fidelity while presenting elapsed time readably.

## Risks / Trade-offs

- [A workflow error has no failing-step identity, especially with parallel steps] -> Mark unfinished steps as errored without claiming which one failed and show the supplied workflow error message.
- [Large parameter values can overrun the modal] -> Constrain both parameter grids with horizontal and vertical overflow and use safe text rendering.
- [Legacy or malformed payloads may lack optional fields] -> Continue raw capture and render only fields present in the structured view.

## Migration Plan

1. Deploy the workflow-error relay with the revised AgentMeshWeb consumer.
2. Validate chat and summarization runs, including code, agentic, in-progress, failed, and long-parameter steps at desktop and narrow viewport sizes.
3. Roll back by reverting the error relay and AgentMeshWeb assets; no persistence or data migration is required.