# Unit Tests

Test projects share the .NET 8 xUnit packages and nullable/implicit-using settings in `Directory.Build.props`. A test project overrides the target framework only when its production project requires it; `CallbackViewer.Tests` targets .NET 9.

Run the complete suite and collect per-project Cobertura reports with:

```powershell
dotnet test AgentMesh.sln --collect:"XPlat Code Coverage"
```

To merge the newest report from each test project into one Cobertura file, install ReportGenerator and run:

```powershell
dotnet tool install --global dotnet-reportgenerator-globaltool
$reports = Get-ChildItem tests -Recurse -Filter coverage.cobertura.xml |
  Group-Object { $_.Directory.Parent.Parent.FullName } |
  ForEach-Object { $_.Group | Sort-Object LastWriteTime -Descending | Select-Object -First 1 }
reportgenerator "-reports:$($reports.FullName -join ';')" "-targetdir:tests/TestResults/CoverageReport" "-reporttypes:Cobertura"
```

Coverage excludes test assemblies and generated/compiler files (`*.g.cs`, `*.generated.cs`, and `obj/**/*.cs`). Unit tests also exclude host startup and live boundaries: ASP.NET listeners and hosted loops, browser rendering/SignalR delivery, live provider/network calls, and real JavaScript sandbox processes. Adapters use fakes or in-memory HTTP handlers instead.

The initial aggregate baseline covers all 13 production assemblies: 1,217 of 3,292 lines (37.0%) and 225 of 792 branches (28.4%). The lower Runtime, CLI, and Web rates reflect their intentionally unstarted host/UI paths; state, routing, authentication, validation, formatting, and deterministic error branches are covered without those external lifecycles.