## MODIFIED Requirements

### Requirement: REST request endpoint SHALL invoke the existing request pipeline
The system SHALL expose HTTP endpoints that accept a user message along with caller-provided conversation messages (`IEnumerable<ContextMessage>`) and return the result produced by the dedicated stateless application instance (`StatelessAppInstance`), with route-based pipeline selection and without retaining conversation state or executing host-side context summarization.

#### Scenario: API request is processed successfully
- **WHEN** API mode is active and a client sends a valid request message with optional conversation messages to the REST endpoint
- **THEN** the endpoint passes the conversation messages to the stateless application instance, executes the resolved pipeline, and returns a successful response containing the workflow output

#### Scenario: Named pipeline request is processed successfully
- **WHEN** API mode is active and a client sends a valid request message with optional conversation messages to `POST /api/pipelines/{pipelineName}/requests` with a pipeline name that matches a loaded pipeline
- **THEN** the endpoint executes the matched pipeline via the stateless application instance and returns a successful response containing the workflow output

#### Scenario: Non-interactive mode does not mutate server state or run context summarization
- **WHEN** an API request is processed in non-interactive mode
- **THEN** the host does not retain or accumulate conversation messages in server memory and does not execute the host-side summarization pipeline

#### Scenario: Named pipeline is not found
- **WHEN** API mode is active and a client sends a valid request message to `POST /api/pipelines/{pipelineName}/requests` with a pipeline name that does not match any loaded pipeline
- **THEN** the endpoint returns `404 Not Found`

#### Scenario: Default route works only with a single loaded pipeline
- **WHEN** API mode is active and a client sends a valid request message to `POST /api/requests` and exactly one pipeline is loaded
- **THEN** the endpoint executes the only loaded pipeline and returns a successful response containing the workflow output generated from that message

#### Scenario: Default route requires explicit pipeline name when multiple pipelines exist
- **WHEN** API mode is active and a client sends a valid request message to `POST /api/requests` and more than one pipeline is loaded
- **THEN** the endpoint returns `400 Bad Request` with RFC7807 `ProblemDetails` and detail message `pipeline name is required`

## ADDED Requirements

### Requirement: Plugin startup and routing failures SHALL be reported as generic RFC7807 errors
The system SHALL not fail-fast for plugin loading and plugin registry issues at startup; instead, plugin-related service configuration errors SHALL be returned at request time as generic RFC7807 responses that do not expose internal plugin file or assembly details.

#### Scenario: No pipelines loaded
- **WHEN** API mode is active, plugin startup completed, and no pipeline is available
- **THEN** the endpoint returns `503 Service Unavailable` with RFC7807 `ProblemDetails` and detail message `No pipelines loaded`

#### Scenario: Plugin configuration issue requires redeploy
- **WHEN** API mode is active and a plugin-related configuration issue exists that requires startup-time correction (such as invalid plugin registration or duplicate pipeline names)
- **THEN** the endpoint returns `503 Service Unavailable` with RFC7807 `ProblemDetails` and a generic detail message instructing the caller to redeploy the service after resolving the plugin issue

#### Scenario: Plugin error responses do not reveal internal details
- **WHEN** API mode is active and a plugin-related configuration issue is returned to the caller
- **THEN** the response does not include internal plugin file paths, assembly names, or implementation details

### Requirement: API schema SHALL document endpoints, payloads, and string-formatted enum representations in Swagger/OpenAPI
The system SHALL generate comprehensive OpenAPI/Swagger documentation with descriptive summaries, parameters, response contracts, and string-serialized enum values for conversation message roles (`"User"` and `"Assistant"`).

#### Scenario: OpenAPI documentation contains complete request and response descriptions
- **WHEN** API mode is active and Swagger/OpenAPI schema is generated
- **THEN** endpoint routes, parameters, request body fields (including `message` and `conversation`), and response schema fields contain human-readable descriptions

#### Scenario: Conversation message roles are represented as strings in OpenAPI schema
- **WHEN** API mode is active and the OpenAPI specification is retrieved
- **THEN** `ContextMessageRole` is documented and serialized as string enum values (`"User"`, `"Assistant"`) rather than integer values