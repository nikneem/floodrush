using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var tables = builder.AddAzureStorage("floodrushstorage")
    .RunAsEmulator()
    .AddTables("floodrushtables");

var api = builder.AddProject<Projects.HexMaster_FloodRush_Api>("hexmaster-floodrush-api")
    .WithReference(tables)
    .WaitFor(tables)
    .WithSeedBasicLevelsCommand()
    .WithOtlpExporter();

builder.AddExecutable(
        "hexmaster-floodrush-game",
        "dotnet",
        @"..\..\..\Game\HexMaster.FloodRush.Game",
        "run",
        "--project",
        "HexMaster.FloodRush.Game.csproj",
        "-f",
        "net10.0-windows10.0.19041.0")
    .WithReference(api)
    .WithEnvironment("FLOODRUSH_API_BASE_URL", api.GetEndpoint("https"))
    .WithOtlpExporter();

builder.Build().Run();
