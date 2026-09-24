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
The host SHALL load the single pipeline plugin assembly and its dependencies from a configured `Plugins` folder only during container startup. The host SHALL NOT scan and compose multiple pipeline plugin assemblies into one runtime, watch the folder for changes, or load a plugin added after the process has started.

#### Scenario: Startup-time plugin discovery from mounted folder
- **WHEN** the service starts and a pipeline plugin assembly is present in the configured `Plugins` folder
- **THEN** the host loads that assembly and makes its registered pipeline available for request handling

#### Scenario: Plugin is absent at startup
- **WHEN** the service starts and no pipeline plugin assembly is present in the configured `Plugins` folder
- **THEN** startup completes without a pipeline and availability is reported when a request requires it

#### Scenario: No runtime hot detection is required
- **WHEN** a plugin DLL is added or replaced after the service has started
- **THEN** the running API does not detect or load that change automatically

#### Scenario: Operator activates a dropped plugin
- **WHEN** a DevOps engineer adds or replaces the plugin files in the mounted `Plugins` folder
- **THEN** the engineer runs `kubectl rollout restart deployment/<deployment-name>` and the API loads the plugin only when the replacement pod starts

### Requirement: Each API deployment SHALL host one chat pipeline
Each API deployment SHALL expose at most one `IChatRequestPipeline` implementation for request processing. Pipeline identity SHALL be represented by the deployment and its network endpoint rather than by an application-level pipeline name used for request routing.

#### Scenario: Single pipeline is available
- **WHEN** an API deployment has one valid chat pipeline registered by its plugin
- **THEN** every chat request accepted by that deployment is executed by that pipeline without pipeline-name selection

#### Scenario: Multiple pipeline registrations are invalid
- **WHEN** a plugin registers more than one chat pipeline in one API deployment
- **THEN** the deployment does not select among them and chat requests return a generic plugin configuration error

### Requirement: Default pipeline behavior SHALL be deployable as a plugin
The official API Docker image SHALL NOT contain or install a default pipeline plugin. The host SHALL support supplying the chat pipeline and its conversation-summarization pipeline through a separately built plugin rather than implementations compiled into the host framework. The plugin assembly and its dependencies SHALL be made available in the configured plugin directory before host startup. The plugin's chat pipeline SHALL be the sole chat pipeline for that API deployment and SHALL require no routing name. API mode SHALL retain its dedicated summarization endpoints without pipeline-name selection.

#### Scenario: API image contains no default plugin
- **WHEN** the official API Docker image is built without deployment-specific additions
- **THEN** its configured `Plugins` folder contains no default pipeline plugin supplied by the image

#### Scenario: Host starts with manually deployed default plugin
- **WHEN** an operator manually places the default-pipeline plugin and its dependencies in the configured plugin directory before startup
- **THEN** the host loads the plugin, routes unnamed chat requests to its sole chat pipeline, and makes its summarization pipeline available through the summarization endpoints

#### Scenario: API does not expose summarization as a special endpoint
- **WHEN** an API host loads a plugin that provides a summarization pipeline
- **THEN** the API retains its standard summarization endpoints without creating plugin-named summarization routes or accepting a pipeline name

### Requirement: Plugin-hosting documentation SHALL define package responsibilities
Documentation for plugin hosting SHALL identify `AgentMesh` as domain entities, `AgentMesh.Contracts` as infrastructure service definitions, `AgentMesh.Infrastructure.*` as external adapters, `AgentMesh.Application` as the plugin-facing framework, and `AgentMesh.Api` as the custom-pipeline host.

#### Scenario: Plugin author reviews layering
- **WHEN** a plugin author reads plugin-hosting documentation
- **THEN** they can choose the correct dependency boundary for framework extensions and external integrations
