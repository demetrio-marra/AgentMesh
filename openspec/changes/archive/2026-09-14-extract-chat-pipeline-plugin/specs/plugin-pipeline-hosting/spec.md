## MODIFIED Requirements

### Requirement: Framework contracts SHALL be distributable as NuGet packages
The framework SHALL provide distributable NuGet package(s) containing the core contracts, immutable API DTO schema, and application-layer APIs required to build plugin pipelines and steps. CLI and API host projects SHALL consume their AgentMesh framework dependencies through NuGet package references and SHALL not directly reference framework project files.

#### Scenario: Plugin author builds against framework package
- **WHEN** a third-party developer creates a plugin project
- **THEN** they can reference the framework NuGet package(s) and implement framework contracts without modifying host code

#### Scenario: Hosts consume framework packages
- **WHEN** a developer inspects the CLI or API host project references
- **THEN** its AgentMesh framework dependencies are NuGet package references and no project reference targets an AgentMesh framework project

## ADDED Requirements

### Requirement: Default pipeline behavior SHALL be deployable as a plugin
The host SHALL support supplying the `default` chat pipeline and its conversation-summarization pipeline through a separately built plugin rather than implementations compiled into the host framework. The plugin assembly and its dependencies SHALL be made available in the configured plugin directory before host startup. API mode SHALL not give the summarization pipeline a dedicated route or special host behavior.

#### Scenario: Host starts with manually deployed default plugin
- **WHEN** an operator manually places the default-pipeline plugin and its dependencies in the configured plugin directory before startup
- **THEN** the host loads the plugin, routes requests for the `default` pipeline to it, and makes its summarization pipeline available for explicit interactive invocation

#### Scenario: API does not expose summarization as a special endpoint
- **WHEN** an API host loads a plugin that provides a summarization pipeline
- **THEN** the API exposes no dedicated summarization endpoint and retains its normal stateless chat request routes
