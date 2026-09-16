## MODIFIED Requirements

### Requirement: REST request endpoint SHALL invoke the existing request pipeline
The system SHALL expose HTTP endpoints that accept a user message along with caller-provided conversation messages (`IEnumerable<ContextMessage>`) and return the result produced by the dedicated stateless application instance (`StatelessAppInstance`), with route-based pipeline selection and without retaining conversation state or executing host-side context summarization. The response SHALL also include a `requestId` (a newly generated GUID for that request), alongside the workflow output, so a `requestId` is present uniformly across synchronous and asynchronous request modes.

#### Scenario: API request is processed successfully
- **WHEN** API mode is active and a client sends a valid request message with optional conversation messages to the REST endpoint
- **THEN** the endpoint passes the conversation messages to the stateless application instance, executes the resolved pipeline, and returns a successful response containing the workflow output and a generated `requestId`

#### Scenario: Named pipeline request is processed successfully
- **WHEN** API mode is active and a client sends a valid request message with optional conversation messages to `POST /api/pipelines/{pipelineName}/requests` with a pipeline name that matches a loaded pipeline
- **THEN** the endpoint executes the matched pipeline via the stateless application instance and returns a successful response containing the workflow output and a generated `requestId`

#### Scenario: Non-interactive mode does not mutate server state or run context summarization
- **WHEN** an API request is processed in non-interactive mode
- **THEN** the host does not retain or accumulate conversation messages in server memory and does not execute the host-side summarization pipeline

#### Scenario: Named pipeline is not found
- **WHEN** API mode is active and a client sends a valid request message to `POST /api/pipelines/{pipelineName}/requests` with a pipeline name that does not match any loaded pipeline
- **THEN** the endpoint returns `404 Not Found`

#### Scenario: Default route works only with a single loaded pipeline
- **WHEN** API mode is active and a client sends a valid request message to `POST /api/requests` and exactly one pipeline is loaded
- **THEN** the endpoint executes the only loaded pipeline and returns a successful response containing the workflow output generated from that message and a generated `requestId`

#### Scenario: Default route requires explicit pipeline name when multiple pipelines exist
- **WHEN** API mode is active and a client sends a valid request message to `POST /api/requests` and more than one pipeline is loaded
- **THEN** the endpoint returns `400 Bad Request` with RFC7807 `ProblemDetails` and detail message `pipeline name is required`

### Requirement: Workflow progress notifier SHALL be mode-specific
The workflow progress notifier implementation SHALL be selected by runtime mode. In API mode, the resolved notifier SHALL be scoped to the individual request being processed: for synchronous requests and async requests without callback URLs it SHALL remain a no-op; for async requests that supply all 5 callback URLs it SHALL deliver progress events to those URLs.

#### Scenario: API mode uses dummy notifier
- **WHEN** the application starts without `--interactive`
- **THEN** `IWorkflowProgressNotifier` resolves to a no-op implementation that performs no console output

#### Scenario: Synchronous API request uses a no-op notifier
- **WHEN** the application starts without `--interactive` and a client calls a synchronous request endpoint
- **THEN** `IWorkflowProgressNotifier` resolves to an implementation that performs no external HTTP calls for that request

#### Scenario: Async API request without callback URLs uses a no-op notifier
- **WHEN** the application starts without `--interactive` and a client calls the async request endpoint without supplying callback URLs
- **THEN** `IWorkflowProgressNotifier` resolves to an implementation that performs no external HTTP calls for that request

#### Scenario: Async API request with callback URLs uses a callback-posting notifier
- **WHEN** the application starts without `--interactive` and a client calls the async request endpoint with all 5 callback URLs supplied
- **THEN** `IWorkflowProgressNotifier` resolves to an implementation scoped to that request that posts progress events to the supplied callback URLs
