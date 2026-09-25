## ADDED Requirements

### Requirement: The architecture documentation SHALL surface custom-pipeline service connectors

The repository README's "Why AgentMesh" section SHALL tell developers that custom pipelines can employ connectors for OpenAI ChatCompletions-compatible endpoints, Mem0, LightRAG, reranker-compatible endpoints, and JSCodeSandbox JavaScript execution. Each named service SHALL link to the public service or repository supplied by the project documentation, and a recognizable badge SHALL be included when a stable badge source is available without implying unsupported functionality.

#### Scenario: Developer reviews available connectors

- **WHEN** a developer reads the README's "Why AgentMesh" section
- **THEN** they can identify all five connector categories and understand that the connectors are available for use in custom pipelines

#### Scenario: Developer follows a connector reference

- **WHEN** a developer selects a named service from the connector documentation
- **THEN** the README takes them to the corresponding OpenAI-compatible endpoint guidance, Mem0 site, LightRAG repository, Cohere site for reranking, or JSCodeSandbox repository

#### Scenario: A service has no suitable stable badge

- **WHEN** the project cannot identify a stable badge source for a listed service
- **THEN** the documentation retains the service name and link without adding a misleading badge