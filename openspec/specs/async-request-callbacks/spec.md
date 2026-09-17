## Purpose

Allow API callers to submit a chat/workflow request that runs to completion in the background and to receive workflow progress via caller-supplied HTTP callback URLs instead of blocking on the response.

## Requirements

### Requirement: Async request endpoint SHALL return a request identifier without waiting for workflow completion
The system SHALL expose an HTTP endpoint that accepts a message, optional conversation history, and up to 5 optional callback URLs, and SHALL respond immediately with a generated `requestId` (a GUID) before the underlying workflow has finished executing.

#### Scenario: Async request is accepted and a request id is returned immediately
- **WHEN** a client sends a valid async request (with or without callback URLs) to the async endpoint
- **THEN** the endpoint returns a response containing a newly generated `requestId` without waiting for the workflow to complete

#### Scenario: Workflow executes in the background after the response is sent
- **WHEN** an async request has been accepted and its `requestId` returned to the caller
- **THEN** the workflow continues executing after the HTTP response has been sent, using the same message and conversation history that were provided in the request

### Requirement: Callback URL configuration SHALL be all-or-nothing
The system SHALL require that if any one of the 5 callback URLs (`workflowStarted`, `workflowStepStarted`, `workflowStepCompleted`, `workflowCompleted`, `workflowError`) is provided on an async request, all 5 SHALL be provided; otherwise the request SHALL be rejected.

#### Scenario: Request with all 5 callback URLs is accepted
- **WHEN** a client sends an async request that supplies all 5 callback URLs
- **THEN** the request is accepted and processed

#### Scenario: Request with no callback URLs is accepted
- **WHEN** a client sends an async request that supplies none of the 5 callback URLs
- **THEN** the request is accepted and processed without any callback delivery

#### Scenario: Request with a partial set of callback URLs is rejected
- **WHEN** a client sends an async request that supplies at least one, but fewer than 5, of the callback URLs
- **THEN** the endpoint returns `400 Bad Request` and the workflow is not executed

### Requirement: Workflow progress SHALL be delivered to configured callback URLs
When an async request supplies all 5 callback URLs, the system SHALL perform an HTTP POST to the corresponding callback URL when each matching workflow progress event occurs, and each callback payload SHALL include the `requestId` returned to the original caller.

#### Scenario: Workflow start callback is delivered
- **WHEN** the workflow begins executing for an async request with callback URLs configured
- **THEN** the system POSTs to the `workflowStarted` callback URL with a payload containing the request's `requestId`

#### Scenario: Workflow step started callback is delivered
- **WHEN** a workflow step begins executing for an async request with callback URLs configured
- **THEN** the system POSTs to the `workflowStepStarted` callback URL with a payload containing the `requestId`, the step name, and the step's input parameters

#### Scenario: Workflow step completed callback is delivered
- **WHEN** a workflow step finishes executing for an async request with callback URLs configured
- **THEN** the system POSTs to the `workflowStepCompleted` callback URL with a payload containing the `requestId`, the step name, and the step's execution statistics (elapsed time, whether it was agentic, and parameter differences)

#### Scenario: Workflow completed callback is delivered
- **WHEN** the workflow finishes executing successfully for an async request with callback URLs configured
- **THEN** the system POSTs to the `workflowCompleted` callback URL with a payload containing the `requestId` and the final workflow result, and does not POST to the `workflowError` callback URL

#### Scenario: Workflow error callback is delivered
- **WHEN** the workflow execution fails for an async request with callback URLs configured
- **THEN** the system POSTs to the `workflowError` callback URL with a payload containing the `requestId` and the error message, and does not POST to the `workflowCompleted` callback URL

#### Scenario: No callbacks are attempted when callback URLs are not configured
- **WHEN** the workflow executes (successfully or not) for an async request that did not supply callback URLs
- **THEN** the system does not attempt any HTTP POST callback for that request

### Requirement: Async request pipeline resolution errors SHALL be returned synchronously and SHALL NOT trigger the workflowError callback
The system SHALL resolve the target pipeline (pipeline-name requirement, existence, and plugin configuration validity) before generating a `requestId`, starting background workflow execution, or responding to the caller. If pipeline resolution fails, the endpoint SHALL return the corresponding synchronous HTTP error response (`400 Bad Request`, `404 Not Found`, or `503 Service Unavailable`) immediately, SHALL NOT return `202 Accepted` or a `requestId`, and SHALL NOT invoke the `workflowError` callback for this failure.

#### Scenario: No pipelines loaded on async endpoint
- **WHEN** a client sends a valid async request and no pipelines are loaded
- **THEN** the endpoint immediately returns `503 Service Unavailable` and does not return a `requestId` or start background execution

#### Scenario: Ambiguous pipeline name on default async route
- **WHEN** a client sends a valid async request to `POST /api/requests/async` and more than one pipeline is loaded
- **THEN** the endpoint immediately returns `400 Bad Request` and does not return a `requestId` or start background execution

#### Scenario: Named pipeline not found on async endpoint
- **WHEN** a client sends a valid async request to `POST /api/pipelines/{pipelineName}/requests/async` with a pipeline name that does not match any loaded pipeline
- **THEN** the endpoint immediately returns `404 Not Found` and does not return a `requestId` or start background execution

#### Scenario: Pipeline resolution failure never triggers the workflowError callback
- **WHEN** a client sends a valid async request with all 5 callback URLs configured and pipeline resolution fails for any reason
- **THEN** the endpoint returns the synchronous HTTP error response described above and the `workflowError` callback URL is never invoked for that failure

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
