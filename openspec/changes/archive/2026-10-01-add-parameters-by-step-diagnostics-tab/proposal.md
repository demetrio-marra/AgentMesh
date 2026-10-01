## Why

The existing Diagnostics modal shows parameters one step at a time, making it difficult to trace a parameter's value across an entire workflow. A matrix view will let users compare each parameter's received values and gaps over the ordered step history without leaving Diagnostics.

## What Changes

- Add a `Parameters by Step` Diagnostics tab that derives a parameter-by-step matrix from the current request's lifecycle events.
- Display parameters as rows and started workflow steps as columns, ordering both by their first received appearance.
- Show each parameter's `newValue` from the matching step-completed `parametersDiff` in the step cell and leave cells blank when that step did not produce a diff for the parameter.
- Make the matrix independently scrollable vertically and horizontally, including when parameter values are large.
- Preserve the current diagnostics session isolation, reset behavior, raw event data, workflow progress, and existing tabs.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-chat-frontend`: Extend the Diagnostics modal with a session-scoped, lifecycle-event-derived Parameters by Step view.

## Impact

- Affects the Diagnostics markup, tab coordination, derived-event rendering, and modal styling in `AgentMeshWeb`.
- Reuses existing streamed `workflowStepStarted` and `workflowStepCompleted` parameter payloads; no API, runtime, or persistence contract changes are expected.

## Non-goals

- Do not change workflow execution, parameter mutation semantics, or server event payloads.
- Do not add parameter editing, filtering, export, or cross-request history.