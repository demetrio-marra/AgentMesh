## Why

The OpenAI chat-completions adapter is housed in an infrastructure project named `AgentMesh.Infrastructure.OpenAIClient`. That name describes a vendor client rather than the adapter's chat-completions responsibility, making the project and its namespaces less clear as the infrastructure layer grows.

## What Changes

- Rename the `AgentMesh.Infrastructure.OpenAIClient` project directory and project file to `AgentMesh.Infrastructure.ChatCompletions`.
- Rename the namespaces declared by `OpenAIClient.cs` and `OpenAIClientFactory.cs` to `AgentMesh.Infrastructure.ChatCompletions`.
- Update solution, project-reference, and application import references so the application continues to register `OpenAIClientFactory` through the renamed namespace.
- Validate the refactor by compiling the solution; no business-logic validation is in scope.

## Capabilities

### New Capabilities

- `chat-completions-infrastructure`: Establishes the stable project and namespace identity for the chat-completions infrastructure adapter.

### Modified Capabilities

- None. This is a source-organization refactor with no intended runtime behavior change.

## Impact

- Affected files include the infrastructure project directory and its two adapter source files, `AgentMesh.sln`, `AgentMesh.Application/AgentMesh.Application.csproj`, and `AgentMesh.Application/ApplicationRuntime.cs`.
- Build and tooling paths that reference the old project name must use the new project name.
- Public contract types such as `IOpenAIClient` and `IOpenAIClientFactory` remain unchanged.

## Non-goals

- Changing OpenAI request handling, configuration, dependency-injection lifetime, or contract behavior.
- Renaming `OpenAIClient` or `OpenAIClientFactory` types.
- Adding or running business-logic, integration, or live-provider validation beyond compilation.