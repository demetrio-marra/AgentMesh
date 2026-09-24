## Context

See proposal.md - Why. Today `RequestsController` (`AgentMesh.Api`) exposes only synchronous endpoints (`POST /api/requests`, `POST /api/pipelines/{pipelineName}/requests`) that call `StatelessAppInstance.ProcessRequest(...)` and await the full `WorkflowResult` before responding. `IWorkflowProgressNotifier` is registered as a single API-wide singleton (`DummyWorkflowProgressNotifier`, a no-op) in `AgentMesh.Api/Program.cs`, and is resolved from the scope that `StatelessAppInstance` creates per request via `serviceProvider.CreateScope()`. There is currently no per-request state associated with the notifier - it cannot know which `requestId` or callback URLs apply to the request it is reporting progress for.

## Goals / Non-Goals

**Goals:**
- Add an async endpoint + DTO that returns a `requestId` immediately and runs the workflow in the background.
- Deliver the 4 progress events plus a `workflowError` failure event as HTTP POST callbacks carrying the `requestId`, scoped correctly per concurrent request.
- Replace `DummyWorkflowProgressNotifier` with a real implementation without breaking the no-op behavior for synchronous requests or async requests without callback URLs.
- Make `requestId` exist uniformly across all request modes: also generate one for synchronous requests and include it in their response, even though synchronous requests never carry callback URLs.

**Non-Goals:**
- Retry/backoff policy, delivery guarantees, authentication of callback endpoints, or persistence/tracking of `requestId` status (e.g. a `GET /api/requests/{requestId}` status endpoint) are out of scope for this change.
- No changes to `AgentMeshCLI`/`ConsoleWorkflowProgressNotifier` or interactive mode.
- No change to the synchronous endpoints' request contract (`ProcessRequestApiInput` is untouched); only their response shape changes to add `requestId`.

## Decisions

