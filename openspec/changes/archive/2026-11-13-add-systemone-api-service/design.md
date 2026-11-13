## Context

See [proposal.md](proposal.md) for motivation and the infrastructure delta specification for behavior.

Observed implementation:
- `src/AgentMesh.Infrastructure.Cohere/CohereV1RerankerService.cs` uses an injected HttpClient, bearer authentication, snake-case JSON, infrastructure-local wire DTOs, and a contract service interface. Its result exposes prompt/completion usage only; neither its adapter nor configuration calculates monetary costs. SystemOne follows this layering and usage-only boundary, but not Cohere's success-shaped null fallbacks.
- Contracts already live in `AgentMesh.Contracts`, despite some legacy namespaces using `AgentMesh.Services`. New models and the interface use `AgentMesh.Contracts.Models.SystemOne` and `AgentMesh.Contracts.Services`, without renaming existing contracts.
- `ApplicationRuntime.RegisterCommonServices` owns common adapter registration, but adding SystemOne to it is outside the revised scope.
- `AppInstance.CalculateExecutionCosts` owns existing framework financial aggregation, outside Infrastructure. No monetary calculation/reporting, core pricing metadata, or aggregation changes are included.
- Configuration examples belong in connector documentation, not Runtime-packaged settings or active settings belonging to another repository.

The configured archived architecture baseline is absent. Current project metadata, implementation, the architecture main spec, and the later archived architecture documentation were inspected instead. Stale context and some main-spec host terminology conflict with today's Runtime/plugin architecture; this change follows observed ownership without silently migrating unrelated specs.

