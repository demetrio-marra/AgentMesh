## Context

See `proposal.md` for motivation. The current `GET api/configuration` endpoint maps `IEnumerable<AgentFlatConfigurationRecord>` into `AgentConfigurationSummaryApiOutput` entries containing only agent role, provider model name, and temperature. The same flattened record already includes provider name and pricing metadata resolved from the referenced LLM configuration, along with sensitive fields that must stay out of the HTTP response.

The architecture baseline keeps API DTOs in `AgentMesh.Api`, common configuration records in `AgentMesh`, and configuration loading in `AgentMesh.Application`. This change should preserve that layering.

## Goals / Non-Goals

**Goals:**
- Add non-sensitive provider and pricing fields to the per-agent API DTO.
- Map fields explicitly from the flattened configuration record in the controller.
- Preserve nullable hourly pricing exactly as configuration expresses it.
- Keep the endpoint route, authentication, and top-level response shape stable.

**Non-Goals:**
- Do not change configuration binding, `AgentFlatConfigurationRecord`, or `AgentConfigurationReadHelper` unless implementation discovers a mismatch with the already-present fields.
- Do not expose provider endpoint, API key, system prompt, or raw LLM class names.
- Do not add derived or calculated cost totals to this endpoint.

## Decisions

### Decision 1: Extend the existing per-agent output DTO

Add provider and pricing fields to `AgentConfigurationSummaryApiOutput` rather than introducing a nested pricing object.

Rationale: the existing agent entry is already a compact summary object, and these fields are direct attributes of the selected LLM/provider configuration. Keeping them flat minimizes response churn for clients.

Alternative considered: add a nested `Pricing` DTO. That gives more grouping but adds structure not present in current configuration records and is unnecessary for four scalar fields.

### Decision 2: Keep explicit allow-list mapping in the controller

Continue projecting each `AgentFlatConfigurationRecord` into the response DTO field by field.

Rationale: explicit mapping prevents accidental serialization of sensitive values already present on the source record, including provider API key and system prompt.

Alternative considered: return or serialize `AgentFlatConfigurationRecord` directly with ignored properties. That is more brittle because future fields could be exposed by default.

### Decision 3: Use decimal values for configured pricing

Expose `CostPerMillionInputTokens` and `CostPerMillionOutputTokens` as decimals and `CostPerHour` as nullable decimal in the API DTO.

Rationale: the configuration DTO and flattened record already model these values as decimal/nullable decimal, which avoids precision loss and preserves `null` for models without hourly pricing.

Alternative considered: convert pricing to double like temperature. Temperature is already a string-to-double API projection; pricing is money-like configuration and should keep decimal semantics.

## Risks / Trade-offs

- [Risk] Existing clients with strict JSON schema validation may reject additional fields. -> Mitigation: keep route and existing fields unchanged, and document this as an additive response change.
- [Risk] Future maintainers may confuse provider name with provider endpoint. -> Mitigation: name the DTO field `Provider` or `ProviderName` clearly and keep endpoint/internal details out of the mapping.
- [Risk] Hourly pricing can be absent. -> Mitigation: preserve `null` in the response instead of replacing it with zero.

## Migration Plan

1. Extend the per-agent API response DTO with provider and pricing fields.
2. Update `ConfigurationController.Get` to map those fields from `AgentFlatConfigurationRecord`.
3. Add or update focused tests/docs verifying the enriched fields and continued exclusion of sensitive/internal fields.
4. Rollback is limited to removing the added DTO fields and mapping if clients cannot accept the additive response.

## Open Questions

None.