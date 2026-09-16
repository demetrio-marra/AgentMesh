## 1. Request/response DTOs

- [x] 1.1 Add `ProcessRequestAsyncApiInput` in `AgentMesh.Api/Models/Api/` with `Message`, `Conversation`, and the 5 optional callback URL properties (`WorkflowStartedCallbackUrl`, `WorkflowStepStartedCallbackUrl`, `WorkflowStepCompletedCallbackUrl`, `WorkflowCompletedCallbackUrl`, `WorkflowErrorCallbackUrl`), mirroring `ProcessRequestApiInput`'s existing fields.
- [x] 1.2 Implement the all-or-nothing callback URL validation (e.g. `IValidatableObject`) on `ProcessRequestAsyncApiInput` covering all 5 URLs, so 0 or 5 URLs pass validation and 1-4 URLs fail with a validation error.
- [x] 1.3 Add a response DTO/type carrying the generated `requestId` returned by the async endpoint.
- [x] 1.4 Add `ProcessRequestApiOutput` in `AgentMesh.Api/Models/Api/` wrapping `RequestId` (`Guid`) plus the existing `WorkflowResult` data, for use by both the synchronous and asynchronous response paths where applicable.

## 2. Callback payload contracts

- [x] 2.1 Define payload record/DTO types for the 5 callback events (`workflowStarted`, `workflowStepStarted`, `workflowStepCompleted`, `workflowCompleted`, `workflowError`), matching the shapes in design.md (`requestId` plus event-specific fields sourced from `EWDisplayParameterRecord` / `EWStepStatisticsRecord` / `WorkflowResult` / the caught exception's message).

## 3. Per-request notifier scoping

- [x] 3.1 Add a scoped `CallbackNotifierContext` type in `AgentMesh.Application` (e.g. `AgentMesh.Application/Models/Workflows/`) holding `requestId` and the 5 nullable callback URLs, registered with `AddScoped<CallbackNotifierContext>()` in `AgentMesh.Api/Program.cs`.
- [x] 3.2 Implement `CallbackWorkflowProgressNotifier` (replacing `DummyWorkflowProgressNotifier`) implementing `IWorkflowProgressNotifier`, using `CallbackNotifierContext` and `IHttpClientFactory`; each `Notify*` method is a no-op when callback URLs are not configured, otherwise POSTs the corresponding payload.
- [x] 3.3 Ensure callback POST failures (non-2xx, exceptions) are caught and logged without throwing, and without stopping subsequent steps.
- [x] 3.4 Delete `DummyWorkflowProgressNotifier.cs` once `CallbackWorkflowProgressNotifier` covers the no-op behavior for requests without callback URLs; confirm the codebase has no remaining references to the old type.
- [x] 3.5 Update `AgentMesh.Api/Program.cs` DI registration: change `IWorkflowProgressNotifier` from `AddSingleton<..., DummyWorkflowProgressNotifier>` to `AddScoped<..., CallbackWorkflowProgressNotifier>`, and register `CallbackNotifierContext` as scoped.
- [x] 3.6 Bump `<Version>` in `AgentMesh.Application.csproj` (the `AgentMesh.Framework.Application` package), run `dotnet pack AgentMesh.Application/AgentMesh.Application.csproj -c Release`, copy the produced `.nupkg` into `artifacts/packages/`, and bump the `AgentMesh.Framework.Application` `PackageReference` version in `AgentMesh.Api.csproj` to match, so the API host picks up the new `StatelessAppInstance`/`CallbackNotifierContext` members.

## 4. Background execution in StatelessAppInstance

- [x] 4.1 Add `StatelessAppInstance.ProcessRequestAsync(...)` that generates a `requestId` (`Guid.NewGuid()`), creates an execution scope, populates that scope's `CallbackNotifierContext` with the `requestId` and callback URLs, and returns the `requestId` synchronously to the caller before the workflow pipeline completes.
- [x] 4.2 Run the pipeline execution as a detached background `Task` from `ProcessRequestAsync`, disposing the execution scope when that task completes (success or failure), and ensure unhandled exceptions in the background task are caught and logged rather than crashing the host.
- [x] 4.3 After the pipeline completes in the background task, invoke the `workflowCompleted` callback (when configured) with the final `WorkflowResult`, since `NotifyWorkflowEnd()` alone does not carry the result.
- [x] 4.4 Catch exceptions thrown by the pipeline in the background task and, when a `workflowError` callback URL is configured, POST a payload containing the `requestId` and the exception's error message instead of the `workflowCompleted` payload.
- [x] 4.5 Refactor `ProcessRequestAsync` to resolve the target pipeline synchronously via `ResolvePipeline` before generating the `requestId` or creating the execution scope/background task, so `PipelineRoutingException` (no pipelines loaded, ambiguous pipeline name, pipeline not found, invalid plugin configuration) propagates synchronously to the caller instead of being caught inside the background task and reported via the `workflowError` callback.
- [x] 4.6 Pass the already-resolved pipeline into the background execution path (`RunInBackgroundAsync` / the shared execution helper) instead of re-resolving it there.

## 5. Controller endpoint

- [x] 5.1 Add a new async action (e.g. `POST /api/requests/async` and/or `POST /api/pipelines/{pipelineName}/requests/async`) on `RequestsController` that accepts `ProcessRequestAsyncApiInput`, calls `StatelessAppInstance.ProcessRequestAsync`, and returns an HTTP response (e.g. `202 Accepted`) containing the `requestId` without waiting for workflow completion.
- [x] 5.2 Ensure the new endpoint returns `400 Bad Request` when the callback URL validation fails (relying on ASP.NET Core's automatic model validation).
- [x] 5.3 Add XML doc comments and `[ProducesResponseType]` attributes consistent with the existing synchronous actions, and confirm Swagger/OpenAPI documentation is generated for the new endpoint and DTOs.
- [x] 5.4 Update `RequestsController.PostDefault` and `PostNamed` (and the shared `ExecuteRequest` helper) to generate a `Guid.NewGuid()` `requestId`, wrap the awaited `WorkflowResult` in `ProcessRequestApiOutput`, and change the actions' declared return type / `[ProducesResponseType]` from `WorkflowResult` to `ProcessRequestApiOutput`.
- [x] 5.5 Update the async actions' private `ExecuteRequestAsync` helper on `RequestsController` to catch `PipelineRoutingException` thrown synchronously by `StatelessAppInstance.ProcessRequestAsync` and translate it into the same `ProblemDetails` response (400/404/503) used by the synchronous endpoints, and add the corresponding `[ProducesResponseType]` attributes (404, 503) to the async actions.

## 6. End-to-end verification

- [x] 6.1 Confirm an async request with all 5 callback URLs pointing at a local test HTTP listener produces the `workflowStarted`/`workflowStepStarted`/`workflowStepCompleted`/`workflowCompleted` callbacks, each containing the same `requestId` returned by the endpoint, in the expected order (`workflowStarted` → `workflowStepStarted`/`workflowStepCompleted` pairs per step → `workflowCompleted`), with no `workflowError` callback for a successful run.
- [x] 6.2 Confirm existing synchronous endpoints (`POST /api/requests`, `POST /api/pipelines/{pipelineName}/requests`) are unaffected by the `IWorkflowProgressNotifier` lifetime change (still succeed, still produce no callback HTTP traffic) and now return a non-empty `requestId` in their response.
- [x] 6.3 Confirm that forcing the pipeline to throw for an async request with all 5 callback URLs configured results in a single `workflowError` callback containing the `requestId` and the error message, with no `workflowCompleted` callback sent for that request.
- [x] 6.4 Update any relevant README/API documentation describing available endpoints to mention the new async endpoint and its 5-callback contract (including `workflowError`), and the `requestId` now present on synchronous responses.
- [x] 6.5 Confirm that sending an async request when no pipelines are loaded, with an ambiguous pipeline name on the default route, or to an unknown named pipeline returns the synchronous HTTP error (503/400/404) immediately, does not return a `requestId`, and never triggers the `workflowError` callback even when all 5 callback URLs are configured.
