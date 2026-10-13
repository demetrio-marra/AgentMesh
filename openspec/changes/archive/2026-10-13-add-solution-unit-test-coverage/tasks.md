## 1. Test Infrastructure

- [x] 1.1 Define the shared .NET 8 xUnit project convention (test SDK, xUnit runner, project reference, nullable settings, and production-compatible framework overrides) and verify a newly scaffolded test project builds without production-host startup.
- [x] 1.2 Add coverage collection configuration that emits Cobertura reports for production assemblies while excluding test/generated code, and verify `dotnet test` writes a coverage artifact under the test-results directory.
- [x] 1.3 Add every new test project to `AgentMesh.sln` and verify the solution lists all production and matching test projects.

## 2. Core and Contract Unit Tests

- [x] 2.1 Create `AgentMesh.Tests`; cover core model calculations, records, serialization behavior, and validation/error paths, and verify its xUnit project passes.
- [x] 2.2 Create `AgentMesh.Contracts.Tests`; cover contract DTO serialization/defaults and deterministic helper or exception behavior, and verify its xUnit project passes.

## 3. Runtime and Application Unit Tests

- [x] 3.1 Create `AgentMesh.Runtime.Tests`; cover deterministic runtime-owned API, authentication, configuration, and helper behavior with in-memory dependencies, and verify its xUnit project passes without starting an HTTP host.
- [x] 3.2 Create `AgentMesh.Application.Tests`; cover parameter-store snapshots/versioned commits, pipeline state transitions, request routing, pipeline branches, workflow-step state updates, agent parsing/retry failures, and executor mappings with faked contracts, and verify its xUnit project passes without invoking providers.

## 4. Infrastructure Adapter Unit Tests

- [x] 4.1 Create `AgentMesh.Infrastructure.ChatCompletions.Tests`; cover request construction, response mapping, configuration validation, and provider-error mapping using fake clients or handlers, and verify its xUnit project passes without network access.
- [x] 4.2 Create `AgentMesh.Infrastructure.Cohere.Tests`; cover reranking request/response mapping and validation/error branches using an in-memory HTTP handler, and verify its xUnit project passes without network access.
- [x] 4.3 Create `AgentMesh.Infrastructure.JSSandbox.Tests`; cover command/payload construction and execution-result/error mapping through an in-memory HTTP handler, and verify its xUnit project passes without launching a sandbox.
- [x] 4.4 Create `AgentMesh.Infrastructure.LightRag.Tests`; cover knowledge request construction, document/result mapping, and invalid-response handling using fake transport, and verify its xUnit project passes without network access.
- [x] 4.5 Create `AgentMesh.Infrastructure.Mem0.Tests`; cover memory request construction, result mapping, configuration validation, and error paths using fake transport, and verify its xUnit project passes without network access.
- [x] 4.6 Extend `AgentMesh.Infrastructure.Responses.Tests` for remaining deterministic request, stream, usage, and error branches, and verify its xUnit project continues to pass without network access.

## 5. Host-Surface Unit Tests

- [x] 5.1 Create `AgentMeshCLI.Tests`; cover deterministic API configuration binding and console formatting helpers with in-memory configuration, and verify its xUnit project passes without entering the console loop or invoking providers.
- [x] 5.2 Create `AgentMeshWeb.Tests`; cover in-memory chat state revisions, operation transitions, and configuration validation, and verify its xUnit project passes without rendering a browser or starting a web host.
- [x] 5.3 Create `CallbackViewer.Tests`; cover deterministic callback request display formatting for headers, path/query, and empty or populated payloads, and verify its xUnit project passes without listening for callbacks.

## 6. Coverage Review and Validation

- [x] 6.1 Run every test project independently while adding tests; verify each completes with no network, Docker, browser, external-process, or live-provider dependency.
- [x] 6.2 Run `dotnet test AgentMesh.sln --collect:"XPlat Code Coverage"`; verify all test projects pass and the aggregate Cobertura report includes every production assembly.
- [x] 6.3 Review the aggregate and per-assembly coverage report; verify meaningful branch, validation, state-transition, and error-path coverage is present, and document any externally hosted or generated code excluded from unit coverage.