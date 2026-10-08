## 1. Standalone Project Setup

- [x] 1.1 Create the .NET 8 `AgentMesh.Infrastructure.Responses` class library with a project reference to `AgentMesh.Contracts`, required implicit/global usings, and stable `OpenAI` SDK dependency; verify `dotnet restore AgentMesh.Infrastructure.Responses/AgentMesh.Infrastructure.Responses.csproj` succeeds.
- [x] 1.2 Confirm the selected stable SDK exposes `OpenAI.Responses.ResponsesClient`, streaming response updates, custom endpoint configuration, and reasoning-effort options on .NET 8; verify the standalone project compiles against those APIs with only a narrowly scoped `OPENAI001` suppression where required.
- [x] 1.3 Include the Responses library and focused test project in `AgentMesh.sln`; do not add project references from existing runtime projects, DI registration, configuration entries, or factory-selection changes; verify composition roots remain unchanged.

## 2. Responses Adapter

- [x] 2.1 Implement a concrete Responses client that validates and accepts model, API key, endpoint, temperature, system prompt, and reasoning-effort constructor inputs, then creates the stable SDK client with the configured endpoint; verify constructor validation tests pass.
- [x] 2.2 Implement both `IChatCompletionsClient.GenerateResponseAsync` overloads, translating string inputs and `AgentMessage` system/user/assistant content into one Responses request while retaining the existing merged-system-prompt behavior; verify translation tests cover each input form and role.
- [x] 2.3 Configure each request for the supplied temperature, reasoning effort, and streaming; aggregate ordered output-text deltas and final token usage into the unchanged `ChatClientResponse`; verify a simulated text-and-usage stream returns the expected text and all three token counts.
- [x] 2.4 Recognize and retain reasoning items, deltas, or summaries emitted by the SDK in a separate internal accumulator without adding them to `ChatClientResponse`; verify a stream with reasoning and output text returns only output text through the public contract.
- [x] 2.5 Preserve cancellation propagation and the existing empty/whitespace response failure semantics while allowing other SDK failures to surface consistently; verify cancellation and empty-output tests pass.

## 3. Focused Verification

- [x] 3.1 Add an isolated test project inside the repository and include it in `AgentMesh.sln`; simulate the SDK stream without credentials or network access and verify request translation, text/usage aggregation, reasoning capture, cancellation, and empty output.
- [x] 3.2 Build the standalone Responses project and run its focused tests; verify `dotnet build AgentMesh.Infrastructure.Responses/AgentMesh.Infrastructure.Responses.csproj` and the focused test command succeed.
- [x] 3.3 Build `AgentMesh.Infrastructure.ChatCompletions/AgentMesh.Infrastructure.ChatCompletions.csproj` without modifying it; verify the existing adapter still builds and no Responses project reference has been introduced.