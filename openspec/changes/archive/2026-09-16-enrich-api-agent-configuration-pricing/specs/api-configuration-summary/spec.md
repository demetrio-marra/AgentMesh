## MODIFIED Requirements

### Requirement: Configuration summary endpoint
The API SHALL expose a `GET` endpoint that returns a structured configuration summary containing sandbox service URL, sandbox name, configured agent id, and the list of configured agents with, for each agent, its unique role, provider name, provider model name, model temperature, cost per million input tokens, cost per million output tokens, and optional cost per hour.

#### Scenario: Successful retrieval
- **WHEN** an authenticated client sends `GET` to the configuration summary endpoint
- **THEN** the API responds with `200 OK` and a JSON body containing the sandbox URL, sandbox name, agent id, and one entry per configured agent with role, provider, model name, temperature, cost per million input tokens, cost per million output tokens, and cost per hour when configured

### Requirement: Secrets excluded from response
The configuration summary response SHALL NOT include provider API keys, provider endpoints, system prompts, raw LLM class names, or any other secret or internal-only configuration value, even though some of these values exist in the underlying agent configuration records.

#### Scenario: Response omits sensitive fields
- **WHEN** an authenticated client retrieves the configuration summary
- **THEN** the response body contains no field carrying a provider API key, provider endpoint, agent system prompt, or raw LLM class name