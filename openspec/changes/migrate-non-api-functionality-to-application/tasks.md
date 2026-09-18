## 1. Establish the API-derived boundary

- [x] 1.1 Build `AgentMesh.Api` and create a checked-in migration inventory that classifies each current `AgentMesh` type as API-retained or Application-relocated, including transitive dependencies; verify the inventory matches API source, global usings, DI registrations, public signatures, and compiler dependencies.
- [x] 1.2 Confirm the retained core contract set includes plugin bootstrap, API configuration, workflow-progress abstractions, and their exposed models; verify `AgentMesh.Api` still builds without an `AgentMesh.Application` project or package reference.

## 2. Relocate Application-only functionality

- [x] 2.1 Create `AgentMesh.Contracts` as a class library that references `AgentMesh`, add it to the solution, and establish its package metadata; verify `AgentMesh.Contracts` builds independently.
- [x] 2.2 Move classified shared non-API models, contracts, exceptions, helpers, and utilities from `AgentMesh` to `AgentMesh.Contracts`; update namespaces and direct consumers, then verify `AgentMesh.Contracts` and `AgentMesh.Application` build.
- [x] 2.3 Update Application and infrastructure project references to consume Contracts where needed, leaving the API with only its core reference; verify the project graph has no cycle and `AgentMesh.Api` has no Application or Contracts reference.
- [x] 2.4 Move any remaining Application-only runtime services, agents, pipelines, steps, and internal contracts from core to Application; update CLI and plugin consumers, then verify no Contracts-owned or Application-owned source is compiled by `AgentMesh`.
- [x] 2.5 Update package metadata, XML documentation inclusion, and compilation-item rules for the final three-layer ownership boundary; verify the Application package contains the intended assembly and XML documentation without creating infrastructure packages.

## 3. Validate host and plugin compatibility

- [x] 3.1 Build `AgentMesh`, `AgentMesh.Contracts`, `AgentMesh.Application`, `AgentMesh.Api`, `AgentMeshCLI`, and the default pipeline plugin independently; verify each succeeds with its intended references.
- [x] 3.2 Manually run the existing API/plugin startup and workflow callback checks; verify routes, API-key authentication, plugin bootstrap loading, callbacks, and pipeline results are unchanged without creating test projects.
- [x] 3.3 Review the final retained-core inventory and generated package contents against the baseline; verify every core type is API-reachable, every shared Application/infrastructure type is Contracts-owned, and document any third-party package versioning impact.