## Purpose

Provide a reproducible Docker deployment path for the standalone AgentMeshWeb chat client while retaining its existing server-side API boundary and deployment-time configuration model.

## ADDED Requirements

### Requirement: Web chat host SHALL provide a production Docker image
The repository SHALL provide Docker build instructions for AgentMeshWeb that build from the repository root and produce an image containing only its published .NET 8 web-host output and the matching ASP.NET Core runtime. The image SHALL start the AgentMeshWeb host and expose one documented HTTP container port.

#### Scenario: Operator builds the web chat image
- **WHEN** an operator invokes the documented Docker build command from the repository root
- **THEN** Docker produces an image for AgentMeshWeb without requiring a locally installed .NET runtime in the final image

#### Scenario: Operator starts the web chat image
- **WHEN** an operator runs the built image with the documented container-port mapping
- **THEN** the AgentMeshWeb HTTP host starts and accepts browser connections through that mapped port

### Requirement: Web chat container SHALL accept deployment-time API configuration
The AgentMeshWeb container SHALL preserve its existing configuration contract for the upstream AgentMesh API. Operators SHALL be able to provide the API base URL, API key, and API-key header name through runtime configuration, and the image SHALL NOT contain deployment credentials.

#### Scenario: Operator provides API credentials at container startup
- **WHEN** an operator supplies valid API configuration through the documented runtime environment variables
- **THEN** the running web host performs authenticated upstream API calls while the browser remains unable to read the API key

#### Scenario: Image is inspected before deployment
- **WHEN** an operator inspects the Docker build inputs and final image configuration
- **THEN** no environment-specific API key is embedded in the image layers or Docker build instructions

### Requirement: Documentation SHALL describe Docker operation and deployment readiness
The README SHALL document the Docker build command, required runtime configuration, and container run command for AgentMeshWeb. It SHALL state that the resulting image is suitable for Kubernetes deployment without adding Kubernetes manifests or Kubernetes-specific configuration to this repository.

#### Scenario: Operator follows the README
- **WHEN** an operator follows the AgentMeshWeb Docker instructions in the README
- **THEN** the operator can build and run the web chat image with its upstream API credentials configured outside the image
