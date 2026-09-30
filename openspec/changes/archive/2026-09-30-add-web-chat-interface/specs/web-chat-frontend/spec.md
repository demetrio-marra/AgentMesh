## Purpose

Provide a browser-based AgentMesh chat client that preserves the CLI's direct API boundary while offering real-time progress, safe conversation controls, and a focused responsive interface.

## ADDED Requirements

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
The web frontend SHALL accept non-empty text messages only, display user and assistant messages as a chronological conversation, and render all displayed message text as Markdown regardless of whether the source text contains Markdown syntax. It SHALL NOT offer image, audio, video, or file input.

#### Scenario: User submits text
- **WHEN** the user submits a non-empty text message while no request is active
- **THEN** the frontend starts a chat request and displays the message in the conversation

#### Scenario: User submits unsupported or empty input
- **WHEN** the user attempts to submit an empty message or non-text content
- **THEN** the frontend does not start a request or add a conversation message

#### Scenario: Assistant returns plain text or Markdown
- **WHEN** an assistant response is displayed
- **THEN** the frontend passes its text through the Markdown renderer and presents the resulting safe formatted content

### Requirement: Web frontend SHALL deliver workflow progress in real time
The web frontend SHALL consume the existing authenticated chat and summarization streaming endpoints and SHALL relay each received workflow lifecycle event to the initiating browser session through SignalR as it arrives. The interface SHALL show current progress without adding progress events to the conversation transcript.

#### Scenario: Chat workflow advances
- **WHEN** the AgentMesh API emits workflow-started, step-started, or step-completed events for an active chat request
- **THEN** the initiating browser receives and displays each event before the terminal result

#### Scenario: Summarization workflow advances
- **WHEN** automatic summarization emits workflow lifecycle events
- **THEN** the initiating browser receives and displays summarization progress until the operation terminates

#### Scenario: Another browser session has an active request
- **WHEN** workflow events are received for one browser session
- **THEN** no other browser session receives those events or observes that session's conversation state

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
The web frontend, as the AgentMesh API caller, SHALL own conversation context independently of AgentMesh.Runtime and SHALL keep messages, token count, and accumulated cost in memory for each browser chat session for the lifetime of the web process. A successful chat completion SHALL commit the submitted user message and returned assistant message together with the returned counters. A new-chat action SHALL cancel any active operation and clear both the visible conversation and its complete context.

#### Scenario: Chat completes successfully
- **WHEN** the chat stream returns one successful terminal result
- **THEN** the frontend commits the user and assistant messages, updates token and cost information, and displays the completed conversation

#### Scenario: User starts a new chat
- **WHEN** the user activates new chat with or without an operation in progress
- **THEN** the frontend cancels any active operation and displays an empty conversation with reset counters

#### Scenario: Separate sessions converse
- **WHEN** two browser sessions submit messages
- **THEN** each session sends, receives, clears, and summarizes only its own conversation context

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
The web frontend SHALL retrieve the existing sanitized API configuration summary and display the sandbox and configured-agent information in an area that the user can expand or collapse. Collapsing the area SHALL increase the space available to the chat and SHALL NOT discard the retrieved configuration.

#### Scenario: Configuration summary loads
- **WHEN** the AgentMesh API returns its configuration summary
- **THEN** the frontend shows the sandbox identity and configured agent details in the configuration area

#### Scenario: User toggles configuration visibility
- **WHEN** the user collapses or expands the configuration area
- **THEN** the chat layout resizes accordingly and the configuration data remains available

#### Scenario: Configuration summary cannot load
- **WHEN** the AgentMesh API rejects or cannot serve the configuration summary
- **THEN** the frontend displays a non-blocking configuration error and keeps chat controls available

### Requirement: Web chat layout SHALL remain usable across common viewport sizes
The web frontend SHALL keep conversation history, progress, text input, stop, new-chat, and configuration-toggle controls usable without incoherent overlap on supported desktop and mobile viewport sizes.

#### Scenario: User opens the frontend on a narrow viewport
- **WHEN** the available viewport width requires a compact layout
- **THEN** controls and message content reflow without horizontal page overflow or obscuring the conversation input