## 1. Existing Result Delivery

- [x] 1.1 Extend AgentMeshWeb-only terminal workflow result DTOs with main-pipeline step statistics and itemized agent execution costs already returned by the API; verify a streamed chat response deserializes these fields without changing framework projects.
- [x] 1.2 Relay each successful current chat workflow result to its existing SignalR chat group as diagnostics-only summary data before optional summarization begins; verify another chat session cannot receive that summary and failed or canceled requests emit none.

## 2. Summary Rendering

- [x] 2.1 Add request-scoped browser summary state and clear it through the existing diagnostics reset path; verify Summary is empty for a new, failed, or canceled request and does not retain prior execution data.
- [x] 2.2 Implement CLI-equivalent ordered row construction: pair repeated agent-cost entries by agent-name FIFO order, keep non-agentic rows distinct, partition hourly-priced rows, and calculate token totals, percentages, itemized costs, pricing-type totals, and grand total; verify with token-priced, non-agentic, repeated-agent, unmatched-cost, and hourly-priced fixtures.
- [x] 2.3 Render the completed summary with semantic tables, safe text-node values, human-readable elapsed durations, explicit empty state, fixed USD precision, and labels beneath the step-consumption grid for total elapsed workflow time and total cost; verify `<1s`, seconds, minutes, and hours formatting, zero-denominator percentages, and both aggregate labels.

## 3. Diagnostics Interface

- [x] 3.1 Prepend the accessible Summary tab and panel ahead of Steps details and Raw data, make it the selected and focused tab whenever Diagnostics opens, and update click and keyboard tab navigation for all three tabs; verify tab order, ARIA selected state, and explicit close/backdrop behavior.
- [x] 3.2 Add responsive summary-table styling with bounded vertical and horizontal scrolling while preserving the existing modal, step-details, and raw-data layouts; verify desktop and narrow viewport rendering with long step names and all table columns.

## 4. Integration Validation

- [x] 4.1 Build AgentMeshWeb and exercise a successful token-priced chat workflow, confirming the completed summary, live Steps details, Raw data, and conversation state remain correct through automatic summarization.
- [x] 4.2 Exercise hourly-priced, failed, canceled, new-request reset, and separate-browser-session flows; verify cost sections, empty state behavior, request isolation, and absence of framework-project changes.
- [x] 4.3 Run `openspec validate add-web-workflow-execution-summary --strict` and verify the finalized planning artifacts validate successfully.