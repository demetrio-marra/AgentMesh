## Context

See `proposal.md` for motivation. `AgentMesh.Api` references the core project but directly imports configuration and service namespaces for API startup, plugin bootstrap, and workflow-progress callbacks. `AgentMesh.Application` currently references the core project and all infrastructure adapters; each infrastructure project also references the core project. The existing API/CLI separation specification requires the API to remain independent of the Application package and to acquire pipeline behavior through plugin bootstraps.

The configured architecture-baseline document was not present at its configured archive path during planning; this design is based on the current project files and existing API/CLI separation specification.

## Goals / Non-Goals

**Goals:**

- Derive a repeatable retained-core inventory from the API compilation and the dependency closure of its referenced public/internal types.
- Introduce `AgentMesh.Contracts` as the shared dependency for definitions used by Application and infrastructure but not by the API.
- Preserve API build independence from the Application and Contracts packages, and preserve existing plugin type identity.

**Non-Goals:**

- Rework API hosting or change public namespaces solely for aesthetic consistency.
- Change endpoint, authentication, callback, plugin, or workflow semantics.
- Treat arbitrary textual references as evidence of API type usage.
- Create automated test projects.

## Decisions

### Use compilation-backed API reachability as the retention rule

Build the API and inspect its project source, generated global usings, service registrations, public signatures, and referenced type dependencies. Retain only the types required to compile and run the API host, including their transitive core dependencies. This is more reliable than folder ownership or text search, which can retain unused code or miss type references through interfaces and generic signatures.

Alternative: manually retain selected folders. Rejected because those folders currently mix API concerns and application runtime concerns.

### Create a Contracts layer for shared non-API definitions

Create `AgentMesh.Contracts` as a class library that references `AgentMesh`. Move every classified definition consumed by Application and at least one infrastructure project, but not by API, from core to Contracts. Update Application and infrastructure projects to reference Contracts instead of relying on core for those definitions. This removes infrastructure details from the API-facing core without requiring the infrastructure projects to reference Application.

Alternative: move the definitions directly to Application. Rejected because Application already references infrastructure adapters, so infrastructure-to-Application references create a circular dependency.

### Move by dependency closure, not individual files

Create a classified inventory before moves: API-retained core types, Contracts-owned shared definitions, Application-only runtime types, and consumers requiring import/reference updates. Move mutually dependent groups together, beginning with leaf models/helpers and ending with service compositions, while keeping a project-reference graph without cycles.

Alternative: move files opportunistically while resolving compiler errors. Rejected because it obscures the intended boundary and risks leaving accidental core dependencies.

### Keep API and plugin contracts in the core package

`IAgentMeshPluginBootstrap`, API configuration contracts, `IWorkflowProgressNotifier`, and all core models they expose remain in `AgentMesh`; API-local implementations stay in `AgentMesh.Api`. Shared Application/infrastructure definitions move to Contracts; Application-only implementations remain in Application. This preserves the current API-to-plugin loading boundary and shared type identity.

Alternative: move all contracts to Application. Rejected because the API must not directly reference the Application package.

### Verify boundaries through independent builds and reference inspection

Build `AgentMesh`, `AgentMesh.Contracts`, `AgentMesh.Application`, API, CLI, and plugin projects independently after the move. Confirm `AgentMesh.Api` has no Application or Contracts project/package reference and that Contracts-owned files no longer compile into core. Manually exercise the existing API/plugin startup and callback path.

Alternative: validate the solution build alone. Rejected because it can mask accidental dependencies through transitive project builds.

## Risks / Trade-offs

- [Missing a transitive API-facing model] -> Use compiler errors, API build, and public-signature review before deleting core copies.
- [Circular project dependency] -> Make Contracts depend only on core, and update Application/infrastructure references to Contracts without adding a Contracts-to-Application reference.
- [Plugin assembly type-identity regression] -> Retain bootstrap contracts in core and test plugin loading with the existing assembly load context.
- [Unintended package compatibility change] -> Review generated package contents and document any unavoidable public-type relocation as a breaking package change before release.

## Migration Plan

1. Capture the API reachability inventory and baseline project/package references.
2. Create Contracts, relocate classified shared non-API dependency groups from core, and update Application/infrastructure consumers and project metadata.
3. Build each executable/package independently, run manual API/plugin checks, and inspect final references.
4. Roll back by restoring the original file ownership and project references if an API or plugin boundary check fails; no data migration is involved.

## Open Questions

- Whether the core and application NuGet packages are consumed by third parties must be confirmed before release; it affects versioning and deprecation handling, but not the source migration approach.