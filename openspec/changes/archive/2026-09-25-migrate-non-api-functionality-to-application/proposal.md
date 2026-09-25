## Why

`AgentMesh` currently contains both the API-facing framework surface and definitions consumed only by the application runtime and infrastructure adapters. This obscures the minimum dependency required by `AgentMesh.Api` and couples the core package to infrastructure-facing contracts.

## What Changes

- Establish the API project's compile-time type usage as the criterion for retaining definitions in `AgentMesh`.
- Create an `AgentMesh.Contracts` class library that references `AgentMesh` and owns definitions used by `AgentMesh.Application` and one or more infrastructure projects, but not required by `AgentMesh.Api`.
- Move Application/infrastructure-only contracts, models, exceptions, helpers, and utilities from `AgentMesh` into `AgentMesh.Contracts`; retain the API-facing configuration, plugin bootstrap, workflow progress, and their required model/contract dependencies in core.
- Update project references, packaging metadata, and XML documentation output so `AgentMesh.Api` remains independent of both the Application and Contracts packages.
- Validate the retained core boundary and new contract dependency graph through independent builds and manual API/plugin checks. Do not create test projects.

## Capabilities

### New Capabilities

None. This is a behavior-preserving internal package-boundary refactor.

### Modified Capabilities

None. Existing API routes, authentication, callbacks, plugin discovery, and pipeline behavior remain unchanged.

## Impact

- Affected projects: `AgentMesh`, new `AgentMesh.Contracts`, `AgentMesh.Application`, `AgentMesh.Api`, `AgentMeshCLI`, infrastructure adapters, and plugin projects.
- The API must continue to reference only the shared core package and load pipeline functionality through plugin bootstraps. Application and infrastructure projects consume shared non-API definitions through `AgentMesh.Contracts`.
- Internal type locations, project references, package contents, and consuming namespaces will change; public API behavior will not.

## Non-goals

- Changing HTTP endpoints, callback payloads, authentication, plugin discovery, or pipeline execution behavior.
- Redesigning core framework abstractions beyond what is necessary to enforce the API-derived package boundary.
- Creating automated test projects; validation is performed through builds and manual runtime checks.