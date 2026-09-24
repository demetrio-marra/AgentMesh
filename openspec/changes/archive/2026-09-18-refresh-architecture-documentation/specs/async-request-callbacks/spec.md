## ADDED Requirements

### Requirement: Callback documentation SHALL identify API and CLI roles
Documentation for asynchronous callbacks SHALL identify `AgentMesh.Api` as the API host that delivers callbacks and `AgentMeshCLI` as a REST terminal frontend that can consume callback events.

#### Scenario: Contributor reviews callback ownership
- **WHEN** a contributor reads asynchronous callback behavior
- **THEN** they can identify which executable host delivers callback events and which frontend consumes them
