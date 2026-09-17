## 1. API Response Contract

- [x] 1.1 Extend `AgentConfigurationSummaryApiOutput` with provider, cost per million input tokens, cost per million output tokens, and nullable cost per hour fields; verify by building `AgentMesh.Api` successfully.
- [x] 1.2 Update `ConfigurationController.Get` to explicitly map the new DTO fields from `AgentFlatConfigurationRecord.ProviderName`, `LLMClassCostPerMillionInputTokens`, `LLMClassCostPerMillionOutputTokens`, and `LLMClassCostPerHour`; verify the mapping remains an allow-list and does not include provider endpoint, provider API key, system prompt, or raw LLM class.

## 2. Validation And Documentation

- [x] 2.1 Add or update focused API/controller tests for `GET api/configuration` showing the enriched per-agent fields, including `null` hourly cost when absent; verify the test fails before the mapping change and passes afterward.
- [x] 2.2 Add or update a focused assertion that sensitive/internal configuration fields are absent from the serialized response; verify the response JSON contains no provider endpoint, API key, system prompt, or raw LLM class field.
- [x] 2.3 Run `dotnet test` for the affected test project or the narrowest available solution slice; verify all relevant tests pass.

## 3. OpenSpec Verification

- [x] 3.1 Run `openspec validate enrich-api-agent-configuration-pricing --strict`; verify the change artifacts and delta spec pass validation.