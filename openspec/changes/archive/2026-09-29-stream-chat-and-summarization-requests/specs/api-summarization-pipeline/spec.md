## MODIFIED Requirements

### Requirement: CLI SHALL use API summarization for local context reduction
The CLI SHALL use the authenticated streamed summarization endpoint for both explicit and automatic summarization and SHALL apply the returned content to its local conversation only after a successful terminal completion event. The API's existing synchronous and asynchronous summarization endpoints SHALL remain available to other clients.

#### Scenario: Explicit summarization succeeds
- **WHEN** the user requests summarization and the stream completes successfully
- **THEN** the CLI replaces the selected older messages with the returned summary message and preserves the configured number of newer messages

#### Scenario: Summarization fails
- **WHEN** the stream delivers an error event, closes without a completion event, or is canceled
- **THEN** the CLI reports the failure and preserves the pre-summarization conversation