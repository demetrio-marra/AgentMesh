## Why

`AgentMesh.Api` currently depends directly on `AgentMesh.Framework.Application` to configure shared runtime services and load plugins. That dependency prevents the HTTP host from being independently packaged and built against the framework contracts it actually consumes.

## What Changes

- Move plugin discovery and bootstrap invocation to the API startup path.
- Pass the API host configuration directly to every discovered plugin bootstrap so plugins do not resolve configuration independently.
- Load each plugin through an API-owned assembly load context that reuses assemblies already loaded by the host, preserving shared type identity and preventing duplicate loads.
- Remove the direct `AgentMesh.Framework.Application` dependency from `AgentMesh.Api`.
- Preserve existing API routes, authentication, callback behavior, and pipeline routing.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `api-cli-separation`: The API host independently composes runtime services through discovered plugins without a direct Application package dependency.

## Non-goals

- Changing HTTP endpoints, request or response contracts, authentication, or pipeline execution semantics.
- Introducing an additional runtime bridge or dedicated runtime plugin project.

## Impact

- Affects API startup, plugin bootstrap implementations, Application composition code, package references, and deployment assets.
- Requires build and startup coverage for plugin configuration forwarding and host-compatible assembly loading.