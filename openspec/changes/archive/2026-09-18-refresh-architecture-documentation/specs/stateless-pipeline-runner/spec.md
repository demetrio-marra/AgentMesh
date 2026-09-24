## ADDED Requirements

### Requirement: Stateless-runner documentation SHALL identify framework ownership
Documentation for stateless pipeline execution SHALL identify `AgentMesh.Application` as the reusable framework package that provides pipeline execution for class-library plugins, with `AgentMesh.Api` hosting custom pipelines at runtime.

#### Scenario: Contributor reviews stateless execution
- **WHEN** a contributor reads stateless pipeline-runner behavior
- **THEN** they can identify the reusable framework boundary separately from the executable pipeline host