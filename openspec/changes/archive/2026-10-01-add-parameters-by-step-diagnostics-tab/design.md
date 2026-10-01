## Context

`AgentMeshWeb` currently stores raw progress events in arrival order, derives its Steps details view from step-started input parameters and step-completed output diffs, and clears that collection for each new request. The Diagnostics dialog has a fixed viewport-constrained layout and its existing tab state is coordinated in the browser.

## Goals / Non-Goals

**Goals:**
- Add a session-scoped parameter-history matrix without changing event transport or backend contracts.
- Preserve received event order as the source for step columns and parameter rows.
- Keep large values inspectable while containing scrolling inside the modal panel.
- Preserve the existing tab keyboard navigation and request reset behavior.

**Non-Goals:**
- Persisting or comparing diagnostics across requests, editing parameter values, or changing the workflow event schema.

## Decisions

### Derive the matrix from step lifecycle events

Build the matrix from the existing diagnostics event collection when it is rendered. Each accepted step-started event produces one column in arrival order. Match its corresponding step-completed event by step name and use each `parametersDiff.newValue` as the value for that step's parameter cell. Build the ordered parameter index as diff entries are first encountered. This keeps the raw events authoritative and avoids duplicate state that could fall out of sync on reset.

Step-started input snapshots remain useful for identifying started steps, but are not the displayed matrix values. A step with no matching completion diff leaves its parameter cells blank, including while the step is still running. This ensures the matrix reports the value produced by the step rather than the value it consumed.

### Render a semantic table in a bounded scroll container

Add the tab, panel, and tab-state handling using the existing Diagnostics patterns. Render the matrix as a table with a parameter-name header column and workflow-step headers, inside an element that owns both horizontal and vertical overflow. Use bounded or wrapping value cells with preserved text formatting so long values remain readable without widening the dialog or page. The table structure provides stable row and column relationships for assistive technology.

### Re-render on diagnostic updates and reset

Render the matrix whenever a raw event is appended and clear it with the current Diagnostics reset path. This retains live visibility for in-progress workflows while leaving incomplete step cells blank, and preserves the existing per-session, latest-request scope without server coordination.

## Risks / Trade-offs

- [Large matrices can make DOM rendering and scrolling heavier] -> The view is scoped to one request and uses simple direct rendering from the already retained event collection; evaluate virtualization only if real diagnostic volumes require it.
- [Repeated step names can make headers ambiguous] -> Keep each event as a distinct ordered column and use its displayed step name, matching the existing step detail ordering.
- [Long unbroken values can force horizontal growth] -> Apply wrapping and size constraints to value cells while retaining two-axis scrolling for genuinely wide matrices.

## Migration Plan

Deploy as an additive browser-only change. Existing clients retain their current Diagnostics behavior until they receive the updated static assets. Rollback consists of reverting the tab, renderer, and associated styles; no data migration or API rollback is needed.

## Open Questions

None.