## Why

The in-repository `samples/AgentMesh.DefaultPipelinePlugin` project was removed, but the README and default-plugin specification still describe it as a bundled, runnable sample. Those stale references send plugin authors to unavailable paths and obscure the separately maintained plugin project that now demonstrates the supported integration.

## What Changes

- Replace AgentMesh README references to the removed local sample with the external `AMCodePipeline` GitHub repository as the runnable plugin example.
- Replace local sample run and Docker commands with a link to the external repository's maintained setup instructions.
- **BREAKING** Update the default-plugin specification: AgentMesh no longer distributes the executable default sample plugin; its documented reference implementation is maintained in the external repository.
- Preserve the framework's plugin-hosting, Runtime-package, request, and deployment contracts.

## Non-goals

- Move or duplicate `AMCodePipeline` source into this repository.
- Change AgentMesh Runtime APIs, HTTP contracts, pipeline behavior, or package boundaries.
- Define the external repository's release, testing, or configuration policies.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `default-chat-pipeline-plugin`: Define the externally maintained `AMCodePipeline` repository as the documented default plugin reference instead of an executable sample bundled with the AgentMesh distribution.

## Impact

- `README.md` will direct plugin developers to `https://github.com/demetrio-marra/AMCodePipeline` and remove commands targeting the deleted `samples/AgentMesh.DefaultPipelinePlugin` path.
- `openspec/specs/default-chat-pipeline-plugin/spec.md` will replace distribution-owned sample requirements with an external-reference requirement while retaining compatibility and framework-package expectations for the documented plugin.
- No runtime code, public API, NuGet package, or deployment behavior changes are planned.