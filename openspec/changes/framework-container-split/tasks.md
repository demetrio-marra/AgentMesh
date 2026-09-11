## 1. Framework contract extraction and packaging

- [ ] 1.1 Define target framework package boundaries (core abstractions, plugin bootstrap contract, immutable API DTO contracts) and verify the mapping is documented in `openspec/changes/framework-container-split/design.md` decisions and reflected in solution/project planning notes.
- [ ] 1.2 Update framework contracts to support multi-pipeline naming (`IChatRequestPipeline.Name`) and verify `dotnet build` succeeds for affected projects.
- [ ] 1.3 Align `IKnowledgeService`, `IRerankerService`, and `IJSSandbox` contract placement with framework package boundaries and verify all referencing projects compile without contract duplication.
- [ ] 1.4 Configure NuGet package metadata/output for framework project(s) and verify `dotnet pack` produces expected package artifacts.

## 2. Plugin startup loading and self-registration

- [ ] 2.1 Implement plugin bootstrap contract and host startup loader for `Plugins/` folder assemblies and verify startup logs confirm plugin discovery and bootstrap invocation.
- [ ] 2.2 Ensure plugin composition executes only at startup (no runtime watch/reload) and verify replacing plugin DLLs at runtime has no effect until restart.
- [ ] 2.3 Add startup validation state for plugin loading failures and duplicate pipeline names (case-insensitive) and verify host remains running while marking plugin state invalid.

## 3. Multi-pipeline registry and request routing

- [ ] 3.1 Build case-insensitive pipeline registry keyed by `IChatRequestPipeline.Name` and verify lookups succeed across name casing variants.
- [ ] 3.2 Add named pipeline endpoint route (`POST /api/pipelines/{pipelineName}/requests`) and verify a valid pipeline name executes and returns `WorkflowResult`.
- [ ] 3.3 Implement not-found handling for named route and verify unknown names return `404 Not Found`.
- [ ] 3.4 Implement default route behavior (`POST /api/requests`) for zero/one/many pipelines and verify: one pipeline executes, zero pipelines returns `503` with `No pipelines loaded`, many pipelines returns `400` with `pipeline name is required`.

## 4. RFC7807 plugin error model and security posture

- [ ] 4.1 Standardize plugin-related failures as RFC7807 `ProblemDetails` responses and verify response payloads include status/title/detail fields.
- [ ] 4.2 Ensure plugin-related responses are generic and include redeploy guidance where applicable, and verify responses do not expose plugin file paths, assembly names, or internal type details.
- [ ] 4.3 Keep full diagnostics in host logs only and verify operational logs contain enough detail to identify plugin loading and duplication root causes.

## 5. Containerization, examples, and compatibility checks

- [ ] 5.1 Add/update container deployment documentation for `Plugins/` volume mounting and verify documented examples cover Docker and Kubernetes startup-time plugin loading.
- [ ] 5.2 Validate API contract compatibility (immutable request/response DTO schema) and verify OpenAPI still exposes the stable request body and `WorkflowResult` response.
- [ ] 5.3 Run end-to-end verification with single-pipeline and multi-pipeline plugin sets and verify all success and error routing scenarios pass as specified.