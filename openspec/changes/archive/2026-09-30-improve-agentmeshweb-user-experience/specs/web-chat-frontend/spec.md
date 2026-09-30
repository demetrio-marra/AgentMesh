## MODIFIED Requirements

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

The Diagnostics modal SHALL provide `Summary`, `Steps details`, `Raw data`, and rightmost `Configuration` tabs, with Summary selected when it opens. Configuration SHALL show the existing sanitized sandbox and agent summary or its existing non-blocking unavailable message. The configuration rail and its application-header control SHALL not be present. Raw data SHALL use the available tab-panel height in the modal for its read-only, vertically scrollable textarea.

#### Scenario: Request is accepted before progress arrives
- **WHEN** the frontend receives successful acceptance of a submitted chat request and no workflow progress has arrived
- **THEN** it appends one distinct assistant placeholder after the user's message with italic `Processing` and a running elapsed duration

#### Scenario: Workflow reports progress
- **WHEN** the browser receives a progress event for the active chat request
- **THEN** the placeholder shows the current executing-step message in italic text, resets and continues its elapsed duration, and the event remains available in diagnostics

#### Scenario: Workflow completes successfully
- **WHEN** the browser receives the terminal completed conversation state for an active chat request
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
- **WHEN** the browser page is refreshed, closed, or navigated away from with a non-empty conversation
- **THEN** the browser leave confirmation uses `The conversation will be lost. Continue?`

### Requirement: Web chat layout SHALL remain usable across common viewport sizes
The web frontend SHALL keep conversation history, text input, stop, New chat, and Diagnostics controls usable without incoherent overlap on supported desktop and mobile viewport sizes. The application workspace SHALL fit within the viewport height, and the transcript SHALL scroll independently to its latest content when new conversation content is rendered. Configuration SHALL be available only in Diagnostics and SHALL not reserve viewport space beside the chat.

#### Scenario: User opens the frontend on a narrow viewport
- **WHEN** the available viewport width requires a compact layout
- **THEN** controls, message content, and Diagnostics tabs reflow without horizontal page overflow or obscuring the composer

#### Scenario: User views a long conversation
- **WHEN** the conversation contains more content than the available workspace height
- **THEN** the transcript scrolls independently to show its latest content while the composer and top-level controls remain usable
- **WHEN** the Configuration diagnostics tab contains more content than its available modal height
- **THEN** its content scrolls without expanding the application beyond the viewport

#### Scenario: User starts with an empty conversation
- **WHEN** no conversation messages are present
- **THEN** the frontend displays `Conversation is empty` and the accumulated cost with exactly two decimal places
