using HexMaster.FloodRush.Server.Levels.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Server.Levels;

/// <summary>
/// Ensures the basic seed levels exist in storage on every API startup.
/// Azurite is ephemeral; after a restart all seeded data is gone. This service
/// re-seeds the basic levels automatically so developers do not have to trigger
/// the Aspire dashboard command after every AppHost restart.
/// </summary>
internal sealed class BasicLevelsSeedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<BasicLevelsSeedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILevelsRepository>();

        try
        {
            var seededCount = await repository.SeedBasicLevelsAsync(stoppingToken);
            logger.LogInformation(
                "BasicLevelsSeedService: ensured {Count} basic seed levels are present in storage.",
                seededCount);
        }
        catch (Exception exception)
        {
            // Non-fatal: the API should still start even if seeding fails (e.g., storage not ready yet).
            logger.LogWarning(exception, "BasicLevelsSeedService: auto-seed failed. Basic levels may not be available.");
        }
    }
}
