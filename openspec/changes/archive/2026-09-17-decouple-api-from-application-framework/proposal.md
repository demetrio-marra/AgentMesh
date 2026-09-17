## Why

`AgentMesh.Api` needs only the public application entry point and its request results, but those types were owned by `AgentMesh.Application`. Moving this API-facing contract surface into `AgentMesh` establishes a framework-definition boundary and lets consumers reference the core framework package for those contracts.

## What Changes

- Move API-facing runtime contracts and result/configuration models from `AgentMesh.Application` to `AgentMesh`.
- Define `IAppInstance` in the core framework and have `AppInstance` implement it.
- Update API request handling and response DTOs to consume the core contract types.
- Publish the updated core and application framework package versions used by the API.
- Defer removal of the remaining `AgentMesh.Application` references for `AgentMeshRuntime` and `PipelineRoutingException` to a follow-up change.

## Non-goals

- Removing all `AgentMesh.Application` dependencies from `AgentMesh.Api`.
- Changing request, response, callback, pipeline-routing, or summarization behavior.
- Changing API endpoints or serialized response shapes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None.

## Impact

- `AgentMesh` now owns `IAppInstance`, `AgentMeshConfiguration`, `WorkflowResult`, `SummarizationResult`, and `AgentExecutionCost`.
- `AgentMesh.Application` implements the core entry-point interface and consumes the relocated models.
- `AgentMesh.Api` injects `IAppInstance` and maps the core workflow result type.
- Package versions advance to `AgentMesh.Framework` 1.0.2 and `AgentMesh.Framework.Application` 1.0.12.