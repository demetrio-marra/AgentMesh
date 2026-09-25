## 1. Update the Plugin Reference Contract

- [x] 1.1 Update `openspec/specs/default-chat-pipeline-plugin/spec.md` to replace the bundled sample requirement with the external `AMCodePipeline` reference repository and verify its requirements no longer promise an in-repository executable sample.

## 2. Update Developer Documentation

- [x] 2.1 Replace the README's local sample description and `dotnet run` command with an `AMCodePipeline` repository link and verify no README link or command targets `samples/AgentMesh.DefaultPipelinePlugin`.
- [x] 2.2 Replace the README's default-plugin Docker build/run guidance with a link to the external project's maintained Docker instructions and verify the README still explains that plugins own their Dockerfiles and images.

## 3. Validate Documentation

- [x] 3.1 Run `openspec validate externalize-default-plugin-sample --strict` and a repository search for `samples/AgentMesh.DefaultPipelinePlugin` to verify the change artifacts are valid and stale documentation references are removed.