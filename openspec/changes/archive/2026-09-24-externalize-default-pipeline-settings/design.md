## Context

See `proposal.md` for motivation. `AgentMesh.Api/Program.cs` currently clears the default configuration sources and rebuilds them from required `appsettings.json`, optional environment-specific host settings, and environment variables before invoking `PluginBootstrapLoader`. The sample plugin then binds `CodeModeWorkflow` directly and delegates common provider, model, and agent registration to `ApplicationRuntime`, all using the established top-level section names.

The four pipeline-owned sections currently live in `AgentMesh.Api/appsettings.json`; provider secrets are overridden in the tracked API development settings. The sample plugin project already copies prompt content to output, but it does not provide a pipeline configuration artifact. There is no automated test project in the repository, so validation must combine builds, output inspection, and focused startup checks.

The configured architecture-baseline path is absent. This design instead follows the current architecture documentation and project metadata: `AgentMesh.Api` owns hosting and source composition, while `AgentMesh.DefaultPipelinePlugin` owns the default pipeline's model, agent, and workflow choices.

## Goals / Non-Goals

**Goals:**
- Preserve existing configuration section paths so current binding code requires no changes.
- Make source ordering deterministic and retain environment-variable precedence.
- Keep host startup tolerant of absent pipeline settings.
- Make the public pipeline template deployable with the sample plugin while keeping local secret overrides untracked.

**Non-Goals:**
- Automatically copy plugin output into the API output or `Plugins` directory.
- Add a new configuration namespace or change any options classes.
- Generalize arbitrary per-plugin configuration discovery.
- Commit or document local development credentials.

## Decisions

### Keep pipeline sections at the JSON document root

`pipelineSettings.json` will be a normal JSON root object containing exactly `InferenceProviders`, `LLMs`, `Agents`, and `CodeModeWorkflow`. This preserves the keys consumed by existing framework and plugin registrations.

Alternative considered: wrap the sections under a named `PipelineSettings` property and add an empty matching object to host settings. Rejected because it changes every binding path and provides no benefit: .NET configuration already merges independent JSON root objects.

### Compose sources explicitly in host startup

The API will add sources in this order:

1. required `appsettings.json`
2. optional `appsettings.{Environment}.json`
3. optional `pipelineSettings.json`
4. optional `pipelineSettings.{Environment}.json`
5. environment variables

Both pipeline files are optional, so a bare host retains its existing startup behavior. The environment-specific pipeline file follows the same convention as host settings and overrides the base pipeline file only in the matching environment. Environment variables remain last and therefore authoritative.

Alternative considered: have the plugin load its own configuration during bootstrap. Rejected because the host passes an already-built `IConfiguration` to the bootstrap and configuration sources must be composed before framework registrations bind their values.

### Treat the public and development files differently

The plugin project will copy `pipelineSettings.json` to its build output with `PreserveNewest`. It will also conditionally copy `pipelineSettings.Development.json` when that local file exists. The exact development-file path will be added to `.gitignore`; existing provider overrides will move there, while unrelated API development settings remain in `appsettings.Development.json`.

Alternative considered: commit a development template with placeholder secrets. Rejected because the request explicitly reserves that file for sensitive local values and excludes it from source control. The committed base file remains the non-secret distributable template.

### Keep deployment placement explicit

Because the API configuration base path is `AppContext.BaseDirectory`, operators must place `pipelineSettings.json` in the API runtime base directory, alongside `appsettings.json`, even when the plugin DLL itself is under `Plugins`. The README will state this requirement and explain that the file comes from the pipeline plugin output. It will not mention the development override.

Alternative considered: search the configured plugin directory for settings. Rejected because this would couple configuration discovery to assembly discovery and introduce ambiguity about which file wins.

## Risks / Trade-offs

- [An operator deploys the plugin DLL but omits its settings] -> Keep loading optional so the host starts, document the required deployment file, and rely on existing plugin-configuration failure behavior when requests need the unavailable pipeline.
- [The plugin output and host runtime use different directories] -> Document that the copied file is a deployment artifact that must be placed in the API base directory.
- [A tracked development file currently contains moved provider overrides] -> Relocate only the `InferenceProviders` development section to the ignored file and verify the tracked file retains all unrelated settings.
- [Configuration precedence changes values unexpectedly] -> Keep environment variables last and validate base, environment-specific, and absent-file startup cases.
- [An ignored development file is absent on another workstation] -> Make both loading and project inclusion tolerant of absence; each developer supplies local secrets independently.

## Migration Plan

1. Create the committed plugin-owned base file by moving only the four named sections from API host settings.
2. Create the local development override and move only pipeline provider overrides from API development settings; add its exact path to `.gitignore`.
3. Configure the sample plugin project to copy both files when present.
4. Add the two optional pipeline sources to API startup in the defined order.
5. Update README deployment/setup guidance without referring to the development override.
6. Build the API and plugin, verify output contents and Git ignore behavior, and exercise startup with and without pipeline settings.

Rollback restores the four sections to API host settings, removes the optional pipeline sources, and stops deploying the separate pipeline file. No persisted data migration is involved.