var builder = DistributedApplication.CreateBuilder(args);

var tables = builder.AddAzureStorage("floodrushstorage")
    .RunAsEmulator()
    .AddTables("floodrushtables");

builder.AddProject<Projects.HexMaster_FloodRush_Api>("hexmaster-floodrush-api")
    .WithReference(tables)
    .WaitFor(tables);

builder.Build().Run();
