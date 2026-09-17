## ADDED Requirements

### Requirement: Pipeline plugins SHALL not depend on host-owned conversation state
Chat and summarization pipeline plugins SHALL receive conversation data through their existing initialization inputs and SHALL NOT require a host-owned conversation context or a stateful application runner.

#### Scenario: Plugin pipelines run with caller-supplied context
- **WHEN** a host invokes a loaded chat or summarization pipeline with supplied conversation messages
- **THEN** the plugin processes those inputs without requiring access to conversation state retained by the host