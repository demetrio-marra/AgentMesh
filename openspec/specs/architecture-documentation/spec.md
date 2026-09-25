# architecture-documentation Specification

## Purpose

Provide a stable architecture reference for AgentMesh so future users and contributors can understand system structure, runtime flow, boundaries, and extension points without reverse-engineering source code.

## Requirements

### Requirement: The project SHALL provide a versioned architecture overview
The repository SHALL include architecture documentation, tracked through OpenSpec artifacts, that describes the system at a project level.

#### Scenario: Architecture overview is available
- **WHEN** a contributor opens the architecture documentation capability
- **THEN** they can find a project-level description of AgentMesh architecture and goals

### Requirement: The architecture documentation SHALL describe layering and responsibilities
The architecture documentation SHALL describe `AgentMesh` as the basic domain-entity package, `AgentMesh.Contracts` as the infrastructure-service contract package, `AgentMesh.Infrastructure.*` as external-system adapters, `AgentMesh.Application` as the internal application-framework assembly, `AgentMesh.Runtime` as the reusable plugin-facing runtime and HTTP package, each plugin as an executable pipeline service, and `AgentMeshCLI` as the REST terminal frontend.

#### Scenario: Layer responsibilities are documented
- **WHEN** a contributor reads the architecture documentation
- **THEN** they can identify the responsibility and dependency direction of every reusable package and executable project group

### Requirement: The architecture documentation SHALL distinguish reusable and executable boundaries
The architecture documentation SHALL distinguish reusable domain, contract, adapter, Application, and Runtime assemblies from executable plugin hosts. It SHALL state that plugin web projects reference `AgentMesh.Runtime`, identify their own assembly through `IAgentMeshPlugin`, own final host construction and deployment, and expose one custom pipeline per service.

#### Scenario: Contributor selects an extension boundary
- **WHEN** a contributor evaluates where to add a domain entity, infrastructure integration, custom pipeline component, plugin-specific dependency, runtime HTTP behavior, or terminal interaction
- **THEN** the architecture documentation identifies the responsible package or executable project

### Requirement: Architecture documentation SHALL define cluster-level multi-pipeline deployment
The architecture documentation SHALL recommend one plugin web workload and Service per pipeline when multiple pipelines are required in the same Kubernetes cluster. It SHALL describe assigning pipeline-specific labels or tags and routing messages that require the same pipeline to the corresponding Service.

#### Scenario: Operator plans multiple pipelines in one cluster
- **WHEN** an operator reads the deployment guidance for hosting multiple pipelines
- **THEN** the documentation directs them to deploy separate plugin web workloads and Services per pipeline

#### Scenario: Operator plans pipeline request routing
- **WHEN** an operator needs to route requests to a particular pipeline
- **THEN** the documentation explains that upstream Kubernetes-aware routing selects the corresponding plugin Service and clients call that deployment's unnamed request endpoint

### Requirement: Architecture documentation SHALL define the plugin deployment lifecycle
The architecture documentation SHALL state that each plugin project produces its own executable service and container image, that Runtime does not scan a `Plugins/` directory or hot-load DLL changes, and that a changed plugin is activated by rebuilding and rolling out that plugin service. It SHALL describe how the packaged configuration and Dockerfile templates seed plugin-owned active files.

#### Scenario: Operator deploys the API image without a plugin
- **WHEN** an operator looks for a standalone official API image
- **THEN** the documentation explains that it has been replaced by independently built plugin web-service images

#### Scenario: Operator drops a plugin into the folder
- **WHEN** an operator considers adding or replacing a DLL in a `Plugins/` directory
- **THEN** the documentation explains that folder-based loading is unsupported and directs the operator to rebuild and redeploy the plugin service

#### Scenario: Developer automates pipeline deployment
- **WHEN** a developer wants committed pipeline changes deployed automatically
- **THEN** the documentation directs them to build and publish the plugin executable or image, deploy it, and verify rollout completion

#### Scenario: Developer creates a plugin host
- **WHEN** a developer starts a plugin project
- **THEN** the documentation explains how to create plugin-owned settings and Dockerfile assets from the Runtime templates

### Requirement: The architecture documentation SHALL describe runtime request flow
The architecture documentation SHALL describe the end-to-end flow from plugin host startup through custom dependency registration, Runtime reflection registration, final host construction, HTTP request handling, pipeline execution, and CLI consumption.

#### Scenario: Startup lifecycle is traceable
- **WHEN** a contributor follows the startup flow section
- **THEN** they can trace builder creation, base and environment configuration loading, plugin-specific registration, Runtime registration, plugin-owned build, and Runtime startup in order

#### Scenario: Request lifecycle is traceable
- **WHEN** a contributor follows the request flow section
- **THEN** they can trace requests from the client through Runtime controllers and plugin pipelines to final answer generation

### Requirement: The architecture documentation SHALL define integration boundaries and extension points
The architecture documentation SHALL identify external service boundaries, reflection-registered AgentMesh extension contracts, the empty plugin marker, and explicit plugin-owned service/configuration registration as separate extension mechanisms.

#### Scenario: Integration and customization paths are clear
- **WHEN** a contributor plans an extension
- **THEN** they can determine whether Runtime discovers it through an AgentMesh contract or the plugin must explicitly register it before Runtime initialization

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
