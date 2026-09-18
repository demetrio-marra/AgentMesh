## ADDED Requirements

### Requirement: Summarization documentation SHALL distinguish host and framework roles
Documentation for API summarization SHALL identify `AgentMesh.Api` as the request host, `AgentMesh.Application` as the reusable pipeline framework, and `AgentMeshCLI` as the REST frontend that requests context reduction.

#### Scenario: Contributor traces summarization ownership
- **WHEN** a contributor reads summarization API behavior
- **THEN** they can distinguish request hosting, pipeline execution framework, and terminal-client responsibilities
