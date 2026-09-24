## MODIFIED Requirements

### Requirement: REST request endpoint SHALL invoke the existing request pipeline
The system SHALL expose HTTP endpoints that accept a user message along with caller-provided conversation messages and return the result produced by the dedicated stateless chat request pipeline, with route-based chat pipeline selection and without retaining conversation state or executing host-side context summarization. In addition, the system SHALL expose separate summarization endpoints that accept a summarization language and conversation messages and invoke the sole registered summarization pipeline without pipeline-name selection. Chat and summarization endpoints SHALL use separate specialized input and output contracts.

#### Scenario: API request is processed successfully
- **WHEN** API mode is active and a client sends a valid chat request message with optional conversation messages to the REST endpoint
- **THEN** the endpoint passes the conversation messages to the stateless chat application instance, executes the resolved chat pipeline, and returns the existing chat workflow output with a generated request identifier

#### Scenario: Summarization request is processed successfully
- **WHEN** API mode is active and a client sends valid summarization language and conversation messages to the summarization endpoint and exactly one summarization pipeline is registered
- **THEN** the endpoint executes that pipeline without requiring or accepting a pipeline name and returns the dedicated summarization output with a generated request identifier

#### Scenario: Summarization is unavailable or ambiguous
- **WHEN** API mode is active and zero or multiple summarization pipelines are registered
- **THEN** the summarization endpoint returns a generic service/configuration error and does not choose a pipeline by name

#### Scenario: Named pipeline request is processed successfully
- **WHEN** API mode is active and a client sends a valid chat request message with optional conversation messages to `POST /api/pipelines/{pipelineName}/requests` with a pipeline name that matches a loaded chat pipeline
- **THEN** the endpoint executes the matched chat pipeline via the stateless application instance and returns a successful response containing the workflow output and a generated request identifier

#### Scenario: Non-interactive mode does not mutate server state or run context summarization
- **WHEN** an API request is processed in non-interactive mode
- **THEN** the host does not retain or accumulate conversation messages in server memory and does not execute host-side context summarization as part of chat request processing

#### Scenario: Named pipeline is not found
- **WHEN** API mode is active and a client sends a valid chat request message to `POST /api/pipelines/{pipelineName}/requests` with a pipeline name that does not match any loaded chat pipeline
- **THEN** the endpoint returns `404 Not Found`

#### Scenario: Default route works only with a single loaded pipeline
- **WHEN** API mode is active and a client sends a valid chat request message to `POST /api/requests` and exactly one chat pipeline is loaded
- **THEN** the endpoint executes the only loaded chat pipeline and returns a successful response containing the workflow output generated from that message and a generated request identifier

#### Scenario: Default route requires explicit pipeline name when multiple pipelines exist
- **WHEN** API mode is active and a client sends a valid chat request message to `POST /api/requests` and more than one chat pipeline is loaded
- **THEN** the endpoint returns `400 Bad Request` with RFC7807 `ProblemDetails` and detail message `pipeline name is required`
