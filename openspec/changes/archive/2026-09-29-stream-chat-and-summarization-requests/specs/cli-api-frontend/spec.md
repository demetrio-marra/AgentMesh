## MODIFIED Requirements

### Requirement: CLI SHALL own conversation state
The CLI SHALL keep the current conversation messages and context counters in memory, serialize the messages into API requests, and update them only after a successful terminal streamed result.

#### Scenario: Completed chat request updates context
- **WHEN** a chat stream delivers a successful terminal workflow result
- **THEN** the CLI appends the submitted user message and returned assistant message to its local conversation and updates its counters

#### Scenario: Dropped request completion arrives
- **WHEN** a completion event arrives for a stream the CLI has already canceled
- **THEN** the CLI ignores it and does not mutate conversation state or print a response

#### Scenario: Stream fails before completion
- **WHEN** a chat stream terminates with an error, closes without a terminal event, or is canceled
- **THEN** the CLI reports the failure and does not mutate conversation state

### Requirement: CLI cancellation SHALL drop local request tracking
When the user cancels an in-progress chat or summarization request, the CLI SHALL cancel the outgoing HTTP request, stop processing its stream, and retain the pre-request conversation state.

#### Scenario: User cancels an active request
- **WHEN** Ctrl+C is pressed during an active stream
- **THEN** the CLI stops waiting for the server, reports cancellation, and does not apply a late terminal result

#### Scenario: API completes a locally canceled request
- **WHEN** a terminal event races with local cancellation
- **THEN** the CLI ignores the result after cancellation and leaves its conversation unchanged

### Requirement: CLI SHALL automatically summarize oversized context
After a successful chat completion, the CLI SHALL request streamed summarization when the local token count exceeds `SummaryTokenThreshold` and the local message count is greater than `NumMessageToPreseve`.

#### Scenario: Both summarization thresholds are exceeded
- **WHEN** a chat completion leaves both configured conditions true
- **THEN** the CLI requests streamed summarization, waits for its successful terminal event, replaces the summarized portion of local context with the returned summary message, and accepts the next prompt only after the update

#### Scenario: A summarization threshold is not exceeded
- **WHEN** either configured condition is false
- **THEN** the CLI does not request automatic summarization

## ADDED Requirements

### Requirement: CLI SHALL consume streamed workflow progress
The CLI SHALL use only the authenticated chat and summarization streaming endpoints for workflow requests, SHALL display each progress event using its existing console progress behavior when it arrives, and SHALL NOT host an HTTP callback listener or send callback URLs.

#### Scenario: Progress arrives during chat
- **WHEN** the chat stream sends workflow-started, step-started, and step-completed events
- **THEN** the CLI displays the progress before the final answer is returned

#### Scenario: Progress arrives during summarization
- **WHEN** either explicit or automatic summarization sends step progress events
- **THEN** the CLI displays those events while waiting for the summary

## REMOVED Requirements

### Requirement: CLI SHALL expose callback URLs for asynchronous work
**Reason**: CLI progress and completion are delivered on the outgoing stream, so a callback listener and publicly reachable URL are unnecessary.
**Migration**: Configure the CLI's API base URL and API key; no callback base URL or inbound listener is required. Other API clients can continue using asynchronous callbacks.