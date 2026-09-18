## ADDED Requirements

### Requirement: Architecture documentation SHALL define project boundaries
The architecture documentation SHALL identify `AgentMesh` as the basic domain-entity package, `AgentMesh.Contracts` as the infrastructure-service contract package, `AgentMesh.Infrastructure.*` as external-system adapters, `AgentMesh.Application` as the plugin-facing framework package, `AgentMesh.Api` as the executable host for custom pipelines, and `AgentMeshCLI` as the REST terminal frontend for the API. It SHALL describe the supported dependency direction and distinguish reusable packages from deployable hosts.

#### Scenario: Contributor selects an extension boundary
- **WHEN** a contributor evaluates where to add a domain entity, infrastructure integration, custom pipeline, runtime host behavior, or terminal interaction
- **THEN** the architecture documentation identifies the responsible project and the relevant package or host boundary
