## Why

AgentMesh lacks a typed SystemOne decision-service connector. Adding a standalone infrastructure adapter and matching Contracts definitions lets consumers request structured judgments without treating the service as an LLM.

## What Changes

- Add the .NET 8 `AgentMesh.Infrastructure.SystemOne` adapter for `POST /v1/systemone`, with configurable provider base URL, API key, model, and timeout.
- Place HTTP transport DTOs/models in Infrastructure and matching public request/result/usage models and the service interface in `AgentMesh.Contracts`.
- Support text-only Noul, Choice, and Score, including mixed named questions and structured JSON state, instructions, and criteria.
- Return the response model, typed answers, and API-reported input/output token usage, following Cohere's usage-only infrastructure boundary.
- Document direct service configuration and usage; add deterministic contract and adapter tests.

## Capabilities

### New Capabilities

- `systemone-infrastructure`: Text-only SystemOne HTTP interface, typed contracts, provider configuration, validation, and errors.

### Modified Capabilities

None. Existing pipeline selection, HTTP endpoints, configuration-summary requirements, and LLM behavior remain unchanged.

## Impact

Affected surfaces are limited to `AgentMesh.Contracts`, the new infrastructure project, solution grouping, related tests, and README connector documentation. Application, core statistics, Runtime composition/settings, CLI, Web, and existing workflow financial reporting remain unchanged.

Cohere's infrastructure reports usage but does not calculate financial cost. This change follows that boundary: no pricing configuration, calculated cost models, cost reporting, or workflow aggregation is added.

## Non-goals

- Images, assets, multipart requests, Box questions, or long-context opt-in.
- Implementing model-discovery, other TypeSafe endpoints, SDK features, or LLM/system-prompt abstractions.
- Executors, pipeline steps, parameter configurations, runtime DI wiring, Application project references, and any monetary cost calculation or reporting.
- Adding a decision to the existing chat pipeline, altering Cohere, changing deployment repositories, or broad architectural cleanup.
