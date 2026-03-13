using HexMaster.FloodRush.Server.Levels;
using HexMaster.FloodRush.Server.Profiles;
using HexMaster.FloodRush.Server.Scores;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddProfilesModule(builder.Configuration);
builder.Services.AddLevelsModule(builder.Configuration);
builder.Services.AddScoresModule(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapProfilesModule();
app.MapLevelsModule();
app.MapScoresModule();

app.Run();
