## Why

The solution currently mixes production projects, the test project, and repository-level metadata in one directory, making project boundaries and repository navigation harder to scan. A conventional `src/` and `tests/` layout will make the build structure clearer while preserving the current product behavior.

## What Changes

- Move every current non-test solution project beneath `src/` and move `AgentMesh.Infrastructure.Responses.Tests` beneath `tests/`.
- Keep solution-root assets, including the solution file, NuGet configuration, repository documentation, OpenSpec content, and GitHub configuration, at the repository root.
- Update solution entries, inter-project references, build and package commands, Docker build paths, CI references, README examples, and path-bearing OpenSpec documentation to use the new layout.
- Preserve project names, assembly/package identities, namespaces, target frameworks, runtime behavior, and test coverage.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a structural repository refactor with no runtime or externally observable contract change.

## Impact

- `AgentMesh.sln` will reference `src/*` and `tests/*` project locations.
- Existing `ProjectReference` values must be recalculated from their relocated project directories; for example, Runtime currently references `AgentMesh` and `AgentMesh.Application`, and the Responses test project references its production project.
- Repository-root Docker invocations and `src/AgentMeshWeb/Dockerfile` require source-directory paths for Docker build-context inputs.
- Root documentation and OpenSpec artifacts containing source-tree commands or file paths require synchronized updates.

## Non-goals

- Change project names, namespaces, public APIs, runtime architecture, package identities, or target frameworks.
- Add projects, tests, CI systems, or container features beyond path adjustments required by the move.