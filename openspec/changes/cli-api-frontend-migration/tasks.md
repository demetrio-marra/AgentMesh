## 1. Configuration Ownership

- [ ] 1.1 Merge the CLI shared runtime sections into API-owned configuration files while preserving API-only authentication and hosting sections, then verify the merged JSON contains both configuration sets without exposing secret values in the task output
- [ ] 1.2 Reduce CLI configuration to API URL/key, callback base URL, and conversation summarization settings, then verify the CLI no longer binds server runtime configuration
- [ ] 1.3 Remove API configuration links to CLI files and remove CLI local pipeline registrations, then verify the API owns its configuration files and the CLI no longer owns processing composition

## 2. Public HTTP Transport Contracts

- [ ] 2.1 Add CLI-owned DTOs for conversation messages, request payloads, async acknowledgements, workflow results, and callback payloads, then verify their JSON shapes match the API OpenAPI document
- [ ] 2.2 Implement an authenticated CLI API client for configuration summary, asynchronous chat requests, and asynchronous summarization, then verify synchronous API rejection responses are surfaced without state mutation
- [ ] 2.3 Add callback URL construction from the configured complete callback base URL, then verify all five URLs are passed unchanged to the API and the default is localhost

## 3. CLI Callback and Request Lifecycle

- [ ] 3.1 Add the CLI callback HTTP listener and handlers for workflow start, step start, step completion, completion, and error, then verify each active callback invokes the existing console notifier
- [ ] 3.2 Add request ID correlation and pending-request lifecycle handling, then verify unknown or locally dropped request IDs return successful callback responses without console output or state mutation
- [ ] 3.3 Replace local pipeline execution in the console loop with asynchronous API submission and completion waiting, then verify successful results and API errors are rendered through the existing console experience
- [ ] 3.4 Preserve Ctrl+C behavior by removing the active request ID locally and abandoning its wait, then verify late callbacks are silently ignored and no cancellation API call is made

## 4. Local Conversation and Summarization

- [ ] 4.1 Implement CLI-owned conversation state updates from successful chat completion results, then verify user and assistant messages and counters are updated only for active requests
- [ ] 4.2 Route explicit `/summarize` through the asynchronous API summarization endpoint, then verify successful callbacks replace the selected older messages and failures preserve the original context
- [ ] 4.3 Add automatic summarization after chat completion when both configured thresholds are exceeded, then verify the next prompt is not accepted until the summarization completion updates local context
- [ ] 4.4 Handle summarization callback progress and terminal results using the same local correlation and abandonment rules, then verify dropped summarization requests produce no visible output

## 5. Startup and Integration Validation

- [ ] 5.1 Retrieve and display the authenticated API configuration summary at CLI startup, then verify server configuration is not read from local CLI runtime settings
- [ ] 5.2 Remove the CLI AgentMesh framework/application package reference and unused runtime imports, then verify the CLI project has no AgentMesh runtime package dependency
- [ ] 5.3 Build the changed CLI and API projects, then verify the CLI build validates the migration and the API build validates only that merged API-owned configuration is consumable; do not add API compatibility or feature validation work
- [ ] 5.4 Run a manual API/CLI smoke flow covering startup summary, one chat request, progress callbacks, Ctrl+C local abandonment, explicit summarization, and automatic summarization, then verify the documented callback and state behavior