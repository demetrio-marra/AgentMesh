## Purpose

Lets operators of the `AgentMesh.Api` host retrieve the same sandbox and agent configuration summary that the CLI prints at startup, through an authenticated HTTP endpoint instead of console access.

## Requirements

### Requirement: Configuration summary endpoint
The API SHALL expose a `GET` endpoint that returns a structured configuration summary equivalent to the CLI startup printout: sandbox service URL, sandbox name, configured agent id, and the list of configured agents with, for each agent, its unique role, provider model name, and temperature.

#### Scenario: Successful retrieval
- **WHEN** an authenticated client sends `GET` to the configuration summary endpoint
- **THEN** the API responds with `200 OK` and a JSON body containing the sandbox URL, sandbox name, agent id, and one entry per configured agent with role, model name, and temperature

### Requirement: Authentication required
The configuration summary endpoint SHALL require the same API key authentication scheme as other endpoints in `AgentMesh.Api`.

#### Scenario: Missing or invalid API key
- **WHEN** a client sends `GET` to the configuration summary endpoint without a valid API key
- **THEN** the API responds with `401 Unauthorized` and returns no configuration data

### Requirement: Secrets excluded from response
The configuration summary response SHALL NOT include provider API keys, system prompts, or any other secret or sensitive configuration value, even though these values exist in the underlying agent configuration records.

#### Scenario: Response omits sensitive fields
- **WHEN** an authenticated client retrieves the configuration summary
- **THEN** the response body contains no field carrying a provider API key or an agent system prompt
