var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.HexMaster_FloodRush_Api>("hexmaster-floodrush-api");

builder.Build().Run();
