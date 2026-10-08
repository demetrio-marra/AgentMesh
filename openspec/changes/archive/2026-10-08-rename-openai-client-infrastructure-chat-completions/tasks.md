## 1. Rename the Infrastructure Project Identity

- [x] 1.1 Rename the `AgentMesh.Infrastructure.OpenAIClient` directory and its project file to `AgentMesh.Infrastructure.ChatCompletions`, and verify the renamed directory contains the adapter source files and project file.
- [x] 1.2 Update the adapter source namespaces to `AgentMesh.Infrastructure.ChatCompletions` while retaining the existing concrete type names and contracts, and verify no adapter source file declares the old namespace.

## 2. Update Solution-Local References

- [x] 2.1 Update `AgentMesh.sln` and `AgentMesh.Application/AgentMesh.Application.csproj` to reference the renamed project path and identity, and verify both references resolve to the renamed project file.
- [x] 2.2 Update the `AgentMesh.Application` composition-root import to the renamed namespace while retaining the existing factory registration, and verify the registration still targets the existing factory type.

## 3. Compile the Refactor

- [x] 3.1 Search tracked solution source and project metadata for `AgentMesh.Infrastructure.OpenAIClient`, and verify no stale references remain after the rename.
- [x] 3.2 Run `dotnet build AgentMesh.sln` and verify it completes successfully with all project references, namespaces, contracts, and dependency-registration code compiling; do not add or run business-logic tests.