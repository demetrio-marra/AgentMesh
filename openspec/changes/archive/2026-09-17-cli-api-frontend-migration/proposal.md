## Why

The CLI still hosts and executes the AgentMesh application locally, duplicating behavior that now belongs to the mature API. This keeps the CLI coupled to AgentMesh NuGet packages and server configuration, while preventing the API and CLI from having a clean stateless-processing and stateful-frontend boundary.

## What Changes

- Make the CLI an HTTP client of `AgentMesh.Api` and remove its AgentMesh application/framework package dependency.
- Keep conversation state, context messages, token/message counters, and local cancellation tracking in the CLI.
- Merge the CLI's shared runtime configuration into API-owned configuration files, preserve API-only authentication and hosting settings, and retain only API connection/authentication and CLI conversation settings in the CLI.
- Retrieve the startup configuration summary from the authenticated API endpoint.
- Add a CLI callback listener and use API async request and summarization endpoints for progress and completion notifications.
- Preserve Ctrl+C behavior by dropping canceled request IDs locally; late callbacks for dropped IDs are ignored while the API continues processing.
- Automatically request summarization after a completed chat turn when both the token threshold and message-preservation count conditions are met.

## Capabilities

### New Capabilities

- `cli-api-frontend`: Define the CLI HTTP frontend, local conversation ownership, callback handling, local cancellation semantics, and automatic summarization behavior.

### Modified Capabilities

- `api-cli-separation`: Change the console host from an in-process application host to an HTTP-only frontend with independent configuration ownership.
- `async-request-callbacks`: Support the CLI callback receiver contract and terminal callback behavior required by the frontend.
- `api-summarization-pipeline`: Support CLI-driven asynchronous summarization and its conversation-state update contract.
- `api-configuration-summary`: Make the API configuration summary the CLI startup configuration source.

## Non-goals

- Adding server-side conversation persistence.
- Adding an API cancellation endpoint or canceling accepted background requests.
- Creating test projects or unit tests.
- Changing pipeline, agent, or infrastructure behavior behind the API.

## Impact

The CLI project, API configuration-file ownership, callback contracts, API request lifecycle handling, HTTP client/listener composition, duplicated CLI transport models, and OpenSpec capability specifications are affected. The CLI will require a configured API base URL, API key, and callback base URL; the API will own the merged runtime configuration required to execute pipelines. No API endpoint or processing behavior is changed.