## 1. Output DTOs

- [x] 1.1 Add `AgentMesh.Api/Models/Api/ConfigurationSummaryApiOutput.cs` with `SandboxServiceUrl`, `SandboxName`, `AgentId`, and `Agents` (`IReadOnlyList<AgentConfigurationSummaryApiOutput>`) and verify the project builds
- [x] 1.2 Add `AgentMesh.Api/Models/Api/AgentConfigurationSummaryApiOutput.cs` with `AgentRole`, `Model`, `Temperature` (`double`) and verify the project builds

## 2. Controller

- [x] 2.1 Create `AgentMesh.Api/Controllers/ConfigurationController.cs` with `[ApiController]`, `[Route("api")]`, `[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.SchemeName)]`, constructor-injecting `SESJSSandboxConfiguration`, `UserConfiguration`, `IEnumerable<AgentFlatConfigurationRecord>`
- [x] 2.2 Implement `GET api/configuration` mapping only `AgentUniqueRole`, `ProviderModelName`, and parsed `Temperature` per agent record (never `ProviderApiKey` or `SystemPrompt`) plus sandbox URL/name/agent id, returning `200 OK` with `ConfigurationSummaryApiOutput`
- [x] 2.3 Add XML doc comments and `[ProducesResponseType]` attributes for `200` and `401`, consistent with `RequestsController`, and verify via `AgentMeshCLI.xml`/Swagger generation that the endpoint appears in the OpenAPI doc

## 3. Verification

- [x] 3.1 Run `dotnet build AgentMesh.sln` and confirm no errors
- [x] 3.2 Manually invoke `GET api/configuration` with a valid API key against a locally run `AgentMesh.Api` and confirm the JSON response matches the fields printed by `AgentMeshCLI`'s startup summary for the same `appsettings.json`, and that no field carries a provider API key or system prompt value
- [x] 3.3 Invoke the endpoint without an API key and confirm a `401 Unauthorized` response
