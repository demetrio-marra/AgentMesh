## Context

See [proposal.md](proposal.md) for motivation. Before this change, API-facing entry-point and result types were declared in `AgentMesh.Application`, coupling API consumers to an implementation package. The implemented relocation keeps behavior intact while changing the owner of types required by request controllers and API response models.

The currently updated API still requires `AgentMesh.Application` for `AgentMeshRuntime` and `PipelineRoutingException`; this design does not attempt to remove those dependencies.

## Goals / Non-Goals

**Goals:**

- Make the application entry-point abstraction and its data contracts available from `AgentMesh.Framework`.
- Preserve the existing `AppInstance` operations, result data, and dependency-injection behavior.
- Have API controllers target the core interface rather than the Application implementation.

**Non-Goals:**

- Moving `AgentMeshRuntime` composition APIs or `PipelineRoutingException` into the core package.
- Eliminating the API project's Application package reference in this iteration.
- Altering endpoint behavior or the wire representation of workflow results.

## Decisions

### Put consumer-facing contracts in the core package

`IAppInstance`, `AgentMeshConfiguration`, `WorkflowResult`, `SummarizationResult`, and `AgentExecutionCost` are owned by `AgentMesh`. They form the data and operation boundary needed by a host such as `AgentMesh.Api`, without revealing pipeline implementation details.

Alternative: leave the types in `AgentMesh.Application`. This keeps the previous coupling and prevents a consumer from targeting only framework definitions.

### Retain Application ownership of execution

`AppInstance` remains an Application implementation and implements the core `IAppInstance` interface. Application composition registers both the concrete type for existing internal consumers and the interface mapping for boundary consumers.

Alternative: move `AppInstance` itself into `AgentMesh`. It depends on Application pipelines, configuration, and infrastructure services, which would reverse the intended dependency direction.

### Migrate API type consumption incrementally

API controllers inject `IAppInstance`; API response models import core workflow results. The API keeps its Application package reference because composition startup and routing exceptions remain there, to be addressed by a follow-up change.

Alternative: remove the Application reference now. That would expand this completed refactor into a composition and error-contract redesign beyond the observed changes.

## Risks / Trade-offs

- [Duplicate registrations can create different `AppInstance` instances] -> Register the concrete singleton once and map `IAppInstance` to that same singleton instance.
- [Consumers may bind to data contracts that later need evolution] -> Treat the core result models as public contracts and make future changes deliberately compatible.
- [The API remains partially coupled to Application] -> Track runtime composition and routing exception migration separately; do not claim complete decoupling in this change.

## Migration Plan

1. Publish `AgentMesh.Framework` 1.0.2 with the relocated contracts.
2. Publish `AgentMesh.Framework.Application` 1.0.12 with the interface implementation and registrations.
3. Update API package references and controller/DTO imports together, then build the solution to verify type resolution.
4. Roll back by restoring the prior package versions and Application-owned type declarations if deployment exposes compatibility issues.