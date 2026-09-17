## 1. Core Contract Relocation

- [x] 1.1 Move `AgentMeshConfiguration` and its agent configuration summary from `AgentMesh.Application` into `AgentMesh`, and verify Application compiles against the core definitions.
- [x] 1.2 Move `WorkflowResult`, `SummarizationResult`, and `AgentExecutionCost` into `AgentMesh`, preserving their namespaces and members so existing consumers receive the same result data.
- [x] 1.3 Add `IAppInstance` to `AgentMesh.Services` with the existing request-processing and summarization operations, and verify it exposes the relocated result types.

## 2. Application Implementation

- [x] 2.1 Update `AppInstance` to implement `IAppInstance` and use the core configuration, workflow, and cost types; verify method signatures match the interface.
- [x] 2.2 Register `IAppInstance` to resolve the existing `AppInstance` singleton in `AgentMeshRuntime`; verify API dependency injection can resolve the interface.
- [x] 2.3 Advance the core and Application framework package versions to 1.0.2 and 1.0.12 respectively, and verify the project package metadata is consistent.

## 3. API Contract Consumption

- [x] 3.1 Add the core framework package reference to `AgentMesh.Api` and update its Application package reference to 1.0.12; verify restore resolves both packages.
- [x] 3.2 Change `RequestsController` to depend on `IAppInstance` and update `ProcessRequestApiOutput` to consume `AgentMesh.Models.Workflows.WorkflowResult`; verify request and summarization controller actions compile.
- [x] 3.3 Build `AgentMesh.sln` and verify the API continues to compile without changing endpoint behavior or response contracts.