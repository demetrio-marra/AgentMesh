## Context

The proposal moves processing from the current in-process CLI host to the existing authenticated, stateless API. The API already exposes synchronous and asynchronous request routes, configuration summary retrieval, and workflow/summarization callbacks. The CLI must preserve interactive conversation behavior and Ctrl+C semantics without retaining the pipeline runtime.

The architecture-baseline path referenced by repository configuration is absent. This design uses the current API/CLI separation and composition artifacts, plus the implemented controllers and callback services, as the available architectural baseline.

## Goals / Non-Goals

**Goals:**

- Make the CLI an HTTP-only frontend with API URL and API key configuration.
- Keep conversation state and summarization decisions in CLI memory.
- Preserve progress rendering through callback delivery.
- Preserve cancellation from the user perspective by dropping local request tracking.
- Let the API own all processing configuration and prompt assets.

**Non-Goals:**

- Server-side conversation persistence.
- API cancellation of accepted background requests.
- A new pipeline execution model or a second API processing stack.
- Test-project or unit-test creation.

## Decisions

### Use asynchronous API requests for interactive work

The CLI submits chat and summarization operations to asynchronous API endpoints because progress notifications are callback-based. Synchronous API calls retain request cancellation tokens but cannot provide the existing step-by-step console progress behavior.

Alternative considered: use synchronous endpoints and cancel the HTTP request. Rejected because it loses progress callbacks and does not preserve the requested callback-driven notifier behavior.

### Treat cancellation as local abandonment

The CLI stores pending request IDs and removes an ID when Ctrl+C cancels the active wait. Callback endpoints return success for unknown IDs and perform no action. The API continues executing accepted work with its existing background token behavior.

Alternative considered: add a server cancellation endpoint and shared cancellation registry. Rejected as unnecessary coordination and explicitly outside this change.

### Separate advertised callback URL from API routing

The CLI configuration supplies a complete API base URL and a complete callback base URL. The CLI builds all five callback URLs from the latter and sends them unchanged. The API does not translate localhost, public hosts, ports, or proxy routes.

The callback listener binds according to the CLI deployment configuration. The default advertised base URL is localhost.

### Use local transport models

The CLI defines only the request, response, conversation, workflow-result, and callback DTOs needed to communicate with the API and render results. It does not reference AgentMesh application or infrastructure packages. JSON property names and enum representations remain compatible with the API contract.

### Serialize conversation mutation

The CLI accepts the next prompt only after the active chat completion has been applied. When both automatic summarization conditions are met, it starts summarization immediately and waits for its completion before accepting another prompt. This prevents concurrent callbacks from racing with conversation mutation.

### Merge runtime configuration into API-owned files

The API currently links shared `appsettings.json` and `appsettings.Development.json` from the CLI, while its own `appsettings.Api.json` files contain API authentication and development hosting settings. The migration creates API-owned effective configuration by merging the CLI shared sections into the corresponding API files and preserving the API-only sections. It then removes the API project links to CLI files.

This is configuration ownership work only. No API controller, endpoint, callback, authentication, or pipeline-processing code is changed. Prompt files remain supplied by the plugin package and are not blindly copied from the CLI.

## Risks / Trade-offs

- [Risk] The API continues consuming resources after the CLI drops a request → Keep local abandonment explicit in the CLI and ignore all late callbacks by request ID.
- [Risk] The API cannot reach a localhost callback URL from another host or container → Make the callback base URL configurable and document that it must be reachable from the API deployment.
- [Risk] The CLI can lose a request ID if Ctrl+C occurs during the initial HTTP submission → Treat the initial submission cancellation as best-effort and avoid mutating conversation state until a correlated completion arrives.
- [Risk] Callback delivery is best-effort in the existing API → Preserve the current callback contract and report active-request callback failures; do not add a durable callback queue in this migration.
- [Risk] Duplicated transport DTOs can drift from API contracts → Keep DTOs limited to the public HTTP contract and validate compatibility through the solution build and API documentation inspection.

## Migration Plan

1. Merge CLI shared runtime configuration into API-owned configuration files, preserve API-only settings, and remove API links to CLI files.
2. Add CLI HTTP transport DTOs, API client, callback listener, pending-request tracking, and local conversation state.
3. Replace local `AppInstance` and pipeline registrations in the CLI with HTTP request handling.
4. Wire automatic and explicit summarization through API callbacks.
5. Build the API and CLI independently and run the existing non-test validation commands.

Rollback consists of restoring the previous CLI package/runtime registrations and API configuration links. The API build check in this change verifies the configuration relocation only; it is not an API compatibility test or an API feature validation step.