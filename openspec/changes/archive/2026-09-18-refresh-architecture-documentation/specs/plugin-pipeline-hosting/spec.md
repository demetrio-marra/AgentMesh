## ADDED Requirements

### Requirement: Plugin-hosting documentation SHALL define package responsibilities
Documentation for plugin hosting SHALL identify `AgentMesh` as domain entities, `AgentMesh.Contracts` as infrastructure service definitions, `AgentMesh.Infrastructure.*` as external adapters, `AgentMesh.Application` as the plugin-facing framework, and `AgentMesh.Api` as the custom-pipeline host.

#### Scenario: Plugin author reviews layering
- **WHEN** a plugin author reads plugin-hosting documentation
- **THEN** they can choose the correct dependency boundary for framework extensions and external integrations
