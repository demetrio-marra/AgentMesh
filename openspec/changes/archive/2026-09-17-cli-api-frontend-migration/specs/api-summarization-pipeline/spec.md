## ADDED Requirements

### Requirement: CLI SHALL use API summarization for local context reduction
The CLI SHALL use the authenticated asynchronous summarization endpoint for both explicit and automatic summarization and SHALL apply the returned content to its local conversation only after a successful completion callback.

#### Scenario: Explicit summarization succeeds
- **WHEN** the user requests summarization and the API completes it successfully
- **THEN** the CLI replaces the selected older messages with the returned summary message and preserves the configured number of newer messages

#### Scenario: Summarization fails
- **WHEN** the API posts a summarization error callback
- **THEN** the CLI reports the error and preserves the pre-summarization conversation
