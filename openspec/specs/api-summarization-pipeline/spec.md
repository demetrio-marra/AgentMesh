# api-summarization-pipeline Specification

## Purpose
Provide authenticated REST clients with a dedicated way to summarize supplied conversation messages through the single registered summarization pipeline, synchronously or in the background with lifecycle callbacks.

## Requirements

### Requirement: Synchronous summarization SHALL return a dedicated result
The API SHALL expose a synchronous summarization endpoint that accepts a summarization language and conversation messages, executes the sole available summarization pipeline through the shared stateless application runner, and returns a specialized response containing a generated request identifier, the summarized content, and the summarization timestamp.

#### Scenario: Synchronous summarization succeeds
- **WHEN** an authenticated client submits a valid language and conversation to the summarization endpoint and exactly one summarization pipeline is available
- **THEN** the API executes that pipeline through the shared stateless application runner and returns the dedicated summarization response with the pipeline's summarized content and `SummarizedContentDatetime`

#### Scenario: Summarization input is invalid
- **WHEN** a client omits the required summarization language or required conversation input
- **THEN** the API returns `400 Bad Request` and does not execute the pipeline

### Requirement: Summarization SHALL not use pipeline name selection
The summarization API SHALL select the sole registered summarization pipeline without accepting a pipeline name route or request field.

#### Scenario: Multiple summarization pipelines are registered
- **WHEN** a summarization request is received and more than one summarization pipeline is available
- **THEN** the API returns a generic service/configuration error and does not select one by name

### Requirement: Asynchronous summarization SHALL return immediately
The API SHALL expose an asynchronous summarization endpoint that accepts the summarization input and a complete set of optional lifecycle callback URLs, returns a generated request identifier without waiting for completion, and executes the summarization in the background.

#### Scenario: Asynchronous summarization is accepted
- **WHEN** an authenticated client submits valid summarization input with zero or all supported callback URLs and exactly one pipeline is available
- **THEN** the API returns `202 Accepted` with the request identifier before summarization completes

#### Scenario: Partial callback configuration is rejected
- **WHEN** an asynchronous summarization request supplies some but not all supported callback URLs
- **THEN** the API returns `400 Bad Request` and does not execute the pipeline

### Requirement: Summarization callbacks SHALL use dedicated payloads
For asynchronous requests with callbacks configured, the API SHALL POST dedicated summarization callback payloads for workflow start, step start, step completion, successful completion, and failure. Each payload SHALL include the originating request identifier; the completion payload SHALL include summarized content and its timestamp, and the failure payload SHALL include an error message.

#### Scenario: Asynchronous summarization completes
- **WHEN** background summarization succeeds with callbacks configured
- **THEN** the API posts the dedicated completion callback and does not post the failure callback

#### Scenario: Asynchronous summarization fails
- **WHEN** background summarization fails with callbacks configured
- **THEN** the API posts the dedicated failure callback and does not post the completion callback

#### Scenario: No callbacks are configured
- **WHEN** asynchronous summarization is accepted without callback URLs
- **THEN** the API performs no callback POSTs for that request

### Requirement: Summarization API SHALL enforce existing API access and documentation conventions
Summarization endpoints SHALL require the configured API key and SHALL document their dedicated request, response, callback, validation, and error contracts in OpenAPI.

#### Scenario: Unauthorized summarization request
- **WHEN** a client calls either summarization endpoint without a valid API key
- **THEN** the API rejects the request with the existing authentication failure response

### Requirement: CLI SHALL use API summarization for local context reduction
The CLI SHALL use the authenticated asynchronous summarization endpoint for both explicit and automatic summarization and SHALL apply the returned content to its local conversation only after a successful completion callback.

#### Scenario: Explicit summarization succeeds
- **WHEN** the user requests summarization and the API completes it successfully
- **THEN** the CLI replaces the selected older messages with the returned summary message and preserves the configured number of newer messages

#### Scenario: Summarization fails
- **WHEN** the API posts a summarization error callback
- **THEN** the CLI reports the error and preserves the pre-summarization conversation
