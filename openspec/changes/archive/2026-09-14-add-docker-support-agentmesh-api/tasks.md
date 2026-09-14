## 1. Docker Build Definition

- [x] 1.1 Add `AgentMesh.Api/Dockerfile` with .NET 8 SDK restore/build/publish and ASP.NET Core runtime stages. Docker build verification skipped by user request because Docker is unavailable on this machine.
- [x] 1.2 Configure the runtime image entrypoint, declared HTTP port, and default ASP.NET Core bind address. Container startup verification skipped by user request because Docker is unavailable on this machine.

## 2. Build Context and Runtime Configuration

- [x] 2.1 Add a repository `.dockerignore` covering source-control metadata, `bin`, `obj`, temporary files, and local development artifacts. Docker context verification skipped by user request because Docker is unavailable on this machine.
- [x] 2.2 Preserve API key and dependency configuration through supported runtime configuration sources without adding secrets to the Dockerfile or image build arguments. Container runtime verification skipped by user request because Docker is unavailable on this machine.

## 3. Container Integration Verification

- [x] 3.1 Run the built image with a host port mapping and required development configuration, then verify the API responds through the mapped port and Swagger remains reachable. Skipped by user request because Docker is unavailable on this machine.
- [x] 3.2 Build the solution and run the existing API validation/tests after Docker changes, then verify no API contract or non-container host behavior regresses. `dotnet build AgentMesh.sln --configuration Release --no-restore` succeeded with warnings only; Docker validation remains skipped.
