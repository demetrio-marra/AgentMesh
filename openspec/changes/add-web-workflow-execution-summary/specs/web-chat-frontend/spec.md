## MODIFIED Requirements

### Requirement: Web frontend SHALL deliver workflow progress in real time
The web frontend SHALL consume the existing authenticated chat and summarization streaming endpoints and SHALL relay each received workflow lifecycle event, including the terminal `workflowCompleted` event, to the initiating browser session through SignalR as it arrives. The interface SHALL show current progress without adding progress events to the conversation transcript. For every relayed progress event, the browser session SHALL receive the original event payload as an unmodified raw data chunk in addition to its user-facing progress message. The terminal `workflowCompleted` event SHALL be retained in Raw data but SHALL NOT create or modify a Steps details entry.

The chat top bar SHALL provide a Diagnostics control adjacent to the Configuration control. Activating Diagnostics SHALL open a viewport-centered, enlarged modal that visually obscures the chat workspace and contains an accessible tablist. The tablist SHALL contain `Summary` first, `Steps details` second, and `Raw data` last. `Summary` SHALL be selected when the modal opens. The `Raw data` panel SHALL contain the existing large vertically scrollable, read-only text area, which SHALL show the latest request's raw progress chunks in arrival order without modifying previously received chunks. The modal SHALL provide an explicit close control and SHALL remain open when the user clicks its backdrop.

The Summary panel SHALL present one HTML execution-summary table for the most recently completed chat workflow. It SHALL list each main-pipeline step in execution order with its name, human-readable elapsed duration, and, for each agentic step, input tokens, input-token percentage, input cost, output tokens, output-token percentage, output cost, and total cost. Non-agentic steps SHALL retain their elapsed duration and display unavailable token and cost values distinctly. Input and output percentages SHALL use the total token-priced agentic input and output tokens respectively as their denominators. The table SHALL include a `TOTAL (TOKEN-BASED)` row with aggregate token counts and token-priced costs. When one or more agent executions have hourly pricing, the Summary panel SHALL also present the hourly-priced rows with elapsed duration, hourly rate, per-row total cost, and a `TOTAL (HOURLY-BASED)` row. Beneath the step-consumption grid, the Summary panel SHALL show labels for the total elapsed workflow time in human-readable format and the combined token-priced and hourly-priced total cost. Durations SHALL format as `<1s` below one second, otherwise use nonzero hour, minute, and second units such as `50s` or `1m 45s`.

The Summary panel SHALL use the successful chat workflow's existing terminal step statistics and itemized agent-cost data. It SHALL not infer token counts or costs from raw lifecycle chunks, modify raw events, or expose configuration credentials. Before a successful chat workflow result is available, the panel SHALL clearly indicate that no completed workflow summary is available. Submitting a new chat request SHALL clear the prior summary before its workflow events arrive. The summary data SHALL remain isolated to its initiating browser chat session and loaded-page lifetime. Viewing the summary SHALL NOT interrupt workflow processing, change the conversation, or alter ordinary progress display.

The `Steps details` panel SHALL provide an arrival-ordered step summary and a detail view for the selected step. A user selecting a step SHALL see its name and type (`Agentic` or `Code`), its input parameters as a two-column scrollable name/value grid, and, after completion, its output parameters as an equivalent grid. A completed step SHALL show elapsed time in human-readable form: `<1s` for a duration under one second, otherwise nonzero hour, minute, and second units such as `50s` or `1m 45s`.

A step that has started but has not completed SHALL remain selectable and show its available input parameters only; it SHALL not show output or elapsed time. When workflow processing ends with an error before one or more started steps complete, the detail view SHALL identify the incomplete state as an error and show the streamed error message when available. The interface SHALL retain the latest request's unmodified raw diagnostic events as the sole source of truth for both Raw data and the derived step-detail view.

Diagnostics chunks, derived step details, and the completed-workflow summary SHALL remain isolated to the initiating browser chat session and SHALL exist until its next submitted request or the loaded page ends. Opening, closing, tabbing, or viewing diagnostics SHALL NOT interrupt an active workflow, modify the conversation, or alter the ordinary progress display.

When the browser session submits a new chat request, its raw diagnostic event collection, Summary data, `Steps details` summary, and selected-step detail SHALL reset before the new request's workflow events arrive, so all tabs show only that latest request's diagnostics.

#### Scenario: User opens diagnostics before workflow completion
- **WHEN** the user activates Diagnostics before a chat workflow has completed successfully
- **THEN** the enlarged modal opens with `Summary` selected and clearly indicates that no completed workflow summary is available
- **WHEN** the user activates `Steps details` or `Raw data`
- **THEN** the existing live step inspection and raw diagnostic content remain available

