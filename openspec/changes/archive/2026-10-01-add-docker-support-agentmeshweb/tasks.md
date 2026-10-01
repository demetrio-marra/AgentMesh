## 1. Container Packaging

- [ ] 1.1 Add an `AgentMeshWeb` multi-stage .NET 8 Dockerfile that publishes and starts the MVC host on a stable HTTP port, then verify `docker build -f AgentMeshWeb/Dockerfile -t agentmeshweb:local .` succeeds from the repository root.
- [x] 1.2 Add or update the root `.dockerignore` to exclude build outputs, source-control metadata, and local artifacts without excluding inputs required by the AgentMeshWeb build, then verify the image build context excludes those paths.

## 2. Operations Documentation

- [x] 2.1 Add an AgentMeshWeb Docker README section with repository-root build and `docker run` commands, including deployment-time `Api__BaseUrl`, `Api__ApiKey`, and `Api__HeaderName` configuration, then verify no credential value is included in commands or image inputs.
- [x] 2.2 State in the AgentMeshWeb Docker README section that the produced image is suitable for Kubernetes deployment without adding repository Kubernetes resources, then verify the change contains no Kubernetes manifests or Kubernetes-specific application configuration.

## 3. Container Validation

- [ ] 3.1 Run the documented image with a configured upstream API and mapped HTTP port, then verify the web chat page is reachable and the browser cannot obtain the configured API key.