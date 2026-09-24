## ADDED Requirements

### Requirement: Configuration-summary documentation SHALL identify configuration ownership
Documentation for the configuration summary SHALL identify `AgentMesh.Api` as the configuration-owning host and `AgentMeshCLI` as the REST client that consumes the sanitized summary.

#### Scenario: Contributor reviews configuration flow
- **WHEN** a contributor reads configuration-summary behavior
- **THEN** they can identify the API host as the source and the CLI as the consumer of the summary