Public API evidence, accessed 2026-11-13:
- [SystemOne reference](https://docs.typesafe.ai/api/system-one): `POST /v1/systemone`, bearer auth, required model/state/questions, discriminated questions/answers, response model and usage.
- [Models and pricing](https://docs.typesafe.ai/models): Jev 1.14/1.13, USD 42/billion = USD 0.042/million input tokens, output free; aliases can move; state is ingested once for batched questions. Long-context pricing is unchanged but that mode is excluded.
- The reference defines billable `usage.input_tokens`; no currency cost is returned. It also defines image/Box and long-context features, explicitly excluded after user clarification.

## Goals / Non-Goals

**Goals:**
- Expose a reliable typed service boundary independent of chat-completion clients.
- Keep transport DTOs in Infrastructure and matching domain request/results and the service interface in Contracts.
- Return exact API usage without implementing any financial reporting.
- Deliver only a standalone adapter and its contract definitions, leaving consumer orchestration unchanged.

**Non-Goals:**
- An executor, pipeline step, parameter configuration, common/runtime registration, Application adapter reference, new REST route, or changes to existing settings/consumers.
- A generic provider registry, automatic model discovery, LLM-agent registration, new configuration-summary fields, or changes to external AMCodePipeline/Helm repositories.
- Pricing configuration, monetary cost models/calculation/reporting, workflow cost aggregation, or core statistics changes.

## Decisions

### 1. Standalone adapter and configuration

Create `src/AgentMesh.Infrastructure.SystemOne` targeting net8.0, nullable and implicit usings enabled, referencing Contracts only; follow the Cohere project and solution Infrastructure grouping. Do not add an Application project reference.

`SystemOneServiceConfiguration` uses section `SystemOneService` with `BaseUrl`, `ApiKey`, `Model`, and `TimeoutSeconds` (30 default), matching Cohere's configuration surface. Validate required values, absolute HTTP(S) base URL, and positive timeout. No temperature, prompt, LLM key, pricing, or monetary cost fields.

Implement `SystemOneService` with an injected HttpClient and configuration, like Cohere. Construction validates configuration without making an HTTP call. Consumers own any future registration; this change adds neither a DI extension nor common/runtime wiring. Normalize a configured base path to a trailing slash and use relative `v1/systemone` so prefixes survive. Documentation recommends HTTPS.

Alternative: register as a keyed chat client or add a fake `Agents` entry. Rejected because that misrepresents the protocol and creates prompt/temperature requirements.

### 2. Public typed Contracts and JSON unions

Define `ISystemOneService.EvaluateAsync(SystemOneInput, CancellationToken)` returning `SystemOneResult` in Contracts. Model selection is service configuration; the result exposes the actual response model and API usage. Place matching public input/result/usage and typed question/answer definitions in `AgentMesh.Contracts.Models.SystemOne`. Place wire request/response/usage/question/answer and provider-error DTOs in `AgentMesh.Infrastructure.SystemOne.Models`; the adapter maps between these matching representations. Contracts does not reference Infrastructure or expose its HTTP DTO types.

Use explicit Noul/Choice/Score question and answer hierarchies, with `type` discriminators on wire DTOs. Use .NET 8 System.Text.Json polymorphism rather than untyped `object` answers or three nullable fields that allow contradictory states. Validate unknown discriminators explicitly. Use `JsonElement` for documented structured JSON values, with owned/cloned lifetime so callers need not keep a JsonDocument alive.

Shape mapping:

| Shape | Representation / invariant |
|---|---|
| State | String, object, or array JsonElement; no null, number, boolean, undefined |
| Instructions | Optional string/object/array; null or omission permitted |
| Noul criteria | Optional true/false descriptions, independently optional and nullable |
| Choice criteria | Named string/object/array/null descriptions, 1-255 entries |
| Score criteria | Ordered string/object/array descriptions, 1-10 levels |
| Noul answer | Double yes probability, not a thresholded boolean |
| Choice answer | Choice key, double confidence, named double probabilities |
| Score answer | Double expected score, double confidence, string-keyed JSON legend, string-keyed double probabilities |
| Usage | Required nonnegative integer input/output token counts |

Use snake-case only for schema property names, never for user dictionary keys or nested JSON contents. Omit absent optional properties without collapsing explicit null descriptions. No arbitrary question-count cap or local token estimation.

Validate inputs before HTTP execution and results before domain mapping. Missing JSON fields must be detected independently of C# value-type defaults. Verify exact question key/type correlation, Choice selection/probability keys, Score legend/probability level keys, finite/ranged numeric values, and Score range 0 through level-count minus one. Probability sums are approximately one according to the docs; do not require exact floating-point equality or introduce an undocumented sum tolerance. Do not reject additive unknown response properties, but reject unsupported `type` values.

Alternative: expose only the wire DTOs as the entire service API. Rejected because callers would inherit transport conventions and could accidentally build invalid type/field combinations; explicit mapping makes validation and domain usage clearer.

### 3. Explicit HTTP errors, no automatic billable retries

Send one JSON evaluation and propagate cancellation through send/read. Raise explicit validation/configuration/response exceptions and HTTP failures, retaining sanitized status and 400/422 details. Do not emit raw state, credentials, or provider body previews into routine logs/public errors; validation locations and bounded safe messages are sufficient. Failures are explicit exceptions, never broad success fallbacks; no executor is involved.

Do not apply the LLM structured-response resilience policy. Automatic retries could replay a paid evaluation after an ambiguous timeout or malformed success. Rate-limit/transport failures remain visible for caller policy, and failed evaluations have no fabricated result or usage.

Alternative: mirror Cohere's null-to-empty/default-usage behavior. Rejected because missing required usage must not masquerade as a valid zero-token result.

### 4. Usage-only reporting follows Cohere

Return `usage.input_tokens` and `usage.output_tokens` in the public result without estimating, pricing, or aggregating them. No cost model, tariff configuration, financial helper, or global usage counter is introduced. Matching wire/public usage models allow a future consumer to use the data without requiring any framework change now.

Alternative: calculate per-call costs inside the adapter or add workflow accounting. Rejected by the revised scope and the user's explicit instruction to follow Cohere's infrastructure boundary.

### 5. Tests and documentation

Use existing xUnit/.NET 8 infrastructure and deterministic HttpMessageHandler fakes. Add a SystemOne infrastructure test project; cover public Contracts consumption, wire serialization/shape validation, adapter mapping/protocol/errors, configuration validation, and request-local usage. No Application/core/Runtime/frontend tests or changes are needed. No credentials, live paid calls, new SDK, or broad test tooling.

Update README connector/project/configuration guidance with a direct-construction example, placeholders, base URL `https://api.typesafe.ai`, and model `jev-1.14.0`. Document reported usage, fractional scores, alias drift, excluded features, and that orchestration/registration and all monetary reporting are outside scope. The API pricing research remains background evidence only, not a configuration or reporting feature.

## Risks / Trade-offs

- [Aliases change] -> Preserve the actual response model and recommend pinning a model/version.
- [Strict response validation rejects incompatible providers] -> Enforce documented required fields and semantic correlation, tolerate additive properties, and test realistic JSON fixtures.
- [A request fails after being billed] -> Do not replay automatically; financial reporting is outside this connector's scope.
- [Generic reuse is limited to one configured endpoint/model per deployment] -> Match Cohere's configuration shape; multiple simultaneous providers are a separate requirement, not a speculative registry.
- [Missing historical baseline] -> Use inspected current ownership; do not edit unrelated stale architecture artifacts.

## Migration Plan

1. Add matching Contracts definitions, standalone adapter, and targeted tests.
2. Document direct construction/configuration and the usage-only boundary.
3. Build affected projects and run targeted tests, verifying protocol fidelity, mapping, errors, and exact usage.
4. Leave Application, Runtime, core statistics, consumer registration, and pipeline scheduling untouched; no data migration or automatic API call is introduced.

Rollback consists of reverting the standalone adapter and its Contracts/documentation additions. Existing workflows retain their original behavior throughout.
