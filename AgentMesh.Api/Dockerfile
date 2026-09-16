FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore AgentMesh.Api/AgentMesh.Api.csproj
RUN dotnet publish AgentMesh.Api/AgentMesh.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "AgentMesh.Api.dll"]
