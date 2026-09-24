## 1. Split Pipeline Configuration

- [x] 1.1 Create `samples/AgentMesh.DefaultPipelinePlugin/pipelineSettings.json` as a valid root JSON object containing exactly the existing `InferenceProviders`, `LLMs`, `Agents`, and `CodeModeWorkflow` sections, remove only those sections from `AgentMesh.Api/appsettings.json`, and verify the moved values and remaining host settings are unchanged.
- [x] 1.2 Create local `samples/AgentMesh.DefaultPipelinePlugin/pipelineSettings.Development.json`, move the `InferenceProviders` development overrides out of `AgentMesh.Api/appsettings.Development.json` while retaining unrelated development settings, add the exact new path to `.gitignore`, and verify `git check-ignore` reports the development file as ignored.
- [x] 1.3 Update the sample plugin project to copy `pipelineSettings.json` and the optional local development override to build output with `PreserveNewest`, then build the plugin and verify the available files appear in its output without being copied into an API `Plugins` directory.

## 2. Compose API Configuration

- [x] 2.1 Add optional `pipelineSettings.json` and optional `pipelineSettings.{Environment}.json` sources to API startup after host JSON settings and before environment variables, then verify source order by inspecting the configured providers or with a focused configuration probe.
- [x] 2.2 Verify the API starts without either pipeline settings file and follows existing unavailable-plugin behavior instead of failing during configuration loading.
- [x] 2.3 Verify a deployed base pipeline file is visible to plugin bindings, an environment-specific pipeline value overrides its base value, and an environment variable overrides both.

## 3. Document Deployment Contract

- [x] 3.1 Update README setup and plugin deployment guidance to require placing `pipelineSettings.json` from the plugin output in the API runtime base directory, identify its four sections, and verify the README does not mention `pipelineSettings.Development.json`.

## 4. Validate The Change

- [x] 4.1 Parse all edited JSON files, build `AgentMesh.Api` and `AgentMesh.DefaultPipelinePlugin`, and verify the builds complete without errors.
- [x] 4.2 Inspect the final diff to confirm no settings outside the four named sections moved, the development override is absent from tracked changes, and only the planned code, configuration, ignore, README, and OpenSpec files changed.