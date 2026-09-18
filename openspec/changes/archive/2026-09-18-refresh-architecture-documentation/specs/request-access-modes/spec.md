## ADDED Requirements

### Requirement: Request-access documentation SHALL distinguish host and framework execution
Documentation for request access modes SHALL identify `AgentMesh.Api` as the REST host that enforces request access and `AgentMesh.Application` as the reusable framework that executes custom agentic pipelines.

#### Scenario: Contributor reviews request processing ownership
- **WHEN** a contributor reads request access mode behavior
- **THEN** they can distinguish REST access enforcement from reusable pipeline execution
