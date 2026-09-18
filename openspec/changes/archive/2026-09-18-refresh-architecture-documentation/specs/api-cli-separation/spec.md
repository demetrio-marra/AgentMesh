## ADDED Requirements

### Requirement: API and CLI separation documentation SHALL identify executable roles
Documentation for API and CLI separation SHALL identify `AgentMesh.Api` as the API host and `AgentMeshCLI` as its REST terminal frontend, without describing the CLI as a composition root for pipeline runtime services.

#### Scenario: Contributor reviews host responsibilities
- **WHEN** a contributor reads the API and CLI separation documentation
- **THEN** they can distinguish API-host responsibilities from REST-terminal responsibilities
