## 1. API Contracts

- [x] 1.1 Add dedicated synchronous and asynchronous summarization input/output DTOs in `AgentMesh.Api`, including language, conversation, request id, summarized content, and summarized-content timestamp fields; verify the API project compiles.
- [x] 1.2 Add dedicated async callback URL validation and summarization callback payload DTOs for start, step start, step completion, completion, and error events; verify partial callback sets produce model-validation `400` responses.

## 2. Summarization Execution

- [x] 2.1 Extend the API application execution boundary to resolve exactly one `ISummarizationPipeline`, initialize it with language, conversation, and a server-generated UTC request timestamp, and map its output to the specialized synchronous response; verify success, zero-pipeline, and multiple-pipeline behaviors.
- [x] 2.2 Add background asynchronous summarization execution with per-request scope and request correlation, returning `202 Accepted` before completion and isolating routing/configuration failures as synchronous HTTP errors; verify completion and failure paths do not emit both terminal callbacks.

## 3. API Endpoints and Callbacks

- [x] 3.1 Add fixed-route authenticated synchronous and asynchronous summarization actions to `RequestsController` without a pipeline-name route or request field; verify Swagger exposes the routes and dedicated request/response schemas.
- [x] 3.2 Wire summarization callback context and delivery through the existing API callback infrastructure, preserving best-effort HTTP POST behavior and including the request id in every payload; verify callback URLs are not called when omitted.
- [x] 3.3 Keep existing chat request endpoints and `AgentMeshCLI` unchanged while updating only required API/application contracts and registrations; verify the solution builds without CLI source changes.

## 4. Verification

- [x] 4.1 Add focused API tests for authentication, input validation, single-pipeline enforcement, synchronous output mapping, asynchronous acceptance, callback payloads, and terminal error behavior; verify the targeted test suite passes.
- [x] 4.2 Run the relevant `dotnet build` and test commands and inspect the generated OpenAPI document for summarization endpoint, DTO, callback, and error documentation coverage.
