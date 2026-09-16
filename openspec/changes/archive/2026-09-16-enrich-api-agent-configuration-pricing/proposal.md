## Why

`AgentMesh.Api` now exposes an authenticated configuration summary endpoint, but its per-agent entries only include role, model, and temperature. Operators also need to understand which inference provider each agent uses and the configured pricing metadata from `LLMs` in `appsettings`, especially when comparing API deployments or estimating runtime cost behavior without direct access to the host configuration files.

The flattened agent configuration already contains provider and pricing fields, so this change expands the API response contract without changing how configuration is loaded.

## What Changes

- Extend each agent entry returned by `GET api/configuration` with provider name, cost per million input tokens, cost per million output tokens, and optional hourly cost.
- Keep the endpoint read-only and protected by the existing API key authentication scheme.
- Preserve explicit allow-list mapping so provider API keys, provider endpoints, system prompts, and other sensitive/internal configuration remain excluded.
- Update API response documentation and focused tests for the enriched shape.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `api-configuration-summary`: Enrich per-agent configuration summary data with provider and non-sensitive LLM pricing metadata loaded from appsettings.

## Impact

- Affected code: `AgentMesh.Api/Models/Api/AgentConfigurationSummaryApiOutput.cs`, `AgentMesh.Api/Controllers/ConfigurationController.cs`, and any focused API tests or OpenAPI expectations covering the configuration response.
- Existing clients will receive additional JSON fields on agent entries; existing fields and route behavior remain unchanged.
- No changes to configuration binding or DI are expected because `AgentFlatConfigurationRecord` already exposes `ProviderName`, `LLMClassCostPerMillionInputTokens`, `LLMClassCostPerMillionOutputTokens`, and `LLMClassCostPerHour`.

## Non-goals

- No exposure of provider API keys, system prompts, provider endpoints, or raw LLM class names.
- No changes to CLI startup output.
- No changes to cost calculation logic or token usage accounting.
- No write/update endpoint for configuration.