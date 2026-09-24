## ADDED Requirements

### Requirement: CLI documentation SHALL identify the terminal frontend boundary
Documentation for the CLI API frontend SHALL identify `AgentMeshCLI` as a REST terminal frontend for `AgentMesh.Api` and SHALL not represent it as a pipeline-execution host.

#### Scenario: Contributor reviews CLI architecture
- **WHEN** a contributor reads the CLI API frontend documentation
- **THEN** they can identify the CLI as an HTTP client and the API as the pipeline runtime host
