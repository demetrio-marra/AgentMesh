## ADDED Requirements

### Requirement: Console hosting SHALL be an API frontend
The console entry point SHALL own only interactive input, in-memory conversation state, HTTP transport, callback reception, and console presentation; pipeline execution and runtime service configuration SHALL remain owned by the API entry point.

#### Scenario: Console starts independently of pipeline runtime
- **WHEN** the CLI starts with a reachable API configuration
- **THEN** it can initialize its interactive frontend without loading the API pipeline implementation locally

### Requirement: Runtime configuration SHALL be merged into API-owned files
The API SHALL own configuration files containing the shared runtime sections currently supplied by the CLI, while preserving API-only authentication and hosting sections. The CLI SHALL retain only settings required to connect to the API, receive callbacks, and manage local conversation summarization. This relocation SHALL NOT change API endpoints or processing behavior.

#### Scenario: API starts from its own configuration
- **WHEN** the API is deployed without the CLI project files
- **THEN** it loads the merged shared runtime configuration and its API-only settings from its own files

#### Scenario: API-only configuration is preserved
- **WHEN** shared CLI configuration is merged into API configuration
- **THEN** API authentication and hosting settings remain present and effective

#### Scenario: API behavior remains unchanged
- **WHEN** the configuration files are relocated and merged
- **THEN** existing API routes, authentication behavior, callbacks, and pipeline processing remain unchanged
