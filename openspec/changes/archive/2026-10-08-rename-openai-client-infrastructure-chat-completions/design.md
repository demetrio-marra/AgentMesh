## Context

The chat-completions adapter currently resides in the `AgentMesh.Infrastructure.OpenAIClient` project. The solution file, `AgentMesh.Application` project reference, and application composition root refer to that project and namespace. The adapter's implementation types implement existing contracts and are registered by the application runtime.

The architecture-baseline document referenced by project context is unavailable at its configured archive path; this design is therefore grounded in the currently inspected solution and project references.

## Goals / Non-Goals

**Goals:**
- Establish `AgentMesh.Infrastructure.ChatCompletions` as the project and namespace identity for the existing adapter.
- Preserve the existing adapter type names, contracts, and dependency-injection registration behavior.
- Make solution compilation the acceptance criterion for the refactor.

**Non-Goals:**
- Provide an old-namespace compatibility shim.
- Alter runtime OpenAI request behavior, configuration values, or service lifetimes.
- Run business-logic, integration, or live-provider tests.

## Decisions

### Rename the project identity atomically

Rename the project directory, project file, and source namespaces in one change, then update each known reference in the solution, application project, and application runtime. This keeps the physical project identity aligned with the namespace identity and avoids a lingering mismatch.

Alternative considered: retain the old project file or namespace as an alias. Rejected because all current consumers are within the solution and a compatibility layer would preserve the ambiguity this refactor removes.

### Keep adapter contracts and concrete type names unchanged

Only the containing namespace and project identity change. `IOpenAIClient`, `IOpenAIClientFactory`, `OpenAIClient`, and `OpenAIClientFactory` remain unchanged so consumers retain the same contract and dependency-registration shape.

Alternative considered: rename concrete classes to match the project. Rejected because it expands the refactor without a stated need and increases consumer churn.

### Validate through solution compilation

Build `AgentMesh.sln` after the rename. Compilation resolves project paths, imports, namespaces, contract compatibility, and the application runtime's registration of the factory, which directly exercises the refactor's structural risk.

Alternative considered: add behavioral or provider tests. Rejected because the user explicitly limits validation to compilation and the change does not alter business logic.

## Risks / Trade-offs

- [Incomplete reference update] -> A stale project path or namespace can prevent the solution from compiling. Mitigation: search for the old identity before and after the rename, then build the solution.
- [Rename metadata drift] -> The project file and solution entry can diverge after a filesystem rename. Mitigation: update both references in the same edit and use the build result as the acceptance check.
- [No legacy namespace support] -> External consumers outside this repository would need to update their imports. Mitigation: this plan scopes the refactor to the current solution; no external package compatibility is promised.

## Migration Plan

1. Rename the infrastructure directory and project file.
2. Update namespaces and all solution-local references from the old identity to the new identity.
3. Compile `AgentMesh.sln`.
4. Roll back by restoring the former directory, project-file path, namespaces, and references if compilation fails and cannot be repaired within the refactor scope.