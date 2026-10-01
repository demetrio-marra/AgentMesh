# web-chat-frontend Specification

## Purpose

Provide a browser-based AgentMesh chat client that preserves the CLI's direct API boundary while offering real-time progress, safe conversation controls, and a focused responsive interface.

## Requirements

### Requirement: Web frontend SHALL remain an API-only client
The web frontend SHALL submit chat and summarization work through the configured authenticated AgentMesh HTTP API and SHALL NOT load or execute AgentMesh application, pipeline, agent, or infrastructure runtime components. The API base URL, API key, and API key header name SHALL be configurable through server-side application configuration. API credentials SHALL remain on the web server and SHALL NOT be rendered into HTML or exposed to browser code or browser-readable configuration.

#### Scenario: Web frontend starts with API configuration
- **WHEN** the web frontend starts with a valid AgentMesh API base URL, API key, and API key header name
- **THEN** it serves the browser experience and performs authenticated AgentMesh API calls without loading AgentMesh runtime components or exposing the API key to the browser

#### Scenario: MVC page is rendered
- **WHEN** the server renders the chat page from its configured MVC view
- **THEN** the returned HTML and referenced browser assets contain no AgentMesh API key

#### Scenario: API is unavailable
- **WHEN** the web frontend cannot reach the configured AgentMesh API
- **THEN** it reports the failure in the browser without mutating the current conversation

### Requirement: Web frontend SHALL provide a text-only chat experience
The web frontend SHALL accept non-empty text messages only and display user and assistant messages as a chronological conversation. It SHALL render displayed message text as safe Markdown and SHALL NOT offer image, audio, video, or file input. When no operation is active, Enter in the message input SHALL submit a non-empty message; Shift+Enter or Ctrl+Enter SHALL insert a newline without submitting. Each user and completed assistant message SHALL provide a Copy action in its lower-right corner. After a request reaches a terminal success, failure, or cancellation state, the message input SHALL regain focus.

#### Scenario: User submits text
- **WHEN** the user submits a non-empty text message while no request is active
- **THEN** the frontend starts a chat request and displays the message in the conversation

#### Scenario: User submits unsupported or empty input
- **WHEN** the user attempts to submit an empty message or non-text content
- **THEN** the frontend does not start a request or add a conversation message

#### Scenario: Assistant returns plain text or Markdown
- **WHEN** an assistant response is displayed
- **THEN** the frontend passes its text through the Markdown renderer and presents the resulting safe formatted content

#### Scenario: User uses message input keyboard shortcuts
- **WHEN** no operation is active and the user presses Enter in the message input with non-empty text
- **THEN** the frontend submits the message
- **WHEN** the user presses Shift+Enter or Ctrl+Enter in the message input
- **THEN** the frontend inserts a newline without submitting

#### Scenario: User copies a completed message
- **WHEN** the user activates Copy for a user or completed assistant message
- **THEN** the frontend copies that message's source text and keeps the action in the message's lower-right corner

#### Scenario: Request reaches a terminal state
- **WHEN** a submitted request completes, fails, or is cancelled
- **THEN** the frontend returns focus to the message input after updating the transcript

### Requirement: Web frontend SHALL deliver workflow progress in real time
The web frontend SHALL relay received workflow lifecycle events only to the initiating browser session and retain each original event payload as a raw diagnostic chunk. The terminal `workflowCompleted` event SHALL remain available in Raw data and SHALL NOT create or modify a Steps details entry. The chat workspace SHALL NOT show a separate current-step or generic error strip.

For an accepted chat request, the conversation SHALL show one transient assistant-answer placeholder immediately after the latest user message. Before a progress event communicates a step, the placeholder SHALL show italic `Processing`; afterward it SHALL show the current executing-step message in italic text. The placeholder SHALL show a human-readable elapsed duration since its latest state change in smaller text at its lower-right corner, reset that duration whenever its displayed state changes, and use a distinct visual treatment from completed assistant messages. A successful terminal result SHALL replace the placeholder with the normal completed assistant message and remove the elapsed-duration display. A failed request SHALL replace the placeholder with a distinct reddish message reading `An error has occurred` and remove the elapsed-duration display.

Activating Diagnostics SHALL open a viewport-centered, enlarged modal that visually obscures the chat workspace and contains an accessible tablist. The tablist SHALL contain `Summary` first, `Steps details` second, `Raw data` third, and rightmost `Configuration`. `Summary` SHALL be selected when the modal opens. Configuration SHALL show the existing sanitized sandbox and agent summary or its existing non-blocking unavailable message. The configuration rail and its application-header control SHALL not be present. The `Raw data` panel SHALL contain the existing large vertically scrollable, read-only text area, which SHALL show the latest request's raw progress chunks in arrival order without modifying previously received chunks and SHALL use the available tab-panel height. The modal SHALL provide an explicit close control and SHALL remain open when the user clicks its backdrop.

