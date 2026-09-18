## ADDED Requirements

### Requirement: Plugin documentation SHALL identify the framework package
Documentation for the default chat pipeline plugin SHALL identify `AgentMesh.Application` as the reusable plugin-facing framework package, with `AgentMesh.Api` as the host that runs deployed custom pipelines.

#### Scenario: Plugin author selects dependencies
- **WHEN** a plugin author reads the default pipeline plugin documentation
- **THEN** they can identify the framework package to target and the host responsible for pipeline execution
