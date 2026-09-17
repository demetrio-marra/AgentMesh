## Purpose

Lets operators of the `AgentMesh.Api` host retrieve the same sandbox and agent configuration summary that the CLI prints at startup, through an authenticated HTTP endpoint instead of console access.

## Requirements

### Requirement: Configuration summary endpoint
The API SHALL expose a `GET` endpoint that returns a structured configuration summary containing sandbox service URL, sandbox name, configured agent id, and the list of configured agents with, for each agent, its unique role, provider name, provider model name, model temperature, cost per million input tokens, cost per million output tokens, and optional cost per hour.

#### Scenario: Successful retrieval
- **WHEN** an authenticated client sends `GET` to the configuration summary endpoint
- **THEN** the API responds with `200 OK` and a JSON body containing the sandbox URL, sandbox name, agent id, and one entry per configured agent with role, provider, model name, temperature, cost per million input tokens, cost per million output tokens, and cost per hour when configured

### Requirement: Authentication required
The configuration summary endpoint SHALL require the same API key authentication scheme as other endpoints in `AgentMesh.Api`.

#### Scenario: Missing or invalid API key
- **WHEN** a client sends `GET` to the configuration summary endpoint without a valid API key
- **THEN** the API responds with `401 Unauthorized` and returns no configuration data

### Requirement: Secrets excluded from response
The configuration summary response SHALL NOT include provider API keys, provider endpoints, system prompts, raw LLM class names, or any other secret or internal-only configuration value, even though some of these values exist in the underlying agent configuration records.

#### Scenario: Response omits sensitive fields
- **WHEN** an authenticated client retrieves the configuration summary
- **THEN** the response body contains no field carrying a provider API key, provider endpoint, agent system prompt, or raw LLM class name

### Requirement: CLI startup SHALL use the API configuration summary
The CLI SHALL retrieve its startup display data from the authenticated API configuration summary endpoint instead of reading server runtime configuration locally.

#### Scenario: Configuration summary succeeds
- **WHEN** the CLI starts and the API returns an authorized configuration summary
- **THEN** the CLI displays the sandbox, user agent, and configured agent information from that response

#### Scenario: Configuration summary is unavailable
- **WHEN** the API rejects or cannot serve the configuration summary
- **THEN** the CLI reports that startup configuration could not be retrieved and does not display locally sourced server configuration
