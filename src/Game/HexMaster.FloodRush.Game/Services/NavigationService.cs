using System.Diagnostics;
using System.Diagnostics.Metrics;
using HexMaster.FloodRush.Game.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.Services;

public sealed class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> logger;

    public NavigationService(ILogger<NavigationService> logger)
    {
        this.logger = logger;
    }

    public Task NavigateToLevelSelectionAsync() =>
        NavigateAsync(AppRoutes.LevelSelection, "level-selection");

    public Task NavigateToGameplayAsync(string levelId) =>
        NavigateAsync($"{AppRoutes.Gameplay}?levelId={levelId}", "gameplay", levelId);

    public Task NavigateToSettingsAsync() =>
        NavigateAsync(AppRoutes.Settings, "settings");

    public Task GoBackAsync() =>
        NavigateAsync("..", "back");

    private async Task NavigateAsync(string route, string target, string? levelId = null)
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("navigation", ActivityKind.Internal);
        activity?.SetTag("navigation.target", target);
        activity?.SetTag("navigation.route", route);
        if (!string.IsNullOrWhiteSpace(levelId))
        {
            activity?.SetTag("level.id", levelId);
        }

        FloodRushTelemetry.NavigationRequests.Add(1, new TagList
        {
            { "target", target }
        });

        logger.LogInformation("Navigating to {Target}.", target);

        var stopwatch = Stopwatch.StartNew();
        await Shell.Current.GoToAsync(route);
        FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
        {
            { "operation", "navigation" },
            { "target", target }
        });
    }
}
