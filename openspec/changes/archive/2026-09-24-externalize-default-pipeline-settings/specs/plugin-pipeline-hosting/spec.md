## ADDED Requirements

### Requirement: Host SHALL layer optional pipeline configuration at startup
The API host SHALL load `pipelineSettings.json` as an optional configuration source after its host settings and before environment variables. The pipeline settings SHALL preserve their established top-level section names, and absence of the file SHALL NOT prevent the API host from starting.

#### Scenario: Deployed pipeline configuration is loaded
- **WHEN** the API starts with `pipelineSettings.json` in its configuration base directory
- **THEN** values from the file are available to the loaded plugin under their established section names

#### Scenario: Pipeline configuration is absent
- **WHEN** the API starts without `pipelineSettings.json`
- **THEN** startup continues and existing missing or invalid plugin configuration handling determines pipeline availability

#### Scenario: Environment overrides pipeline configuration
- **WHEN** an environment variable and `pipelineSettings.json` define the same configuration key
- **THEN** the environment variable value takes precedence