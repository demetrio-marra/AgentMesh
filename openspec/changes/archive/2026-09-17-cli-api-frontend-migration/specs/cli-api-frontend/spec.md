## Purpose

Provide a thin console frontend that delegates processing to the authenticated API while retaining the interactive conversation state and user-facing progress behavior locally.

## ADDED Requirements

### Requirement: CLI processing SHALL use the API over HTTP
The CLI SHALL submit chat and summarization work to the configured API using HTTP and SHALL NOT load or execute AgentMesh application, pipeline, agent, or infrastructure runtime components locally.

#### Scenario: CLI starts with API configuration
- **WHEN** the CLI starts with an API base URL and API key
- **THEN** it uses those values for authenticated API communication and does not require AgentMesh runtime packages

#### Scenario: API request fails before acceptance
- **WHEN** the API is unreachable or rejects a request synchronously
- **THEN** the CLI reports the failure and does not mutate its conversation state

### Requirement: CLI SHALL own conversation state
The CLI SHALL keep the current conversation messages and context counters in memory, serialize the messages into API requests, and update them only after successful completion callbacks.

#### Scenario: Completed chat request updates context
- **WHEN** a chat completion callback belongs to an active CLI request
- **THEN** the CLI appends the submitted user message and returned assistant message to its local conversation

#### Scenario: Dropped request completion arrives
- **WHEN** a callback belongs to a request ID no longer tracked by the CLI
- **THEN** the CLI ignores the callback and does not mutate conversation state or print a response

### Requirement: CLI SHALL expose callback URLs for asynchronous work
The CLI SHALL listen for API callback POSTs and SHALL send fully formed callback URLs based on a configured callback base URL. The API SHALL receive those URLs unchanged.

#### Scenario: Default callback base URL
- **WHEN** no callback base URL is configured
- **THEN** the CLI uses its localhost callback base URL

#### Scenario: Progress callback is received
- **WHEN** the API posts a workflow-started, step-started, or step-completed callback for an active request
- **THEN** the CLI invokes the existing console progress notification behavior

### Requirement: CLI cancellation SHALL drop local request tracking
When the user cancels an accepted asynchronous request, the CLI SHALL remove its request ID from local tracking without sending a cancellation request to the API.

#### Scenario: User cancels an active request
- **WHEN** Ctrl+C is pressed after the API request ID has been registered locally
- **THEN** the CLI stops waiting for that request, removes its request ID, and reports the request as canceled

#### Scenario: API completes a locally canceled request
- **WHEN** the API later posts callbacks for a locally canceled request
- **THEN** the CLI silently returns successful callback responses without displaying or applying the result

### Requirement: CLI SHALL automatically summarize oversized context
After a successful chat completion, the CLI SHALL request asynchronous summarization when the local token count exceeds `SummaryTokenThreshold` and the local message count is greater than `NumMessageToPreseve`.

#### Scenario: Both summarization thresholds are exceeded
- **WHEN** a chat completion leaves both configured conditions true
- **THEN** the CLI requests summarization, waits for its completion callback, replaces the summarized portion of local context with the returned summary message, and accepts the next prompt only after the update

#### Scenario: A summarization threshold is not exceeded
- **WHEN** either configured condition is false
- **THEN** the CLI does not request automatic summarization
