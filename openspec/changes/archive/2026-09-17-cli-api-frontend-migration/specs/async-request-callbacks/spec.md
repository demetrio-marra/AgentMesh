## ADDED Requirements

### Requirement: Async callbacks SHALL support a stateful console frontend
The existing asynchronous request callback contract SHALL provide enough correlation and result information for a CLI to render progress and update its local conversation without sharing server-side state.

#### Scenario: Active request receives terminal success
- **WHEN** the API posts a workflow-completed callback containing a request ID and workflow result
- **THEN** the CLI can correlate it to the pending request and apply the result to its local state

#### Scenario: Active request receives terminal error
- **WHEN** the API posts a workflow-error callback containing a request ID and error message
- **THEN** the CLI can correlate it to the pending request and report the error without mutating conversation state

#### Scenario: Unknown callback request ID
- **WHEN** the API posts a valid callback for an unknown request ID
- **THEN** the callback receiver accepts it without exposing an error to the API and performs no user-visible action
