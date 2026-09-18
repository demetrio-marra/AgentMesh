# API Reachability Inventory

Baseline captured before migration on 2026-09-18.

## Retain In `AgentMesh`

These files are required by `AgentMesh.Api` source or by the transitive closure of its public API-facing contracts.

- `Configuration`:
  - none
- `Exceptions`:
  - `PipelineRoutingException.cs`
- `Models`:
  - `AgentExecutionCost.cs`
  - `AgentMeshConfiguration.cs`
  - `ContextMessage.cs`
  - `ContextMessageRole.cs`
  - `EWDisplayParameterRecord.cs`
  - `EWStepStatisticsRecord.cs`
  - `ParameterMutation.cs`
  - `SummarizationResult.cs`
  - `WorkflowResult.cs`
- `Services`:
  - `CallbackNotifierContext.cs`
  - `IAgentMeshPluginBootstrap.cs`
  - `IAppInstance.cs`
  - `IWorkflowProgressNotifier.cs`

## Move To `AgentMesh.Contracts`

These definitions are shared by Application and infrastructure adapters but are not API-reachable:

- `Configuration/AgentFlatConfigurationRecord.cs`
- `Configuration/ResilienceConfiguration.cs`
- all files under `Contracts/`
- `Exceptions/BadAgentResponseException.cs`
- `Exceptions/BadStructuredResponseException.cs`
- `Exceptions/CodeSandboxCallException.cs`
- `Exceptions/EmptyAgentResponseException.cs`
- all files under `Models/AgentMemory/`, `Models/ChatClient/`, `Models/ChatMessages/`, `Models/CodeSandbox/`, `Models/Knowledge/`, and `Models/Rerank/`
- `Services/IJSSandbox.cs`
- `Services/IKnowledgeService.cs`
- `Services/IRerankerService.cs`
- `Utils/Resilience.cs`

## Move To `AgentMesh.Application`

These definitions are application runtime functionality or Application-only contracts:

- `Models/BaseEWParameterConfiguration.cs`
- `Models/CommitResult.cs`
- `Models/CommitResultItem.cs`
- `Models/EWAgenticStepExecutionResult.cs`
- `Models/EWParameterConstants.cs`
- `Models/EWStepExecutionResult.cs`
- `Models/IEWParameterConfiguration.cs`
- `Models/ParameterStoreItem.cs`
- `Models/ParametersSnapshot.cs`
- `Services/DefaultEWParameterSerializer.cs`
- `Services/EWPipeline.cs`
- `Services/IChatRequestPipeline.cs`
- `Services/IEWAgent.cs`
- `Services/IEWAgenticStep.cs`
- `Services/IEWParameterSerializer.cs`
- `Services/IEWPipeline.cs`
- `Services/IEWStep.cs`
- `Services/IParameterStore.cs`
- `Services/ISummarizationPipeline.cs`
- `Services/OmittedValueEWParameterSerializer.cs`
- `Services/ParameterStore.cs`
- `Utils/ListsFormatter.cs`
- `Utils/SerializationUtils.cs`
- `Utils/TypesUtils.cs`

`Models/EWDisplayDiffParameterRecord.cs` was retained in core because it is exposed by the API callback payload through `EWStepStatisticsRecord`.

## Boundary Checks

- `AgentMesh.Api/AgentMesh.Api.csproj` references `AgentMesh` only and has no Application reference.
- `AgentMesh.Api` currently builds successfully with `dotnet build AgentMesh.Api/AgentMesh.Api.csproj --no-restore`.
- The retained set is based on API source, global usings, public signatures, and transitive contract types, not folder names alone.

## Package And Versioning Impact

- `AgentMesh.Framework.Application` remains the only distributable NuGet package.
- `AgentMesh`, `AgentMesh.Contracts`, and infrastructure projects are build-time project references; no infrastructure NuGet packages are required by this migration.
- Third-party package versions remain unchanged, including Microsoft.Extensions `10.0.9`, Polly `8.3.1`, OpenAI `2.11.0`, and Swashbuckle `6.6.2`.