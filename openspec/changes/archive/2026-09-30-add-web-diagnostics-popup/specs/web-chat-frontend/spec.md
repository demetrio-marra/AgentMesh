## MODIFIED Requirements

### Requirement: Web frontend SHALL deliver workflow progress in real time
The web frontend SHALL consume the existing authenticated chat and summarization streaming endpoints and SHALL relay each received workflow lifecycle event to the initiating browser session through SignalR as it arrives. The interface SHALL show current progress without adding progress events to the conversation transcript. For every relayed progress event, the browser session SHALL receive the original event payload as an unmodified raw data chunk in addition to its user-facing progress message.

The chat top bar SHALL provide a Diagnostics control adjacent to the Configuration control. Activating Diagnostics SHALL open a viewport-centered modal that visually obscures the chat workspace, occupies approximately 80 percent of the available viewport, and contains one large vertically scrollable, read-only text area. The text area SHALL show the session's raw progress chunks in arrival order, appending each later chunk at the end without replacing or reformatting prior chunks. The modal SHALL provide an explicit close control and SHALL remain open when the user clicks its backdrop.

Diagnostics chunks SHALL remain isolated to the initiating browser chat session and SHALL exist only for the lifetime of the loaded page. Opening, closing, or viewing diagnostics SHALL NOT interrupt an active workflow, modify the conversation, or alter the ordinary progress display.

#### Scenario: Chat workflow advances
- **WHEN** the AgentMesh API emits workflow-started, step-started, or step-completed events for an active chat request
- **THEN** the initiating browser receives and displays each event before the terminal result, and receives the corresponding unmodified raw payload chunk for diagnostics

#### Scenario: Summarization workflow advances
- **WHEN** automatic summarization emits workflow lifecycle events
- **THEN** the initiating browser receives and displays summarization progress until the operation terminates, and its raw payload chunks are appended to the same session diagnostics list in arrival order

#### Scenario: User views diagnostics during a workflow
- **WHEN** the user activates Diagnostics while the browser chat session has received zero or more workflow progress events
- **THEN** a centered, background-obscuring modal opens with one large vertically scrollable read-only text area containing those raw chunks in arrival order
- **WHEN** later progress events arrive while the modal is open
- **THEN** their raw chunks are appended to the end of that text area without interrupting the active workflow or replacing existing content

#### Scenario: User closes diagnostics
- **WHEN** the user activates the diagnostics modal's explicit close control
- **THEN** the modal closes and the accumulated diagnostics remain available for the same loaded browser page
- **WHEN** the user clicks the modal backdrop
- **THEN** the modal remains open

#### Scenario: Another browser session has an active request
- **WHEN** workflow events are received for one browser session
- **THEN** no other browser session receives those events, their raw diagnostic chunks, or observes that session's conversation state