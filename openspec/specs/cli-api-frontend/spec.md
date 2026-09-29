# cli-api-frontend Specification

## Purpose
Provide a thin console frontend that delegates processing to the authenticated API while retaining the interactive conversation state and user-facing progress behavior locally.

## Requirements

### Requirement: CLI processing SHALL use the API over HTTP
The CLI SHALL submit chat and summarization work to the configured API using HTTP and SHALL NOT load or execute AgentMesh application, pipeline, agent, or infrastructure runtime components locally.

#### Scenario: CLI starts with API configuration
- **WHEN** the CLI starts with an API base URL and API key
- **THEN** it uses those values for authenticated API communication and does not require AgentMesh runtime packages

#### Scenario: API request fails before acceptance
- **WHEN** the API is unreachable or rejects a request synchronously
- **THEN** the CLI reports the failure and does not mutate its conversation state

### Requirement: CLI SHALL own conversation state
The CLI SHALL keep the current conversation messages and context counters in memory, serialize the messages into API requests, and update them only after a successful terminal streamed result.

#### Scenario: Completed chat request updates context
- **WHEN** a chat stream delivers a successful terminal workflow result
- **THEN** the CLI appends the submitted user message and returned assistant message to its local conversation

#### Scenario: Dropped request completion arrives
- **WHEN** a callback belongs to a request ID no longer tracked by the CLI
- **THEN** the CLI ignores the callback and does not mutate conversation state or print a response

### Requirement: CLI SHALL consume streamed workflow progress
The CLI SHALL use only the authenticated chat and summarization streaming endpoints for workflow requests, SHALL display each progress event using its existing console progress behavior when it arrives, and SHALL NOT host an HTTP callback listener or send callback URLs.

#### Scenario: Progress arrives during chat
- **WHEN** the chat stream sends workflow-started, step-started, and step-completed events
- **THEN** the CLI displays the progress before the final answer is returned

#### Scenario: Progress arrives during summarization
- **WHEN** either explicit or automatic summarization sends step progress events
- **THEN** the CLI displays those events while waiting for the summary

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
- **THEN** the CLI requests summarization, waits for its completion callback, replaces the summarized portion of local context with the returned summary message, and accepts the next prompt only after the update

#### Scenario: A summarization threshold is not exceeded
- **WHEN** either configured condition is false
- **THEN** the CLI does not request automatic summarization

### Requirement: CLI documentation SHALL identify the terminal frontend boundary
Documentation for the CLI API frontend SHALL identify `AgentMeshCLI` as a REST terminal frontend for `AgentMesh.Api` and SHALL not represent it as a pipeline-execution host.

#### Scenario: Contributor reviews CLI architecture
- **WHEN** a contributor reads the CLI API frontend documentation
- **THEN** they can identify the CLI as an HTTP client and the API as the pipeline runtime host
