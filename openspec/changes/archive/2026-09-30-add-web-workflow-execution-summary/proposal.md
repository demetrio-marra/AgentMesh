## Why

The Diagnostics modal exposes individual step details and raw lifecycle events, but it does not provide the CLI's concise execution-wide view of elapsed time, token consumption, and model costs. Operators need an at-a-glance summary of a completed workflow without leaving AgentMeshWeb or reconstructing totals from raw JSON.

## What Changes

- Add `Summary` as the first Diagnostics tab and select it whenever the modal opens.
- Display the completed chat workflow's existing step statistics and itemized agent costs in an HTML execution-summary table, including token counts, percentages, input/output costs, totals, human-readable durations, total elapsed time, and total cost.
- Render separate token-priced and hourly-priced sections when the completed workflow contains either pricing type, plus the combined cost total.
- Extend AgentMeshWeb's terminal workflow-result model to retain the already-published statistics and cost data required by the summary.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `web-chat-frontend`: Provide a selected-by-default workflow execution summary in Diagnostics using existing terminal API result data.

## Impact

- Affected code: AgentMeshWeb API result models, chat coordinator state/SignalR state delivery as needed, Diagnostics Razor markup, browser diagnostics rendering, and modal styling.
- APIs: no new routes, callback fields, framework-project changes, or dependencies; the web client will consume existing terminal workflow result fields.

## Non-goals

- Changing AgentMesh, AgentMesh.Application, AgentMesh.Runtime, or infrastructure projects.
- Persisting, exporting, or altering workflow diagnostics or accounting data.
- Replacing the existing Steps details or Raw data tabs, their raw-event source, or request-session isolation.