## Purpose

Provide a stable architecture reference for AgentMesh so future users and contributors can understand system structure, runtime flow, boundaries, and extension points without reverse-engineering source code.

## Requirements

### Requirement: The project SHALL provide a versioned architecture overview
The repository SHALL include architecture documentation, tracked through OpenSpec artifacts, that describes the system at a project level.

#### Scenario: Architecture overview is available
- **WHEN** a contributor opens the architecture documentation capability
- **THEN** they can find a project-level description of AgentMesh architecture and goals

### Requirement: The architecture documentation SHALL describe layering and responsibilities
The architecture documentation SHALL describe `AgentMesh` as the basic domain-entity package, `AgentMesh.Contracts` as the infrastructure-service contract package, `AgentMesh.Infrastructure.*` as external-system adapters, `AgentMesh.Application` as the plugin-facing agentic-pipeline framework package, `AgentMesh.Api` as the executable host for custom pipelines, and `AgentMeshCLI` as the REST terminal frontend for the API.

#### Scenario: Layer responsibilities are documented
- **WHEN** a contributor reads the architecture documentation
- **THEN** they can identify the responsibility of each of the six projects and project groups

### Requirement: The architecture documentation SHALL distinguish reusable and executable boundaries
The architecture documentation SHALL distinguish reusable domain, contract, adapter, and framework packages from executable hosts. It SHALL state that custom class-library plugins use `AgentMesh.Application`, each `AgentMesh.Api` deployment loads and runs one custom pipeline over HTTP, `AgentMeshCLI` calls the API without hosting pipeline execution, and multiple pipelines in one cluster are isolated in separate API workloads rather than composed in one process.

#### Scenario: Contributor selects an extension boundary
- **WHEN** a contributor evaluates where to add a domain entity, infrastructure integration, custom pipeline, runtime host behavior, or terminal interaction
- **THEN** the architecture documentation identifies the responsible project and directs each deployable pipeline to its own API workload and Service

### Requirement: Architecture documentation SHALL define cluster-level multi-pipeline deployment
The architecture documentation SHALL recommend one pipeline plugin per API pod and Service when multiple pipelines are required in the same Kubernetes cluster. It SHALL describe assigning pipeline-specific labels or tags to workloads and Services and routing all messages that require the same pipeline to the corresponding Service.

#### Scenario: Operator plans multiple pipelines in one cluster
- **WHEN** an operator reads the deployment guidance for hosting multiple pipelines
- **THEN** the documentation directs them to deploy separate API workloads and Services per pipeline instead of loading multiple pipeline plugins into one pod

#### Scenario: Operator plans pipeline request routing
- **WHEN** an operator needs to route requests to a particular pipeline
- **THEN** the documentation explains that upstream Kubernetes-aware routing uses the pipeline identity to select the corresponding Service and that clients call that deployment's unnamed request endpoint

### Requirement: Architecture documentation SHALL define the plugin deployment lifecycle
The architecture documentation SHALL state that the official API Docker image includes no default pipeline plugin, that plugin folder changes are not detected by running pods, and that a Kubernetes rollout restart is required after a plugin is added or replaced. It SHALL also describe how pipeline developers can automate plugin delivery and rollout restart in a deployment chain.

#### Scenario: Operator deploys the API image without a plugin
- **WHEN** a DevOps engineer deploys the official API Docker image without mounting or adding a plugin
- **THEN** the documentation explains that no default plugin is present and the API starts without a pipeline

#### Scenario: Operator drops a plugin into the folder
- **WHEN** a DevOps engineer adds or replaces a plugin in the configured folder of an existing deployment
- **THEN** the documentation states that nothing is loaded automatically and instructs the engineer to run `kubectl rollout restart deployment/<deployment-name>` before the API can load the plugin in a new pod

#### Scenario: Developer automates pipeline deployment
- **WHEN** a developer wants committed pipeline changes to be deployed automatically
- **THEN** the documentation directs them to create a deployment chain that builds and publishes the plugin, makes it available to the deployment, runs `kubectl rollout restart deployment/<deployment-name>`, and verifies that the rollout completes successfully

### Requirement: The architecture documentation SHALL describe runtime request flow
The architecture documentation SHALL describe the end-to-end runtime flow for user request handling and summarization behavior.

#### Scenario: Request lifecycle is traceable
- **WHEN** a contributor follows the runtime flow section
- **THEN** they can trace request handling from console input through pipelines and step execution to final answer generation

### Requirement: The architecture documentation SHALL define integration boundaries and extension points
The architecture documentation SHALL identify external service boundaries and the supported extension mechanisms.

#### Scenario: Integration and customization paths are clear
- **WHEN** a contributor plans an extension
- **THEN** they can identify relevant boundary contracts and extension points before implementation