### Background execution: fire-and-forget within a manually created scope
`StatelessAppInstance.ProcessRequest` already creates a `DI` scope (`serviceProvider.CreateScope()`) per request and disposes it once the pipeline finishes (`using var executionScope = ...`). For the async endpoint, the controller action cannot await this call. Instead, add a new method, e.g. `StatelessAppInstance.ProcessRequestAsync(...)`, that:
- resolves the target pipeline synchronously via the same `ResolvePipeline` used by the synchronous flow, before generating the `requestId` or starting any background work; a `PipelineRoutingException` (no pipelines loaded, ambiguous pipeline name, pipeline not found, invalid plugin configuration) thrown here propagates synchronously out of `ProcessRequestAsync` to the controller action, which translates it into the same `400`/`404`/`503` `ProblemDetails` response used by the synchronous endpoints - it is never delivered through the `workflowError` callback, and no execution scope or background task is created for this failure,
- once the pipeline is resolved, generates the `requestId` (`Guid.NewGuid()`),
- creates its own scope explicitly (not disposed via `using` in the calling method's synchronous frame),
- starts pipeline execution - using the already-resolved pipeline instance, not re-resolving it - as a detached `Task` (not awaited by the controller action),
- disposes the scope when the detached task completes (success or failure),
- returns the `requestId` synchronously to the controller so the HTTP response (`202 Accepted`) can be sent immediately.

Only failures that occur once the resolved pipeline is actually executing (i.e. inside the detached background task) are reported via the `workflowError` callback; pipeline-routing failures are a pre-condition of accepting the request at all and are therefore always synchronous, mirroring the synchronous endpoints' behavior.

Alternative considered: `IHostedService`/background queue (e.g. `Channel<T>` + `BackgroundService`). Rejected for this change because it adds infrastructure (a durable queue, worker lifetime management) disproportionate to the stated scope; a detached task fulfills "runs in the background, does not block the caller" without new hosted infrastructure. This can be revisited if reliability/backpressure requirements emerge later.

### Per-request notifier scoping: scoped `IWorkflowProgressNotifier` + a scoped context object
`IWorkflowProgressNotifier` changes from `AddSingleton` to `AddScoped` in `AgentMesh.Api/Program.cs`. A new scoped context type (e.g. `CallbackNotifierContext`) holds the `requestId` and the 5 callback URLs (nullable) for the current scope. `AgentMesh.Api` consumes `AgentMesh.Application` only through the versioned local NuGet package `AgentMesh.Framework.Application` (`PackageReference`, not a `ProjectReference`), so `CallbackNotifierContext` is defined in `AgentMesh.Application` (e.g. `AgentMesh.Application/Models/Workflows/CallbackNotifierContext.cs`) rather than in `AgentMesh.Api` - this lets `StatelessAppInstance` (also in `AgentMesh.Application`) reference and populate it directly. It is registered as `AddScoped<CallbackNotifierContext>()` in `AgentMesh.Api/Program.cs` and populated by `StatelessAppInstance.ProcessRequestAsync` immediately after creating the execution scope, before the pipeline runs, using the scope's own `IServiceProvider` (`executionScope.ServiceProvider.GetRequiredService<CallbackNotifierContext>()`).

Because `AgentMesh.Api` only picks up `AgentMesh.Application` changes through that package, adding members to `StatelessAppInstance` and adding `CallbackNotifierContext` requires bumping `<Version>` in `AgentMesh.Application.csproj`, running `dotnet pack AgentMesh.Application/AgentMesh.Application.csproj -c Release`, copying the produced `.nupkg` into `artifacts/packages/`, and bumping the `AgentMesh.Framework.Application` `PackageReference` version in `AgentMesh.Api.csproj` to match. `AgentMeshCLI` and the sample plugins are not required to bump in lockstep unless they need the new APIs.

The new `CallbackWorkflowProgressNotifier` (replacing `DummyWorkflowProgressNotifier`) is constructed with `CallbackNotifierContext` and an `HttpClient` (via `IHttpClientFactory`). On each `Notify*` call:
- if the context has no callback URLs configured (synchronous requests, or async requests without callbacks), it is a no-op, matching current behavior;
- otherwise it POSTs a JSON payload to the corresponding URL, always including `requestId`.

Alternative considered: keep `IWorkflowProgressNotifier` as a singleton and pass a per-call context object through every `Notify*` method signature. Rejected because it changes the shared `IWorkflowProgressNotifier` interface signature, which would also force an unrelated change to `ConsoleWorkflowProgressNotifier` and every pipeline step call site that invokes the notifier; scoping the notifier itself keeps the interface untouched and confines the change to the API host's DI wiring and `StatelessAppInstance`.

Alternative considered: `AsyncLocal<T>` ambient context instead of a scoped DI object. Rejected because the codebase already uses DI scopes per request (`CreateScope()`), and a scoped service is simpler to reason about and test than ambient async-local state, especially once the pipeline execution is detached onto its own `Task`.

### Callback delivery: best-effort, fire-and-forget HTTP POST per event
Each callback POST is awaited within the background task (so ordering between `workflowStepStarted`/`workflowStepCompleted` calls for the same step is preserved) but failures (network errors, non-2xx responses) are caught, logged, and do not fault the workflow execution or stop subsequent steps/callbacks.

### Payload shapes
Mirroring `IWorkflowProgressNotifier`, each POST body is a small JSON object:
- `workflowStarted`: `{ requestId }`
- `workflowStepStarted`: `{ requestId, stepName, inputParameters: [{ name, value }] }` (from `EWDisplayParameterRecord`)
- `workflowStepCompleted`: `{ requestId, stepName, elapsed, isAgentic, parametersDiff: [{ name, oldValue, newValue }] }` (from `EWStepStatisticsRecord` / `ParametersDiff`)
- `workflowCompleted`: `{ requestId, result }` where `result` is the `WorkflowResult` produced once the pipeline finishes (only available at the very end, so it is attached to the `workflowCompleted` callback rather than to `NotifyWorkflowEnd()`'s no-argument signature - the notifier itself does not have `WorkflowResult` when `NotifyWorkflowEnd()` is invoked from inside the pipeline. `StatelessAppInstance.ProcessRequestAsync` performs the final `workflowCompleted` POST itself after the pipeline call returns, instead of relying solely on `NotifyWorkflowEnd()`).
- `workflowError`: `{ requestId, errorMessage }` where `errorMessage` is the exception's message caught by the background task in `StatelessAppInstance.ProcessRequestAsync`. This callback has no counterpart on `IWorkflowProgressNotifier` (the interface has no error-notification method); it is posted directly by `ProcessRequestAsync`'s background-task catch block when the pipeline throws, and it is mutually exclusive with `workflowCompleted` for a given request - the background task tries the `workflowCompleted` POST on success or the `workflowError` POST on failure, never both.

### Validation of the all-or-nothing callback rule
Implemented as a data-annotation-friendly custom validation (e.g. `IValidatableObject` on `ProcessRequestAsyncApiInput`, consistent with the existing `[Required]` usage on `ProcessRequestApiInput.Message`) so the `400 Bad Request` is produced by the standard ASP.NET Core model validation pipeline rather than manual checks inside the controller action. All 5 callback URL properties (including `WorkflowErrorCallbackUrl`) participate in the same all-or-nothing rule.

### Uniform `requestId` on synchronous responses via a wrapper DTO
`WorkflowResult` (`AgentMesh.Application/Models/Workflows/WorkflowResult.cs`) is a shared `readonly record struct` also returned by `AgentMeshCLI`/interactive mode, which has no notion of a per-HTTP-request id. Rather than adding an API-only `RequestId` field to that shared model, `AgentMesh.Api` introduces a new response DTO, e.g. `ProcessRequestApiOutput`, containing `RequestId` (`Guid`) plus the existing `WorkflowResult` (or its flattened fields). `RequestsController.PostDefault`/`PostNamed` generate a `Guid.NewGuid()` for the synchronous flow the same way the async path does, await `StatelessAppInstance.ProcessRequest(...)` as today, and wrap the result before returning. No callback URLs or notifier scoping are involved for the synchronous path - the `requestId` here exists purely for response uniformity with the async endpoint, not for callback correlation.

Alternative considered: add `RequestId` directly to `WorkflowResult`. Rejected because `WorkflowResult` is shared with `AgentMeshCLI`, which has no concept of an HTTP request id; adding it there would leak an API-only concept into a cross-host model and force every non-API caller to populate a meaningless value.

## Risks / Trade-offs

- [Detached `Task` execution is not tracked or awaited by the host] → Acceptable for this change's scope (no delivery/status guarantees promised); document as a known limitation. A future change can add tracking/persistence if needed.
- [Process restart or crash during background execution loses in-flight requests, with no callback ever sent] → Out of scope per Non-Goals; callers must treat callbacks as best-effort.
- [Callback endpoints could be slow or unreachable, delaying step-by-step delivery ordering within a single request] → Each callback awaited sequentially within the background task keeps ordering correct for a given request; a slow callback endpoint only delays that request's own subsequent steps, not other requests (separate scopes/tasks).
- [Widening `IWorkflowProgressNotifier` from singleton to scoped changes its lifetime for `AgentMesh.Api` only] → `AgentMeshCLI` keeps its own singleton registration; the interface contract is unchanged so this is confined to `AgentMesh.Api/Program.cs` and `AgentMesh.Api/Services`.
- [Changing the synchronous endpoints' response type from `WorkflowResult` to a wrapper DTO is a breaking contract change for existing API consumers] → Necessary to keep `requestId` present uniformly across sync and async responses; the wrapper still exposes all existing `WorkflowResult` data, so consumers only need to adjust the top-level shape (e.g. read `result.WorkflowResult` or flattened fields) rather than losing any data.
