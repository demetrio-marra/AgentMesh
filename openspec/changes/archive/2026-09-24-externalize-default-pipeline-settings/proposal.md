## Why

The API host currently owns configuration sections that describe the sample default pipeline rather than the host itself. Separating those values into plugin-owned settings makes the deployment boundary clearer while allowing the API to start when no pipeline configuration file is supplied.

## What Changes

- Add `pipelineSettings.json` to the default pipeline plugin project with only `InferenceProviders`, `LLMs`, `Agents`, and `CodeModeWorkflow`, preserving their existing top-level configuration keys.
- Copy `pipelineSettings.json` to the plugin build output so operators can deploy it with the plugin.
- Add a development-only `pipelineSettings.Development.json` for sensitive local overrides, copy it to build output when present, and exclude it from Git.
- Load `pipelineSettings.json` optionally during API startup, followed by optional `pipelineSettings.{Environment}.json`, before environment variables so later sources retain override precedence.
- Remove only the four transferred sections from the API host's `appsettings.json`; all other host and infrastructure settings remain there.
- Update the README and capability specifications to state that a deployed pipeline must provide its configuration file, while keeping the development override undocumented as requested.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `plugin-pipeline-hosting`: Define optional host loading and precedence for externally supplied pipeline configuration while preserving startup without the file.
- `default-chat-pipeline-plugin`: Require the sample plugin to provide and copy its pipeline-owned configuration template for deployment.

## Impact

- Affected projects: `AgentMesh.Api` startup/configuration and `samples/AgentMesh.DefaultPipelinePlugin` content packaging.
- Affected deployment contract: operators deploying the default pipeline provide `pipelineSettings.json` beside the API's runtime configuration output; absence does not crash the host, but a pipeline that depends on missing settings may remain unavailable through existing plugin-validation behavior.
- Affected documentation: root README setup and plugin-hosting guidance, plus the two existing OpenSpec capabilities above.
- Configuration keys and binding classes remain unchanged because the moved sections stay at the JSON root.

## Non-goals

- Moving `Logging`, `ApiAuth`, `User`, `PluginHost`, `Resilience`, `SESJSSandbox`, conversation summarization, memory, LightRAG, or reranker settings.
- Changing plugin discovery, dependency injection contracts, endpoint behavior, or configuration section names.
- Documenting or committing `pipelineSettings.Development.json`; it exists only for local development secrets.