#### Scenario: User opens detailed diagnostics
- **WHEN** the user activates Diagnostics after the browser has received zero or more workflow events
- **THEN** the enlarged modal opens with `Summary` selected while an arrival-ordered step summary and selectable detail view remain available in `Steps details`
- **WHEN** the user activates `Raw data`
- **THEN** the modal displays the latest request's original raw chunks in arrival order in the existing read-only text area

#### Scenario: User views a completed token-priced workflow summary
- **WHEN** a chat workflow completes successfully with token-priced agentic steps
- **THEN** its Summary panel lists the workflow steps in execution order with human-readable elapsed time, token counts, percentages, costs, and the token-priced total row

#### Scenario: Workflow contains non-agentic steps
- **WHEN** a completed chat workflow includes one or more non-agentic steps
- **THEN** each such step remains listed with its elapsed duration and has distinct unavailable token and cost values

#### Scenario: Workflow contains hourly-priced agents
- **WHEN** a completed chat workflow contains one or more hourly-priced agent executions
- **THEN** the Summary panel presents their hourly rate and computed costs in addition to the token-priced section and displays both pricing-type totals and their combined cost

#### Scenario: User views workflow totals
- **WHEN** a chat workflow completes successfully
- **THEN** the Summary panel shows total elapsed workflow time in human-readable format and the combined workflow cost beneath the step-consumption grid

#### Scenario: User views diagnostics during a workflow
- **WHEN** the user activates Diagnostics while the browser chat session has received zero or more workflow progress events
- **THEN** the centered modal opens with the selected Summary tab while Steps details and Raw data remain available
- **WHEN** later progress events arrive while the modal is open
- **THEN** their raw chunks are appended to the end of the Raw data text area without interrupting the active workflow or replacing existing content

#### Scenario: Chat workflow advances
- **WHEN** the AgentMesh API emits workflow-started, step-started, or step-completed events for an active chat request
- **THEN** the initiating browser receives and displays each event before the terminal result, including its corresponding unmodified raw payload chunk

#### Scenario: Chat workflow completes
- **WHEN** the AgentMesh API emits a `workflowCompleted` event for an active chat request
- **THEN** the initiating browser appends its unmodified payload to Raw data before displaying the completed workflow summary
- **THEN** Steps details ignores the terminal event and retains only step lifecycle entries

#### Scenario: Summarization workflow advances
- **WHEN** automatic summarization emits workflow lifecycle events
- **THEN** the initiating browser receives and displays summarization progress until the operation terminates, and its raw payload chunks are appended to the same session diagnostics collection in arrival order

#### Scenario: Workflow step progresses and completes
- **WHEN** the browser receives a step-started event
- **THEN** the step appears in the summary in its arrival order with its inputs available and no completion-only values
- **WHEN** the browser later receives that step's completion event
- **THEN** the selected step shows output values and human-readable elapsed time

#### Scenario: Workflow ends while a step is incomplete
- **WHEN** a workflow error is received after one or more steps started without their completion events
- **THEN** those incomplete steps remain inspectable, indicate that an error occurred, and show the supplied error message when available

#### Scenario: User closes diagnostics
- **WHEN** the user activates the diagnostics modal's explicit close control
- **THEN** the modal closes and the accumulated diagnostics and completed summary remain available for the same request
- **WHEN** the user clicks the modal backdrop
- **THEN** the modal remains open

#### Scenario: User views live and isolated diagnostics
- **WHEN** later progress events arrive while the Diagnostics modal is open
- **THEN** the step summary, selected step details, and Raw data content reflect the new events without interrupting the active workflow or replacing earlier raw chunks
- **WHEN** workflow events or a completed workflow summary are received for one browser session
- **THEN** no other browser session receives those events, derived step details, raw diagnostic chunks, summary data, or observes that session's conversation state

#### Scenario: Another browser session has an active request
- **WHEN** workflow events or a completed workflow summary are received for one browser session
- **THEN** no other browser session receives those events, raw diagnostic chunks, derived step details, summary data, or observes that session's conversation state

#### Scenario: User submits a later request
- **WHEN** the user submits a new chat request after an earlier request has received workflow steps or completed a workflow summary
- **THEN** Raw data clears its earlier chunks and Summary clears its earlier execution data before receiving the new request's events
- **THEN** `Steps details` is derived from the same raw event collection and lists only the new request's steps