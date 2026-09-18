## ADDED Requirements

### Requirement: Async summarization SHALL use the shared stateless runner
The system SHALL execute accepted asynchronous summarization requests through the same stateless application runner used for asynchronous chat requests. The runner SHALL preserve the submitted language and conversation for the detached execution and SHALL deliver the existing summarization completion or failure callback contract.

#### Scenario: Async summarization executes after acceptance
- **WHEN** an authenticated client submits a valid asynchronous summarization request
- **THEN** the API returns the generated request identifier before the shared runner completes the summarization using the submitted language and conversation

#### Scenario: Async summarization failure is reported
- **WHEN** accepted asynchronous summarization fails while executing in the shared runner and callbacks are configured
- **THEN** the system posts the summarization failure callback for that request and does not post its completion callback
