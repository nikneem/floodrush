using System.Diagnostics;
using System.Diagnostics.Metrics;
using HexMaster.FloodRush.Game.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.Services;

public sealed class ApplicationExitService : IApplicationExitService
{
    private readonly ILogger<ApplicationExitService> logger;

    public ApplicationExitService(ILogger<ApplicationExitService> logger)
    {
        this.logger = logger;
    }

    public void Exit()
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("application.exit", ActivityKind.Internal);
        FloodRushTelemetry.UserActions.Add(1, new TagList
        {
            { "screen", "welcome" },
            { "action", "quit-confirmed" }
        });

        logger.LogInformation("Exiting the FloodRush application.");
        Application.Current?.Quit();
    }
}
