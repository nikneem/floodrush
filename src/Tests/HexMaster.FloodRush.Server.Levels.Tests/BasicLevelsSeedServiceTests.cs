using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Tests;

public sealed class BasicLevelsSeedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_SeedsLevels_InDevelopmentEnvironment()
    {
        var repo = new TrackingLevelsRepository { SeededCount = 3 };
        using var service = BuildService(repo, Environments.Development);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.True(repo.SeedWasCalled);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotSeed_InProductionEnvironment()
    {
        var repo = new TrackingLevelsRepository();
        using var service = BuildService(repo, Environments.Production);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.False(repo.SeedWasCalled);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotThrow_WhenSeedingFails()
    {
        var repo = new ThrowingLevelsRepository();
        using var service = BuildService(repo, Environments.Development);

        var ex = await Record.ExceptionAsync(async () =>
        {
            await service.StartAsync(CancellationToken.None);
            await service.StopAsync(CancellationToken.None);
        });

        Assert.Null(ex);
    }

    private static BasicLevelsSeedService BuildService(ILevelsRepository repo, string environmentName)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILevelsRepository>(repo);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new BasicLevelsSeedService(
            scopeFactory,
            new FakeHostEnvironment(environmentName),
            NullLogger<BasicLevelsSeedService>.Instance);
    }
}

internal sealed class TrackingLevelsRepository : ILevelsRepository
{
    public bool SeedWasCalled { get; private set; }
    public int SeededCount { get; init; }

    public ValueTask<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
        string profileId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyCollection<ReleasedLevelSummaryDto>>([]);

    public ValueTask<LevelRevisionDto?> GetLevelRevisionAsync(
        string profileId, string levelId, string revision, CancellationToken cancellationToken) =>
        ValueTask.FromResult<LevelRevisionDto?>(null);

    public async ValueTask<int> SeedBasicLevelsAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        SeedWasCalled = true;
        return SeededCount;
    }
}

internal sealed class ThrowingLevelsRepository : ILevelsRepository
{
    public ValueTask<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
        string profileId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyCollection<ReleasedLevelSummaryDto>>([]);

    public ValueTask<LevelRevisionDto?> GetLevelRevisionAsync(
        string profileId, string levelId, string revision, CancellationToken cancellationToken) =>
        ValueTask.FromResult<LevelRevisionDto?>(null);

    public ValueTask<int> SeedBasicLevelsAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Simulated storage failure.");
}

internal sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
{
    public string ApplicationName { get; set; } = "TestApp";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = environmentName;
}