#### Scenario: Request is accepted before progress arrives
- **WHEN** the frontend receives successful acceptance of a submitted chat request and no workflow progress has arrived
- **THEN** it appends one distinct assistant placeholder after the user's message with italic `Processing` and a running elapsed duration

#### Scenario: Workflow reports progress
- **WHEN** the browser receives a progress event for the active chat request
- **THEN** the placeholder shows the current executing-step message in italic text, resets and continues its elapsed duration, and the event remains available in diagnostics

#### Scenario: Workflow completes successfully
- **WHEN** the frontend receives the terminal completed conversation state for an active chat request
- **THEN** it replaces the placeholder with the completed assistant message and removes the placeholder duration

#### Scenario: Workflow fails
- **WHEN** an active chat request reports an error or fails before completion
- **THEN** it replaces the placeholder with the reddish `An error has occurred` message, does not add an error to the persisted conversation, and does not offer a retry action

#### Scenario: User views configuration diagnostics
- **WHEN** the user opens Diagnostics and selects the rightmost `Configuration` tab
- **THEN** the frontend displays the existing sanitized configuration summary or its non-blocking unavailable message without changing the conversation or workflow

#### Scenario: User views raw diagnostics
- **WHEN** the user selects `Raw data` in Diagnostics
- **THEN** the read-only raw-data textarea fills the available modal tab-panel height and scrolls vertically for overflowing content

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

### Requirement: Web frontend SHALL support one cancelable in-flight operation per chat
The web frontend SHALL allow at most one chat or summarization operation to be active for a browser chat session. While an operation is active, the send action SHALL be unavailable and a stop action SHALL be available. Stopping SHALL cancel the associated outbound API stream and preserve the conversation state that existed before the active operation began.

#### Scenario: User stops an active request
- **WHEN** the user activates stop during chat or summarization
- **THEN** the frontend cancels that API stream, stops displaying it as active, and does not apply a late terminal result

#### Scenario: User tries to send concurrently
- **WHEN** the user attempts to submit another message while an operation is active
- **THEN** the frontend rejects the concurrent submission and keeps the original operation active

#### Scenario: Stream fails before successful completion
- **WHEN** an active stream ends unexpectedly or returns a workflow error
- **THEN** the frontend displays an error, clears the active state, and retains the pre-operation conversation context

### Requirement: Web frontend SHALL own isolated conversation context
The web frontend SHALL own conversation context independently of AgentMesh.Runtime and keep messages, token count, and accumulated cost in memory for each browser chat session for the lifetime of the web process. A successful chat completion SHALL commit the submitted user message and returned assistant message together with returned counters. Failed, cancelled, and transient placeholder messages SHALL remain presentation-only and SHALL NOT be included in the context for a later request. Before New chat clears a non-empty conversation, the frontend SHALL present `The conversation will be lost. Continue?`. Before page close, refresh, or navigation with a non-empty conversation, it SHALL use the same text for the browser-provided leave confirmation.

#### Scenario: Failed request is followed by another request
- **WHEN** a chat request fails and the user later submits a new request
- **THEN** the next request contains only the previously committed conversation messages and excludes the failed request and its error display

#### Scenario: Chat completes successfully
- **WHEN** the chat stream returns one successful terminal result
- **THEN** the frontend commits the user and assistant messages, updates token and cost information, and displays the completed conversation

#### Scenario: User starts a new chat
- **WHEN** the user activates new chat with or without an operation in progress
- **THEN** the frontend cancels any active operation and displays an empty conversation with reset counters

#### Scenario: Separate sessions converse
- **WHEN** two browser sessions submit messages
- **THEN** each session sends, receives, clears, and summarizes only its own conversation context

#### Scenario: User discards a non-empty conversation
- **WHEN** the user activates New chat with a non-empty conversation
- **THEN** the frontend asks `The conversation will be lost. Continue?` and clears the conversation only after confirmation
- **WHEN** the user declines the confirmation
- **THEN** the frontend preserves the conversation and its context

#### Scenario: User leaves a non-empty conversation
- **WHEN** the browser page is refreshed or navigated away from with a non-empty conversation
- **THEN** the frontend requests the browser's leave confirmation using `The conversation will be lost. Continue?`
- **WHEN** the user remains on the page after the browser leave prompt
- **THEN** the frontend preserves the conversation and its context

### Requirement: Automatic summarization SHALL replace the displayed context
After a successful chat completion, the web frontend SHALL request streamed summarization when the configured token threshold is exceeded and the conversation contains more messages than the configured preservation count. On successful summarization, it SHALL replace the summarized messages with the returned summary, retain the configured trailing messages, update the context counters, and refresh the entire visible chat from that resulting context before accepting another message.

