## Purpose

Define framework and host behavior for loading third-party pipeline plugins from startup-mounted DLLs, enabling multi-pipeline execution through a stable framework contract and NuGet-based extensibility.

## Requirements

### Requirement: Framework contracts SHALL be distributable as NuGet packages
The framework SHALL provide distributable NuGet package(s) containing the core contracts, immutable API DTO schema, and application-layer APIs required to build plugin pipelines and steps. CLI and API host projects SHALL consume their AgentMesh framework dependencies through NuGet package references and SHALL not directly reference framework project files.

#### Scenario: Plugin author builds against framework package
- **WHEN** a third-party developer creates a plugin project
- **THEN** they can reference the framework NuGet package(s) and implement framework contracts without modifying host code

#### Scenario: Hosts consume framework packages
- **WHEN** a developer inspects the CLI or API host project references
- **THEN** its AgentMesh framework dependencies are NuGet package references and no project reference targets an AgentMesh framework project

### Requirement: Plugins SHALL self-register services through a bootstrap contract
The host SHALL support plugin bootstrap self-registration so plugin authors can explicitly register their pipelines, steps, parameters, and dependencies.

#### Scenario: Plugin registers services through bootstrap
- **WHEN** the host loads a plugin assembly at startup and finds a plugin bootstrap implementation
- **THEN** it invokes plugin registration to add only the services explicitly declared by the plugin

### Requirement: Host SHALL load plugins from the Plugins folder at startup
The host SHALL load plugin assemblies from a configured `Plugins` folder during container startup.

#### Scenario: Startup-time plugin discovery from mounted folder
- **WHEN** the service starts and plugin assemblies are present in the configured `Plugins` folder
- **THEN** the host loads those assemblies and makes plugin-registered pipelines available for request handling

#### Scenario: No runtime hot detection is required
- **WHEN** plugin DLLs are added or replaced after the service has started
- **THEN** those changes are not applied until the service is restarted/redeployed

### Requirement: Multiple chat pipelines SHALL be supported with unique names
The host SHALL support multiple `IChatRequestPipeline` implementations simultaneously, and each pipeline SHALL expose a unique `Name` used for routing.

#### Scenario: Pipeline names are resolved case-insensitively
- **WHEN** a client requests a named pipeline route using a different letter case
- **THEN** the host resolves the pipeline name successfully using case-insensitive matching

#### Scenario: Duplicate pipeline names invalidate plugin configuration
- **WHEN** multiple loaded pipelines expose the same `Name` under case-insensitive comparison
- **THEN** requests return a plugin configuration error response requiring redeploy, as defined by request access mode behavior

### Requirement: Default pipeline behavior SHALL be deployable as a plugin
The host SHALL support supplying the `default` chat pipeline and its conversation-summarization pipeline through a separately built plugin rather than implementations compiled into the host framework. The plugin assembly and its dependencies SHALL be made available in the configured plugin directory before host startup. API mode SHALL not give the summarization pipeline a dedicated route or special host behavior.

#### Scenario: Host starts with manually deployed default plugin
- **WHEN** an operator manually places the default-pipeline plugin and its dependencies in the configured plugin directory before startup
- **THEN** the host loads the plugin, routes requests for the `default` pipeline to it, and makes its summarization pipeline available for explicit interactive invocation

#### Scenario: API does not expose summarization as a special endpoint
- **WHEN** an API host loads a plugin that provides a summarization pipeline
- **THEN** the API exposes no dedicated summarization endpoint and retains its normal stateless chat request routes

### Requirement: Plugin-hosting documentation SHALL define package responsibilities
Documentation for plugin hosting SHALL identify `AgentMesh` as domain entities, `AgentMesh.Contracts` as infrastructure service definitions, `AgentMesh.Infrastructure.*` as external adapters, `AgentMesh.Application` as the plugin-facing framework, and `AgentMesh.Api` as the custom-pipeline host.

#### Scenario: Plugin author reviews layering
- **WHEN** a plugin author reads plugin-hosting documentation
- **THEN** they can choose the correct dependency boundary for framework extensions and external integrations
