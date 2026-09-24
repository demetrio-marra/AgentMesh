## Why

Callers that trigger long-running workflows via `POST /api/requests` must currently block on the full HTTP request until the workflow completes, with no way to observe intermediate progress. Some integrations need to fire-and-forget a request and receive progress/completion via webhooks instead of holding a connection open.

## What Changes

- Add a new asynchronous endpoint on `RequestsController` that accepts a request payload with 5 optional callback URLs (one per workflow progress event, including errors) and returns immediately with a generated `requestId` (GUID), without waiting for workflow completion.
- Add a new request DTO (e.g. `ProcessRequestAsyncApiInput`) with the same fields as `ProcessRequestApiInput` (`Message`, `Conversation`) plus 5 optional callback URL properties: `WorkflowStartedCallbackUrl`, `WorkflowStepStartedCallbackUrl`, `WorkflowStepCompletedCallbackUrl`, `WorkflowCompletedCallbackUrl`, `WorkflowErrorCallbackUrl`.
- Add validation: if any one of the 5 callback URLs is provided, all 5 must be provided, otherwise the request is rejected with `400 Bad Request`.
- Execute the workflow in the background (fire-and-forget from the HTTP request's perspective) after returning the `requestId` to the caller.
- If the background workflow execution fails, POST to the `workflowError` callback URL (when configured) with a payload containing the `requestId` and the error message, instead of the `workflowCompleted` callback.
- **BREAKING**: Replace `DummyWorkflowProgressNotifier` (a no-op singleton) with a per-request notifier implementation that performs HTTP POST calls to the 4 progress-event callback URLs as the corresponding `IWorkflowProgressNotifier` events occur (the 5th, `workflowError`, is posted directly by the background execution path on failure, not via the notifier interface), including the `requestId` in every callback payload. This requires `IWorkflowProgressNotifier` resolution to become request-scoped/contextual instead of a single no-op singleton for the API host.
- **BREAKING**: Also generate a `requestId` for the existing synchronous endpoints (`POST /api/requests`, `POST /api/pipelines/{pipelineName}/requests`) and include it in their response, so a `requestId` exists uniformly across both synchronous and asynchronous request modes. The synchronous endpoints' response type changes from the bare `WorkflowResult` to a new wrapper DTO (e.g. `ProcessRequestApiOutput`) carrying `RequestId` plus the existing `WorkflowResult` data.

## Capabilities

### New Capabilities
- `async-request-callbacks`: Asynchronous request submission endpoint that returns a `requestId` immediately and delivers workflow progress (including failures) via caller-supplied HTTP callback URLs.

### Modified Capabilities
- `request-access-modes`: The "Workflow progress notifier SHALL be mode-specific" requirement changes — API mode no longer always resolves a no-op notifier; when an async request with callback URLs is being processed, the notifier posts progress events to those URLs, scoped to the originating request. The "REST request endpoint SHALL invoke the existing request pipeline" requirement also changes — synchronous responses now include a generated `requestId` alongside the workflow output.

## Impact

- `AgentMesh.Api/Controllers/RequestsController.cs`: new endpoint and action method; existing synchronous actions change their return type to include `requestId`.
- `AgentMesh.Api/Models/Api/`: new `ProcessRequestAsyncApiInput` DTO and new `ProcessRequestApiOutput` response wrapper DTO (used by both sync and async responses).
- `AgentMesh.Api/Services/DummyWorkflowProgressNotifier.cs`: replaced with a callback-posting implementation.
- `AgentMesh.Api/Program.cs`: DI registration for `IWorkflowProgressNotifier` changes from a plain singleton to a request-scoped/contextual registration.
- `AgentMesh.Application/Services/StatelessAppInstance.cs`: background execution path needs to run outside the HTTP request's cancellation scope and needs a way to associate the created scope's `IWorkflowProgressNotifier` with the callback URLs and `requestId`.
- No changes to `AgentMeshCLI` console notifier or interactive mode behavior.
