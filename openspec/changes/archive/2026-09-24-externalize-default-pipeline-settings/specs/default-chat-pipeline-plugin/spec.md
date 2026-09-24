## ADDED Requirements

### Requirement: Default pipeline sample SHALL provide deployable pipeline configuration
The default pipeline sample SHALL provide a `pipelineSettings.json` configuration template containing only the `InferenceProviders`, `LLMs`, `Agents`, and `CodeModeWorkflow` sections at the JSON root. The file SHALL be copied to the sample plugin build output so an operator can deploy it with the plugin, and user documentation SHALL identify it as required configuration for operating the default pipeline.

#### Scenario: Sample plugin is built
- **WHEN** a developer builds the default pipeline sample plugin
- **THEN** `pipelineSettings.json` is present in the plugin build output with the four pipeline-owned configuration sections

#### Scenario: Operator configures the default pipeline
- **WHEN** an operator follows the documented default pipeline deployment instructions
- **THEN** the operator is directed to provide `pipelineSettings.json` with provider, model, agent, and workflow settings

#### Scenario: Host-only settings remain separate
- **WHEN** a developer inspects the default pipeline configuration template
- **THEN** it does not contain API host, authentication, plugin discovery, resilience, sandbox, memory, knowledge, reranker, or conversation-summarization settings