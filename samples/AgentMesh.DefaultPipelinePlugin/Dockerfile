FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.config ./
COPY artifacts/packages ./artifacts/packages/
COPY samples/AgentMesh.DefaultPipelinePlugin/AgentMesh.DefaultPipelinePlugin.csproj ./samples/AgentMesh.DefaultPipelinePlugin/
RUN dotnet restore samples/AgentMesh.DefaultPipelinePlugin/AgentMesh.DefaultPipelinePlugin.csproj

COPY samples/AgentMesh.DefaultPipelinePlugin ./samples/AgentMesh.DefaultPipelinePlugin/
RUN dotnet publish samples/AgentMesh.DefaultPipelinePlugin/AgentMesh.DefaultPipelinePlugin.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "AgentMesh.DefaultPipelinePlugin.dll"]
