## MODIFIED Requirements

### Requirement: Web frontend SHALL deliver workflow progress in real time
The web frontend SHALL consume the existing authenticated chat and summarization streaming endpoints and SHALL relay each received workflow lifecycle event to the initiating browser session through SignalR as it arrives. The interface SHALL show current progress without adding progress events to the conversation transcript. For every relayed progress event, the browser session SHALL receive the original event payload as an unmodified raw data chunk in addition to its user-facing progress message.

The chat top bar SHALL provide a Diagnostics control adjacent to the Configuration control. Activating Diagnostics SHALL open a viewport-centered, enlarged modal that visually obscures the chat workspace and contains an accessible tablist. The tablist SHALL contain `Steps details` first and selected when the modal opens, and `Raw data` last. The `Raw data` panel SHALL contain the existing large vertically scrollable, read-only text area, which SHALL show the latest request's raw progress chunks in arrival order without modifying previously received chunks. The modal SHALL provide an explicit close control and SHALL remain open when the user clicks its backdrop.

The `Steps details` panel SHALL provide an arrival-ordered step summary and a detail view for the selected step. A user selecting a step SHALL see its name and type (`Agentic` or `Code`), its input parameters as a two-column scrollable name/value grid, and, after completion, its output parameters as an equivalent grid. A completed step SHALL show elapsed time in human-readable form: `<1s` for a duration under one second, otherwise nonzero hour, minute, and second units such as `50s` or `1m 45s`.

A step that has started but has not completed SHALL remain selectable and show its available input parameters only; it SHALL not show output or elapsed time. When workflow processing ends with an error before one or more started steps complete, the detail view SHALL identify the incomplete state as an error and show the streamed error message when available. The interface SHALL retain the latest request's unmodified raw diagnostic events as the sole source of truth for both Raw data and the derived step-detail view.

Diagnostics chunks and derived step details SHALL remain isolated to the initiating browser chat session and SHALL exist until its next submitted request or the loaded page ends. Opening, closing, tabbing, or viewing diagnostics SHALL NOT interrupt an active workflow, modify the conversation, or alter the ordinary progress display.

When the browser session submits a new chat request, its raw diagnostic event collection, `Steps details` summary, and selected-step detail SHALL reset before the new request's workflow events arrive, so both tabs show only that latest request's diagnostics.

#### Scenario: User opens detailed diagnostics
- **WHEN** the user activates Diagnostics after the browser has received zero or more workflow events
- **THEN** the enlarged modal opens with `Steps details` selected, an arrival-ordered step summary, and a detail view for a selectable step
- **WHEN** the user activates `Raw data`
- **THEN** the modal displays the latest request's original raw chunks in arrival order in the existing read-only text area

#### Scenario: Chat workflow advances
- **WHEN** the AgentMesh API emits workflow-started, step-started, or step-completed events for an active chat request
- **THEN** the initiating browser receives and displays each event before the terminal result, including its corresponding unmodified raw payload chunk

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

#### Scenario: User views diagnostics during a workflow
- **WHEN** the user activates Diagnostics while the browser chat session has received zero or more workflow progress events
- **THEN** the centered modal opens with the selected Steps details tab and Raw data remains available in its tab
- **WHEN** later progress events arrive while the modal is open
- **THEN** their raw chunks are appended without interrupting the active workflow or replacing existing content

#### Scenario: User closes diagnostics
- **WHEN** the user activates the diagnostics modal's explicit close control
- **THEN** the modal closes and the accumulated diagnostics remain available for the same loaded browser page
- **WHEN** the user clicks the modal backdrop
- **THEN** the modal remains open

#### Scenario: User views live and isolated diagnostics
- **WHEN** later progress events arrive while the Diagnostics modal is open
- **THEN** the step summary, selected step details, and Raw data content reflect the new events without interrupting the active workflow or replacing earlier raw chunks
- **WHEN** workflow events are received for one browser session
- **THEN** no other browser session receives those events, derived step details, raw diagnostic chunks, or observes that session's conversation state

#### Scenario: User submits a later request
- **WHEN** the user submits a new chat request after an earlier request has received workflow steps
- **THEN** Raw data clears its earlier chunks before receiving the new request's events
- **THEN** `Steps details` is derived from the same raw event collection and lists only the new request's steps

#### Scenario: Another browser session has an active request
- **WHEN** workflow events are received for one browser session
- **THEN** no other browser session receives those events, their raw diagnostic chunks, derived step details, or observes that session's conversation state