#### Scenario: Summarization thresholds are exceeded
- **WHEN** a completed chat leaves both automatic summarization conditions true
- **THEN** the frontend starts summarization, applies the returned summary and preserved messages, refreshes the displayed chat to exactly match the rewritten context, and then enables input

#### Scenario: Summarization does not complete
- **WHEN** automatic summarization is canceled or fails
- **THEN** the frontend retains and redisplays the complete post-chat context that existed before summarization began

#### Scenario: Summarization thresholds are not exceeded
- **WHEN** either automatic summarization condition is false after chat completion
- **THEN** the frontend keeps the completed chat context without requesting summarization

### Requirement: Configuration summary SHALL be available without occupying chat space
The web frontend SHALL retrieve the existing sanitized API configuration summary and display the sandbox and configured-agent information in the Diagnostics Configuration tab. The summary SHALL remain available without reserving space beside the chat and SHALL NOT be discarded while the user views other diagnostics tabs.

#### Scenario: Configuration summary loads
- **WHEN** the AgentMesh API returns its configuration summary
- **THEN** the frontend shows the sandbox identity and configured agent details in the configuration area

#### Scenario: User views configuration in Diagnostics
- **WHEN** the user selects the Configuration tab in Diagnostics
- **THEN** the frontend shows the retrieved configuration data without resizing the chat workspace

#### Scenario: Configuration summary cannot load
- **WHEN** the AgentMesh API rejects or cannot serve the configuration summary
- **THEN** the frontend displays a non-blocking configuration error and keeps chat controls available

### Requirement: Web chat layout SHALL remain usable across common viewport sizes
The web frontend SHALL keep conversation history, text input, stop, New chat, and Diagnostics controls usable without incoherent overlap on supported desktop and mobile viewport sizes. The application workspace SHALL fit within the viewport height, and the transcript SHALL scroll independently to its latest content when new conversation content is rendered. Configuration SHALL be available only in Diagnostics and SHALL not reserve viewport space beside the chat. The empty conversation label SHALL read `Conversation is empty`, and accumulated conversation cost SHALL display exactly two decimal places.

#### Scenario: User opens the frontend on a narrow viewport
- **WHEN** the available viewport width requires a compact layout
- **THEN** controls and message content reflow without horizontal page overflow or obscuring the conversation input

#### Scenario: User views a long conversation
- **WHEN** the conversation contains more content than the available workspace height
- **THEN** the transcript scrolls independently to show its latest content while the composer and top-level controls remain usable
- **WHEN** the Configuration diagnostics tab contains more content than its available modal height
- **THEN** its content scrolls without expanding the application beyond the viewport

#### Scenario: User starts with an empty conversation
- **WHEN** no conversation messages are present
- **THEN** the frontend displays `Conversation is empty` and the accumulated cost with exactly two decimal places

### Requirement: Diagnostics SHALL show parameter history by workflow step
The Diagnostics modal SHALL include a keyboard-accessible `Parameters by Step` tab immediately after the `Steps details` tab. The tab SHALL show one row for every parameter present in a step-completed `parametersDiff` for the current request and one column for every started workflow step received for that request. The matrix SHALL be derived from the same session-scoped diagnostic lifecycle events used by existing Diagnostics views, without modifying the retained raw chunks or workflow processing.

Step columns SHALL be ordered by the arrival of their corresponding step-started events. Parameter rows SHALL be ordered by the first matching `parametersDiff` event in which the parameter appears. A cell SHALL display the `newValue` from the matching step-completed `parametersDiff` entry for its row parameter and column step, and SHALL be blank when that step has no matching diff entry. The matrix SHALL scroll horizontally and vertically within the available Diagnostics panel area. Parameter values, including multi-kilobyte text, SHALL remain inspectable without expanding the modal beyond its viewport bounds.

#### Scenario: User selects the parameter-history tab
- **WHEN** the Diagnostics modal is open and the user selects `Parameters by Step`
- **THEN** the tab becomes the selected Diagnostics view and displays a matrix derived from the latest request's received lifecycle events

#### Scenario: Workflow receives parameters across steps
- **WHEN** the browser receives step-started and step-completed events for multiple steps with parameter diffs that first appear at different times
- **THEN** columns appear in step-start arrival order, rows appear in parameter first-diff order, matching cells show each diff entry's `newValue`, and steps without a matching diff entry have blank cells

#### Scenario: Matrix content exceeds the panel bounds
- **WHEN** the parameter-history matrix has more rows, columns, or parameter-value text than fits in the Diagnostics panel
- **THEN** the matrix remains vertically and horizontally scrollable within the panel and the modal remains within the viewport

#### Scenario: A new request starts
- **WHEN** the browser session submits a new chat request after the prior request received diagnostic events
- **THEN** Parameters by Step clears the prior request's matrix and subsequently shows only lifecycle events received for the new request