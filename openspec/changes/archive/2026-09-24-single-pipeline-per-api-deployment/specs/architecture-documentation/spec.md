## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: The architecture documentation SHALL distinguish reusable and executable boundaries
The architecture documentation SHALL distinguish reusable domain, contract, adapter, and framework packages from executable hosts. It SHALL state that custom class-library plugins use `AgentMesh.Application`, each `AgentMesh.Api` deployment loads and runs one custom pipeline over HTTP, `AgentMeshCLI` calls the API without hosting pipeline execution, and multiple pipelines in one cluster are isolated in separate API workloads rather than composed in one process.

#### Scenario: Contributor selects an extension boundary
- **WHEN** a contributor evaluates where to add a domain entity, infrastructure integration, custom pipeline, runtime host behavior, terminal interaction, or additional cluster pipeline
- **THEN** the architecture documentation identifies the responsible project and directs each deployable pipeline to its own API workload and Service