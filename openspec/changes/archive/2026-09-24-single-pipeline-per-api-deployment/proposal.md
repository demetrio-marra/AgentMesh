## Why

Supporting multiple pipelines in one API process adds plugin discovery, registry validation, and route-selection complexity without a meaningful deployment benefit. A simpler operational model deploys one pipeline per API pod and uses Kubernetes Services and workload tags to route all requests for a pipeline to the corresponding service.

## What Changes

- **BREAKING** Remove the named chat-pipeline endpoints `/api/pipelines/{pipelineName}/requests` and `/api/pipelines/{pipelineName}/requests/async`; clients use the existing unnamed synchronous and asynchronous request endpoints.
- Change the API and framework runtime model from selecting among multiple chat pipelines to resolving one registered chat pipeline per deployment.
- Simplify plugin loading and registration around a single pipeline plugin, removing multi-plugin discovery, naming, duplicate-name checks, and pipeline-name routing logic.
- Preserve tolerant API startup when no plugin or usable pipeline is found; the host starts and returns a generic RFC7807 `503 Service Unavailable` only when a request needs the unavailable pipeline.
- Update README architecture and deployment guidance to recommend one pipeline per pod/service, label or tag each workload/service by pipeline identity, and route requests for the same pipeline to that service within the Kubernetes cluster.
- Remove README guidance that describes loading or selecting multiple plugins/pipelines in one API process.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `plugin-pipeline-hosting`: Replace simultaneous multi-pipeline hosting and name-based selection with a single plugin/pipeline deployment contract while preserving startup-mounted plugin extensibility.
- `request-access-modes`: Remove named pipeline routes and define unnamed request endpoints against the deployment's sole chat pipeline, including request-time failure when none is available.
- `architecture-documentation`: Document the one-pipeline-per-pod boundary and Kubernetes service/tag routing model for hosting multiple pipelines in one cluster.

## Impact

Affected areas include `AgentMesh.Api` routes, OpenAPI output, plugin bootstrap loading, API configuration, `AgentMesh.Application` pipeline resolution, `AgentMesh` application-runner contracts and routing exceptions, tests, sample plugin documentation, Kubernetes deployment examples, and README architecture guidance. API clients using named routes must migrate to a pipeline-specific service endpoint and call `/api/requests` or `/api/requests/async`.

## Non-goals

- Defining a cluster ingress, service mesh, or message-broker implementation for selecting Kubernetes Services.
- Adding runtime plugin hot reload or changing pipeline execution semantics.
- Removing the plugin boundary or compiling a concrete pipeline into the API host.
- Changing summarization endpoint behavior beyond ensuring it follows the same single-deployment ownership model.