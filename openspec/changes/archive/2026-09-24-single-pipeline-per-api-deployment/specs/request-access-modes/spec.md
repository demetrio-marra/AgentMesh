## MODIFIED Requirements

### Requirement: REST request endpoint SHALL invoke the existing request pipeline
The system SHALL expose only unnamed HTTP chat endpoints that accept a user message along with caller-provided conversation messages (`IEnumerable<ContextMessage>`) and return the result produced by the single stateless application runner using the deployment's sole chat pipeline, without retaining conversation state or executing host-side context summarization. The response SHALL also include a `requestId` (a newly generated GUID for that request), alongside the workflow output, so a `requestId` is present uniformly across synchronous and asynchronous request modes. In addition, the system SHALL expose separate summarization endpoints that accept a summarization language and conversation messages and invoke the sole registered summarization pipeline without pipeline-name selection. Chat and summarization endpoints SHALL use separate specialized input and output contracts.

#### Scenario: API request is processed successfully
- **WHEN** a client sends a valid request message with optional conversation messages to `POST /api/requests` and one chat pipeline is available
- **THEN** the endpoint passes the conversation messages to the single stateless application runner, executes that pipeline, and returns a successful response containing the workflow output and a generated `requestId`

#### Scenario: Async API request is accepted successfully
- **WHEN** a client sends a valid asynchronous request to `POST /api/requests/async` and one chat pipeline is available
- **THEN** the endpoint starts that pipeline without pipeline-name selection and returns an accepted response containing a generated `requestId`

#### Scenario: Named pipeline request is processed successfully
- **WHEN** a client needs a particular pipeline and sends a valid request to that pipeline's Kubernetes Service using `POST /api/requests`
- **THEN** the selected deployment executes its sole pipeline and returns a successful response without an application-level pipeline name

#### Scenario: Named pipeline is not found
- **WHEN** a client sends a request to the removed `/api/pipelines/{pipelineName}/requests` path
- **THEN** the API has no matching endpoint and does not perform application-level pipeline lookup

#### Scenario: Default route works only with a single loaded pipeline
- **WHEN** a client sends a valid request to `POST /api/requests` and exactly one chat pipeline is registered
- **THEN** the endpoint executes that pipeline and returns a successful response containing the workflow output and a generated `requestId`

#### Scenario: Default route requires explicit pipeline name when multiple pipelines exist
- **WHEN** multiple pipeline deployments exist in the cluster
- **THEN** upstream routing selects the pipeline-specific Service and the selected API deployment accepts the unnamed request without a pipeline-name parameter

#### Scenario: Named pipeline routes are unavailable
- **WHEN** a client sends a request to `/api/pipelines/{pipelineName}/requests` or `/api/pipelines/{pipelineName}/requests/async`
- **THEN** the API has no matching named-pipeline endpoint

#### Scenario: Non-interactive mode does not mutate server state or run context summarization
- **WHEN** an API request is processed in non-interactive mode
- **THEN** the host does not retain or accumulate conversation messages in server memory and does not execute host-side context summarization

#### Scenario: Summarization request is processed successfully
- **WHEN** a client sends valid summarization language and conversation messages to the summarization endpoint and exactly one summarization pipeline is registered
- **THEN** the endpoint executes that pipeline through the single stateless application runner without requiring or accepting a pipeline name and returns the dedicated summarization output with a generated request identifier

#### Scenario: Summarization is unavailable or ambiguous
- **WHEN** zero or multiple summarization pipelines are registered
- **THEN** the summarization endpoint returns a generic service or configuration error and does not choose a pipeline by name

### Requirement: Plugin startup and routing failures SHALL be reported as generic RFC7807 errors
The system SHALL not fail fast when no plugin or usable pipeline is found at startup. Plugin loading or registration failures that can be represented safely SHALL be retained as deployment availability state and returned at request time as generic RFC7807 responses that do not expose internal plugin file or assembly details.

#### Scenario: No pipeline plugin is deployed
- **WHEN** the API starts without a pipeline plugin in the configured plugin location
- **THEN** startup succeeds and the host remains available to receive requests

#### Scenario: No pipelines loaded
- **WHEN** a chat request reaches an API deployment with no usable chat pipeline
- **THEN** the endpoint returns `503 Service Unavailable` with RFC7807 `ProblemDetails` and detail message `No pipelines loaded`

#### Scenario: Plugin configuration issue requires redeploy
- **WHEN** a request requires a pipeline and a plugin-related loading or registration issue prevents the deployment from exposing exactly one usable pipeline
- **THEN** the endpoint returns `503 Service Unavailable` with RFC7807 `ProblemDetails` and a generic detail message instructing the caller to redeploy the service after resolving the plugin issue

#### Scenario: Plugin error responses do not reveal internal details
- **WHEN** a plugin-related configuration issue is returned to the caller
- **THEN** the response does not include internal plugin file paths, assembly names, or implementation details

### Requirement: API schema SHALL document endpoints, payloads, and string-formatted enum representations in Swagger/OpenAPI
The system SHALL generate comprehensive OpenAPI/Swagger documentation for the unnamed chat and summarization endpoints with descriptive summaries, parameters, response contracts, and string-serialized enum values for conversation message roles (`"User"` and `"Assistant"`). The schema SHALL NOT advertise named-pipeline request routes.

#### Scenario: OpenAPI documentation contains complete request and response descriptions
- **WHEN** the OpenAPI specification is generated
- **THEN** the unnamed endpoint routes, request body fields (including `message` and `conversation`), and response schema fields contain human-readable descriptions

#### Scenario: Named routes are absent from OpenAPI documentation
- **WHEN** the OpenAPI specification is generated
- **THEN** `/api/pipelines/{pipelineName}/requests` and `/api/pipelines/{pipelineName}/requests/async` are not present

#### Scenario: Conversation message roles are represented as strings in OpenAPI schema
- **WHEN** the OpenAPI specification is retrieved
- **THEN** `ContextMessageRole` is documented and serialized as string enum values (`"User"`, `"Assistant"`) rather than integer values