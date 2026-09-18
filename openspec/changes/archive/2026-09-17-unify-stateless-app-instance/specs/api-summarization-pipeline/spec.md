## MODIFIED Requirements

### Requirement: Synchronous summarization SHALL return a dedicated result
The API SHALL expose a synchronous summarization endpoint that accepts a summarization language and conversation messages, executes the sole available summarization pipeline through the shared stateless application runner, and returns a specialized response containing a generated request identifier, the summarized content, and the summarization timestamp.

#### Scenario: Synchronous summarization succeeds
- **WHEN** an authenticated client submits a valid language and conversation to the summarization endpoint and exactly one summarization pipeline is available
- **THEN** the API executes that pipeline through the shared stateless application runner and returns the dedicated summarization response with the pipeline's summarized content and `SummarizedContentDatetime`

#### Scenario: Summarization input is invalid
- **WHEN** a client omits the required summarization language or required conversation input
- **THEN** the API returns `400 Bad Request` and does not execute the pipeline
