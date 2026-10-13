## 1. Classify and move solution projects

- [x] 1.1 Inventory every project entry in `AgentMesh.sln`, classify `AgentMesh.Infrastructure.Responses.Tests` as test and the remaining projects as source, and verify the inventory contains all fourteen current entries.
- [x] 1.2 Move each non-test project directory intact to `src/<project-name>/`, leaving `AgentMesh.sln`, `NuGet.config`, README, `.github`, and `openspec` at the repository root; verify all thirteen source project files exist under `src/`.
- [x] 1.3 Move `AgentMesh.Infrastructure.Responses.Tests` intact to `tests/AgentMesh.Infrastructure.Responses.Tests/` and verify its project and test source files exist under `tests/` with no project directory left at the root.

## 2. Repair build graph and operational paths

- [x] 2.1 Update every `AgentMesh.sln` project path to its `src/` or `tests/` location while retaining project names and GUIDs, then verify `dotnet sln AgentMesh.sln list` reports all fourteen relocated projects.
- [x] 2.2 Update affected `ProjectReference` paths, including the test-to-source reference across `tests/` and `src/`, and verify `dotnet restore AgentMesh.sln` completes without unresolved project warnings.
- [x] 2.3 Update Dockerfile paths and root-context container commands for relocated projects, including `src/AgentMeshWeb/Dockerfile` and any applicable templates; verify a dry review of each `COPY`, restore, and publish path resolves from the repository root.
- [x] 2.4 Update tracked CI, README, and OpenSpec references that document source-tree paths or runnable commands; verify repository-root metadata remains outside `src/` and `tests/`.

## 3. Validate the relocated repository

- [x] 3.1 Search tracked files excluding generated `bin/` and `obj/` directories for obsolete root-level project paths, and verify remaining matches are intentional identifiers rather than filesystem references.
- [x] 3.2 Run `dotnet build AgentMesh.sln` and `dotnet test tests/AgentMesh.Infrastructure.Responses.Tests/AgentMesh.Infrastructure.Responses.Tests.csproj`, resolving only migration-caused failures and verifying the solution and test project succeed.
- [x] 3.3 Run the documented `dotnet pack` commands using `src/` project paths and verify the package builds succeed. Docker build validation was skipped per user request because the Docker CLI is unavailable.