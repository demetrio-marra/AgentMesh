# stream-request-events Specification

## Purpose
Allow outbound-only HTTP clients to observe chat and summarization workflow progress and receive the final result over one authenticated, long-lived request.

## Requirements

### Requirement: Chat requests SHALL stream workflow events
The API SHALL accept the existing synchronous chat input at `POST /api/requests/stream` without callback URLs, execute the deployment's sole chat pipeline with caller-supplied conversation, and return a `text/event-stream` response that remains open until a terminal event or client disconnect. It SHALL emit workflow-started, step-started, and step-completed events as the corresponding workflow notifications occur, without buffering them until pipeline completion.

#### Scenario: Progress arrives before chat completes
- **WHEN** an authenticated client posts a valid chat message and conversation to `/api/requests/stream`
- **THEN** the client receives each available progress event on the open response as the workflow advances, before the terminal result

### Requirement: Summarization requests SHALL stream dedicated events
The API SHALL accept the existing synchronous summarization input at `POST /api/summarize/stream` without callback URLs, execute the sole summarization pipeline using the supplied language and conversation, and stream the same lifecycle event kinds as chat with a summarization-specific terminal result.

#### Scenario: Summarization completes on the same connection
- **WHEN** an authenticated client posts valid summarization input to `/api/summarize/stream`
- **THEN** progress arrives during execution and the terminal completion contains summarized content and its timestamp on that same HTTP response

### Requirement: Stream frames SHALL carry correlated and typed results
Each event SHALL be an SSE frame with an `event` name and one JSON `data` object containing the same generated `requestId` for the full request. Event names SHALL be `workflowStarted`, `workflowStepStarted`, `workflowStepCompleted`, `workflowCompleted`, and `workflowError`. Step payloads SHALL include the existing corresponding callback information (step name, input parameters or elapsed time, agentic flag, and parameter differences). Successful chat completion SHALL contain the existing workflow result; successful summarization completion SHALL contain summarized content and timestamp. A started stream SHALL end with exactly one `workflowCompleted` or `workflowError` event and no later events; every emitted frame SHALL be flushed promptly.

#### Scenario: Chat result is correlated with progress
- **WHEN** a streamed chat workflow finishes successfully
- **THEN** exactly one `workflowCompleted` event carries the generated request ID and full workflow result, and the response ends

#### Scenario: Workflow fails after streaming begins
- **WHEN** a pipeline fails after stream response headers have been sent
- **THEN** exactly one `workflowError` event carries the same request ID and an error message, no completion event is sent, and the response ends

### Requirement: Stream endpoints SHALL preserve API access and failure behavior
Stream endpoints SHALL enforce the configured API key, validate their respective request bodies, and resolve pipeline availability before opening a stream. Pre-stream validation and routing failures SHALL return the existing HTTP error status and problem response, without an SSE body or request ID. Existing synchronous and asynchronous routes and callback contracts SHALL remain available and unchanged; stream requests SHALL NOT issue callbacks. OpenAPI SHALL document the new routes, inputs, SSE content type, event names, and error responses.

#### Scenario: Invalid or unavailable request
- **WHEN** authentication, input validation, or pipeline resolution fails before streaming starts
- **THEN** the API responds with the corresponding existing HTTP error and no workflow executes

#### Scenario: Existing requests remain supported
- **WHEN** a client uses an existing sync or async chat or summarization endpoint
- **THEN** its response and callback behavior remains unchanged

### Requirement: Client disconnect SHALL stop stream execution
The server SHALL propagate cancellation from a disconnected streaming client to the associated pipeline execution and SHALL release the request-scoped resources without sending a terminal event to a closed connection.

#### Scenario: Client closes the response during a step
- **WHEN** the stream client disconnects while a workflow is executing
- **THEN** the server requests cancellation of that workflow and does not continue attempting stream writes or callbacks