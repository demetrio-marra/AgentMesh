## Context

See `proposal.md` for motivation and the three delta specs for required behavior.

`AgentMesh.Api` currently scans every `*Plugin.dll`, invokes every discovered bootstrap, and exposes named and unnamed request routes. The loaded plugin calls `ApplicationRuntime.RegisterCommonServices`, so a deployment with no plugin can start but does not register `IAppInstance`; request-time controller activation therefore cannot reliably produce the specified RFC7807 `503` response. In the framework, `AppInstance`, `PipelineRegistryInitializer`, `PluginHostState`, `IChatRequestPipeline.Name`, and name-bearing `IAppInstance` overloads implement multi-pipeline registry and routing behavior.

The API intentionally references the core `AgentMesh` contract project rather than the application framework. A valid plugin remains responsible for registering its framework runtime and concrete pipeline graph. The architecture-baseline path configured in `openspec/config.yaml` is stale; this design uses the inspected current source and the available archived `2026-09-10-document-project-architecture` design as context.

## Goals / Non-Goals

**Goals:**
- Make one plugin assembly and one chat pipeline the unambiguous unit of an API deployment.
- Keep the API host constructible without a plugin and translate pipeline absence into request-time RFC7807 responses.
- Remove pipeline identity and selection from framework execution contracts.
- Preserve the API/framework package boundary and plugin-owned runtime registration.

**Non-Goals:**
- Moving framework service registration into `AgentMesh.Api` or adding a direct application-framework reference.
- Selecting among multiple plugin candidates or chat pipelines by ordering, filename, or name.
- Implementing Kubernetes ingress, service-mesh, queue, or broker routing resources.
- Changing callback execution, workflow execution, or summarization contracts beyond shared availability handling.

## Decisions

### Decision 1: Discover at most one plugin entry assembly

`PluginBootstrapLoader` will resolve the configured plugin directory and accept zero or one `*Plugin.dll` entry assembly. Zero candidates is a valid startup state. One candidate is loaded through the existing isolated load context and its bootstrap is invoked. Multiple candidates are not composed or ordered; they are recorded as an invalid deployment state so requests fail generically instead of selecting an arbitrary plugin.

The loader will return a small host-owned result describing `Loaded`, `Missing`, or `Invalid` without exposing assembly details to HTTP clients. Startup logs may retain diagnostic details for operators.

Alternatives considered:
- Add a configured plugin filename: deterministic, but adds configuration and migration work when deployment cardinality already provides the invariant.
- Load the first sorted candidate: simple, but silently masks an invalid pod image or mount.
- Keep loading every bootstrap and only reject multiple pipelines later: preserves the complexity this change removes and permits cross-plugin service composition.

### Decision 2: Provide an API-side unavailable `IAppInstance` fallback

After plugin loading, the API composition root will register a fallback `IAppInstance` only when no plugin supplied one. The fallback throws the existing generic routing exception from execution methods, using `No pipelines loaded` for a missing plugin and the generic redeploy response for an invalid plugin state. This keeps controller activation and RFC7807 handling intact without coupling the host to `AgentMesh.Application`.

A valid plugin registration always wins; the fallback is not a second runtime implementation used for routing.

Alternatives considered:
- Register the full application runtime in the API: violates the existing host/framework decoupling and duplicates plugin composition ownership.
- Add plugin-state checks directly to every controller action: duplicates availability policy and still leaves non-controller `IAppInstance` consumers inconsistent.
- Allow dependency-injection activation to fail: produces framework-generated server errors rather than the required stable RFC7807 contract.

### Decision 3: Resolve the sole pipeline by cardinality at request time

The framework will remove chat pipeline names, name-bearing `IAppInstance` overloads, startup registry initialization, duplicate-name state, and name-specific routing exceptions. `AppInstance` will resolve `IChatRequestPipeline` by cardinality: zero returns the no-pipeline error, one executes, and more than one returns the generic plugin configuration error. The same cardinality rule remains for summarization.

This count check is an invariant guard, not multi-pipeline routing logic. Performing it at request time preserves startup tolerance and avoids maintaining a parallel registry snapshot.

Alternatives considered:
- Inject one `IChatRequestPipeline` directly: missing registrations fail dependency injection before application error mapping, and multiple registrations can resolve implicitly depending on container behavior.
- Validate and cache the pipeline at startup: reintroduces registry state and makes unavailable deployments fail too early.

### Decision 4: Remove named routes and parameters end to end

`RequestsController` will retain only `/api/requests` and `/api/requests/async` for chat execution. Private helpers and `IAppInstance` calls will no longer carry a nullable pipeline name. Name-required and not-found routing exceptions will be removed when no callers remain. OpenAPI metadata will document only unnamed routes and their request-time `503` behavior.

### Decision 5: Document Kubernetes as the pipeline-routing layer

README architecture, plugin-hosting, endpoint, and container sections will state that each API pod mounts one pipeline plugin and is exposed by a pipeline-specific Service. Deployments and Services use a consistent pipeline identity label/tag; upstream routing sends messages requiring that identity to that Service, whose clients call the unnamed endpoint. Existing text describing multi-plugin scanning, named routes, or built-in-plus-plugin composition will be removed or rewritten.

## Risks / Trade-offs

- **[Risk] Existing clients call named routes** -> Document the breaking migration to pipeline-specific service addresses and unnamed endpoints; add route/OpenAPI tests proving named routes are absent.
- **[Risk] A plugin mount contains multiple entry assemblies** -> Refuse to choose, log operator diagnostics, and expose only the generic request-time configuration response.
- **[Risk] Fallback and plugin registrations coexist accidentally** -> Register the fallback conditionally after plugin bootstrap processing and test both missing-plugin and valid-plugin service resolution.
- **[Risk] Removing `IChatRequestPipeline.Name` breaks third-party plugin compilation** -> Treat this as a framework contract break, update the sample plugin, and call it out in migration documentation.
- **[Trade-off] Cluster routing moves operational responsibility outside AgentMesh** -> Provide a concrete label/Service pattern while leaving ingress or message-router technology deployment-specific.

## Migration Plan

1. Release updated framework packages and API host with the single-pipeline contracts and unnamed routes.
2. Update each plugin to remove `IChatRequestPipeline.Name` and ensure its bootstrap registers exactly one chat pipeline.
3. Build one API image or plugin mount per pipeline and deploy each as a separate Kubernetes workload with matching pipeline identity labels and a dedicated Service selector.
4. Redirect callers or upstream message routing from named paths to the appropriate Service base address plus `/api/requests` or `/api/requests/async`.
5. Verify valid-plugin, missing-plugin, invalid-cardinality, synchronous, asynchronous, summarization, and OpenAPI behavior before removing old deployments.

Rollback requires redeploying the previous API/framework/plugin versions together and restoring clients that use named routes; mixed old and new framework contracts are not supported.