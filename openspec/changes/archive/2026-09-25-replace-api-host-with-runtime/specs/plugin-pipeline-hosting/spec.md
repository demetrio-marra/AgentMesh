## ADDED Requirements

### Requirement: Runtime SHALL discover plugin components through a marker assembly
Each plugin SHALL expose a concrete type implementing the empty `IAgentMeshPlugin` marker contract. Runtime initialization SHALL use that marker type to identify the compile-time referenced plugin assembly and reflection-register every concrete implementation of documented AgentMesh extension contracts, including pipeline, summarization pipeline, step, parameter, agent, and serializer contracts.

#### Scenario: Runtime initializes a plugin assembly
- **WHEN** a plugin passes its marker type to Runtime initialization
- **THEN** Runtime scans that marker's assembly and registers its recognized AgentMesh component implementations

#### Scenario: Plugin provides no manual framework registrations
- **WHEN** a plugin contains recognized AgentMesh component implementations
- **THEN** the plugin does not need to register those implementations individually

#### Scenario: Runtime encounters an unrelated plugin type
- **WHEN** the plugin assembly contains a type that implements no recognized AgentMesh extension contract
- **THEN** Runtime does not automatically register that type

### Requirement: Plugin SHALL compose custom dependencies before Runtime initialization
The plugin composition root SHALL load configuration and register any plugin-specific configuration or services before invoking Runtime initialization. Runtime SHALL preserve those registrations so reflected components can receive plugin-specific dependencies when the built provider later constructs them.

#### Scenario: Pipeline depends on plugin configuration
- **WHEN** the plugin registers its configuration before Runtime initialization and a reflected pipeline requires that configuration
- **THEN** dependency injection supplies the registered configuration when the pipeline is instantiated

#### Scenario: Plugin defines no custom dependency
- **WHEN** all plugin component dependencies are supplied by AgentMesh
- **THEN** the plugin can invoke Runtime initialization without custom service registrations

### Requirement: Plugin SHALL retain final host construction
Runtime initialization SHALL register services without building the service provider. The plugin SHALL call the final web-application build operation and SHALL pass the built application to a separate Runtime startup operation that configures controllers, authentication, authorization, Swagger, and host execution.

#### Scenario: Plugin builds after registration
- **WHEN** plugin-specific and Runtime service registrations are complete
- **THEN** the plugin performs the final web-application build exactly before invoking Runtime startup

#### Scenario: Runtime registration executes
- **WHEN** the plugin invokes Runtime registration
- **THEN** Runtime does not build an intermediate or final service provider

#### Scenario: Runtime starts a built application
- **WHEN** the plugin passes its built web application to Runtime startup
- **THEN** the existing controllers, authentication, authorization, and Swagger surface are configured and the service starts

## MODIFIED Requirements

### Requirement: Framework contracts SHALL be distributable as NuGet packages
The framework SHALL provide distributable NuGet packages containing core contracts, immutable API DTO schemas, infrastructure contracts, and the Runtime and Application APIs required to build plugin web services. Plugins SHALL consume AgentMesh framework dependencies through NuGet package references and SHALL not directly reference framework project files.

#### Scenario: Plugin author builds against framework package
- **WHEN** a third-party developer creates a plugin web project
- **THEN** they can reference `AgentMesh.Runtime` and implement framework contracts without modifying Runtime code

#### Scenario: Hosts consume framework packages
- **WHEN** a developer inspects a plugin web host's references
- **THEN** its AgentMesh framework dependencies are NuGet package references and no project reference targets an AgentMesh framework project

#### Scenario: Plugin author builds against Runtime package
- **WHEN** a third-party developer creates a plugin web project
- **THEN** they can reference `AgentMesh.Runtime`, implement framework contracts, and host the API without modifying Runtime code

#### Scenario: Plugin consumes framework packages
- **WHEN** a developer inspects a plugin project's references
- **THEN** its AgentMesh framework dependencies are NuGet package references and no project reference targets an AgentMesh framework project

### Requirement: Each API deployment SHALL host one chat pipeline
Each plugin web deployment SHALL expose exactly one `IChatRequestPipeline` implementation for request processing. Pipeline identity SHALL be represented by the deployment and its network endpoint rather than by an application-level pipeline name used for request routing.

#### Scenario: Single pipeline is available
- **WHEN** Runtime reflection finds one chat pipeline in the plugin assembly
- **THEN** every chat request accepted by that deployment is executed by that pipeline without pipeline-name selection

#### Scenario: Multiple pipeline registrations are invalid
- **WHEN** Runtime initialization finds more than one chat pipeline implementation in the plugin assembly
- **THEN** startup or request availability reports a generic plugin configuration error rather than selecting among them

### Requirement: Default pipeline behavior SHALL be deployable as a plugin
The default pipeline SHALL be deployable as its own plugin web application. Its executable SHALL consume Runtime, contain the chat and summarization pipeline implementations, and expose the standard unnamed request and summarization endpoints without a separately mounted plugin DLL.

#### Scenario: API image contains no default plugin
- **WHEN** the Runtime package is produced
- **THEN** it contains no compiled default pipeline implementation or executable default-plugin image

#### Scenario: Host starts with manually deployed default plugin
- **WHEN** an operator deploys and starts the default plugin web application
- **THEN** it reflection-registers its compiled pipeline components and exposes the unnamed chat and summarization endpoints

#### Scenario: API does not expose summarization as a special endpoint
- **WHEN** the default plugin provides a summarization pipeline
- **THEN** Runtime exposes the standard summarization endpoints without creating plugin-named routes or accepting a pipeline name

#### Scenario: Start the default plugin service
- **WHEN** the default plugin executable starts with valid configuration
- **THEN** Runtime registers its components and exposes its chat and summarization pipelines through the standard API

#### Scenario: Deployment contains no plugin folder
- **WHEN** the default plugin service is published or containerized
- **THEN** it runs without discovering or loading assemblies from a configured `Plugins/` directory

### Requirement: Plugin-hosting documentation SHALL define package responsibilities
Documentation for plugin hosting SHALL identify `AgentMesh` as domain entities, `AgentMesh.Contracts` as infrastructure service definitions, `AgentMesh.Infrastructure.*` as external adapters, `AgentMesh.Application` as the internal application framework assembly, `AgentMesh.Runtime` as the plugin-facing runtime package, and each plugin project as the executable custom-pipeline host.

#### Scenario: Plugin author reviews layering
- **WHEN** a plugin author reads plugin-hosting documentation
- **THEN** they can choose the correct package and composition boundary for framework extensions, custom dependencies, configuration, and deployment

## REMOVED Requirements

### Requirement: Plugins SHALL self-register services through a bootstrap contract
**Reason**: Runtime now reflection-registers recognized framework components from the assembly identified by the empty marker contract.

**Migration**: Replace the bootstrap implementation with an `IAgentMeshPlugin` marker type and retain only explicit plugin-specific registrations before Runtime initialization.

### Requirement: Host SHALL load plugins from the Plugins folder at startup
**Reason**: Plugins are executable hosts with compile-time Runtime references; dynamic DLL discovery and loading are no longer supported.

**Migration**: Publish and deploy each plugin web project directly, then restart or roll out that service when its executable changes.