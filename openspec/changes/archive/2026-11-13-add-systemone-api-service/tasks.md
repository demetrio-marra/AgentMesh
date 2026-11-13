## 1. Contracts and project structure

- [x] 1.1 Add the net8.0 `AgentMesh.Infrastructure.SystemOne` project referencing Contracts, its existing-pattern xUnit test project, and solution Infrastructure/Tests grouping, without an Application project reference; verify `dotnet sln AgentMesh.sln list` and the new project build. Budget: <=1 hour.
- [x] 1.2 Define public `ISystemOneService`, typed SystemOne input/result/usage and Noul/Choice/Score domain models in Contracts, with owned structured JSON values; verify a compile-time consumer and tests preserving JSON after source-document disposal. Budget: <=2 hours.
- [x] 1.3 Define matching transport request/response/usage/question/answer and sanitized provider-error DTOs in Infrastructure with snake-case schema fields and explicit discriminators; verify mixed-question serialization and all three answer fixtures, required-field detection, null-vs-omitted descriptions, fractional Score, unchanged user dictionary keys, and no Infrastructure dependency from Contracts. Budget: <=2 hours.

## 2. Infrastructure configuration and HTTP evaluation

- [x] 2.1 Implement `SystemOneServiceConfiguration` and constructor validation of base URL, credentials, model, and positive timeout, following Cohere without any pricing fields; verify invalid values fail before HTTP execution and valid configuration constructs the standalone service. Budget: <=1 hour.
- [x] 2.2 Implement request mapping/validation for string/object/array state, optional instructions, Noul criteria, Choice 1-255 entries, and Score 1-10 levels; verify exact boundaries, invalid JSON shapes, and rejection of unsupported features before sending. Budget: <=2 hours.
- [x] 2.3 Implement one authenticated JSON `POST v1/systemone` using injected HttpClient, prefix-preserving base URL normalization, timeout, and cancellation; verify fake-handler requests for two providers, trailing-slash variants, exact body/model, no image/long-context headers, and no additional endpoint calls. Budget: <=2 hours.
- [x] 2.4 Map validated typed answers, actual model, and required nonnegative API usage; verify Noul probabilities, Choice fields, fractional Score/legend/probabilities, key/type correlation, criterion keys, invalid ranges, missing usage/fields, unknown discriminators, and tolerated additive response properties. Budget: <=2 hours.
- [x] 2.5 Implement distinguishable HTTP/transport/timeout/cancellation/JSON failures with sanitized 400/422 details and no automatic retry; verify 400/401/422/429/500, malformed successes, cancellation, single-send behavior, and absence of state/credentials from logs and public errors. Budget: <=2 hours.

## 3. Usage-only boundary and documentation

- [x] 3.1 Verify adapter-to-contract mapping preserves API input/output usage, including nonzero and zero counts, and keeps concurrent evaluations' model/answers/usage independent; verify fake-handler tests and that no monetary price/cost fields or financial calculations exist. Budget: <=1 hour.
- [x] 3.2 Update README connector/project/configuration guidance with direct construction, placeholders, pinned TypeSafe model, text-only limits, and the usage-only boundary; verify examples match service/contract names and introduce no executor, step, DI wiring, pricing configuration, or automatic pipeline calls. Budget: <=1 hour.
- [x] 3.3 Run targeted SystemOne contract/adapter tests and build the affected projects; verify no errors, no live paid calls, and no implementation changes to Application, Runtime, core statistics, existing workflow accounting, or frontend consumers. Budget: <=1 hour.
- [x] 3.4 Validate the completed change with `openspec validate add-systemone-api-service --strict`; verify requirements/scenarios have matching tests and implementation scope remains only the standalone Infrastructure adapter/models, matching Contracts definitions, solution entries, related tests, and connector documentation. Budget: <=1 hour.
