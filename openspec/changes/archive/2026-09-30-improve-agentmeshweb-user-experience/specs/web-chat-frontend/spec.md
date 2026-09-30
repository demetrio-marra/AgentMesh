## MODIFIED Requirements

### Requirement: Web frontend SHALL provide a text-only chat experience
The web frontend SHALL accept non-empty text messages only, display user and assistant messages as a chronological conversation, and render all displayed message text as Markdown regardless of whether the source text contains Markdown syntax. It SHALL NOT offer image, audio, video, or file input. When no operation is active, pressing Enter in the message input SHALL submit a non-empty message; pressing Shift+Enter or Ctrl+Enter SHALL insert a newline without submitting.

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

### Requirement: Web frontend SHALL own isolated conversation context
The web frontend, as the AgentMesh API caller, SHALL own conversation context independently of AgentMesh.Runtime and SHALL keep messages, token count, and accumulated cost in memory for each browser chat session for the lifetime of the web process. A successful chat completion SHALL commit the submitted user message and returned assistant message together with the returned counters. A new-chat action SHALL cancel any active operation and clear both the visible conversation and its complete context. Before a new-chat action clears a non-empty conversation, the frontend SHALL present a confirmation with the text `The conversation will be lost. Continue?` and clear the conversation only when the user confirms. Before the browser page unloads or refreshes with a non-empty conversation, the frontend SHALL request browser-provided leave confirmation using that warning text; if the browser allows the user to remain, the conversation SHALL stay intact.

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

### Requirement: Web chat layout SHALL remain usable across common viewport sizes
The web frontend SHALL keep conversation history, progress, text input, stop, new-chat, and configuration-toggle controls usable without incoherent overlap on supported desktop and mobile viewport sizes. The application workspace SHALL fit within the viewport height, with the conversation transcript and expanded configuration area independently vertically scrollable. When new conversation content is rendered, the transcript SHALL scroll to its latest content. The empty conversation label SHALL read `Conversation is empty`, and accumulated conversation cost SHALL display exactly two decimal places.

#### Scenario: User opens the frontend on a narrow viewport
- **WHEN** the available viewport width requires a compact layout
- **THEN** controls and message content reflow without horizontal page overflow or obscuring the conversation input

#### Scenario: User views a long conversation
- **WHEN** the conversation contains more content than the available workspace height
- **THEN** the transcript scrolls independently to show its latest content while the composer and top-level controls remain usable
- **WHEN** the configuration area is expanded and contains more content than its available height
- **THEN** the configuration area scrolls independently without expanding the application beyond the viewport

#### Scenario: User starts with an empty conversation
- **WHEN** no conversation messages are present
- **THEN** the frontend displays `Conversation is empty` and the accumulated cost with exactly two decimal places
