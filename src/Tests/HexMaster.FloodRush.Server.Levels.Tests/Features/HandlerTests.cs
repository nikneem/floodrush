using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Server.Levels.Features.GetLevelRevision;
using HexMaster.FloodRush.Server.Levels.Features.GetReleasedLevels;
using HexMaster.FloodRush.Server.Levels.Features.SeedBasicLevels;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Tests.Features;

public sealed class GetLevelRevisionQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_DelegatesToRepository_AndReturnsResult()
    {
        var expected = new LevelRevisionDto("level-001", "v1", "Level 1", "Easy", 10, 8, 30000, 50, []);
        var repo = new StubLevelsRepository { LevelRevision = expected };
        var handler = new GetLevelRevisionQueryHandler(repo);

        var result = await handler.HandleAsync(
            new GetLevelRevisionQuery("profile-1", "level-001", "v1"),
            CancellationToken.None);

        Assert.Equal(expected, result);
        Assert.Equal("profile-1", repo.LastProfileId);
        Assert.Equal("level-001", repo.LastLevelId);
        Assert.Equal("v1", repo.LastRevision);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNull_WhenRepositoryReturnsNull()
    {
        var repo = new StubLevelsRepository { LevelRevision = null };
        var handler = new GetLevelRevisionQueryHandler(repo);

        var result = await handler.HandleAsync(
            new GetLevelRevisionQuery("profile-1", "level-missing", "v99"),
            CancellationToken.None);

        Assert.Null(result);
    }
}

public sealed class GetReleasedLevelsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsReleasedLevelsFromRepository()
    {
        var levels = new List<ReleasedLevelSummaryDto>
        {
            new("level-001", "v1", "Level 1", "Easy", 50, DateTimeOffset.UtcNow)
        };
        var repo = new StubLevelsRepository { ReleasedLevels = levels };
        var handler = new GetReleasedLevelsQueryHandler(repo);

        var result = await handler.HandleAsync(
            new GetReleasedLevelsQuery("profile-1"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(result.Levels);
        Assert.Equal("level-001", result.Levels.First().LevelId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyResponse_WhenNoLevels()
    {
        var repo = new StubLevelsRepository { ReleasedLevels = [] };
        var handler = new GetReleasedLevelsQueryHandler(repo);

        var result = await handler.HandleAsync(
            new GetReleasedLevelsQuery("profile-1"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Levels);
    }
}

public sealed class SeedBasicLevelsCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSeededCount_FromRepository()
    {
        var repo = new StubLevelsRepository { SeededCount = 3 };
        var handler = new SeedBasicLevelsCommandHandler(repo);

        var result = await handler.HandleAsync(
            new SeedBasicLevelsCommand(),
            CancellationToken.None);

        Assert.Equal(3, result.SeededCount);
    }

    [Fact]
    public async Task HandleAsync_ReturnsZero_WhenNothingSeeded()
    {
        var repo = new StubLevelsRepository { SeededCount = 0 };
        var handler = new SeedBasicLevelsCommandHandler(repo);

        var result = await handler.HandleAsync(
            new SeedBasicLevelsCommand(),
            CancellationToken.None);

        Assert.Equal(0, result.SeededCount);
    }
}

/// <summary>Manual stub for ILevelsRepository used across handler tests.</summary>
internal sealed class StubLevelsRepository : ILevelsRepository
{
    public IReadOnlyCollection<ReleasedLevelSummaryDto> ReleasedLevels { get; set; } = [];
    public LevelRevisionDto? LevelRevision { get; set; }
    public int SeededCount { get; set; }

    public string? LastProfileId { get; private set; }
    public string? LastLevelId { get; private set; }
    public string? LastRevision { get; private set; }

    public ValueTask<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
        string profileId, CancellationToken cancellationToken)
    {
        LastProfileId = profileId;
        return ValueTask.FromResult(ReleasedLevels);
    }

    public ValueTask<LevelRevisionDto?> GetLevelRevisionAsync(
        string profileId, string levelId, string revision, CancellationToken cancellationToken)
    {
        LastProfileId = profileId;
        LastLevelId = levelId;
        LastRevision = revision;
        return ValueTask.FromResult(LevelRevision);
    }

    public ValueTask<int> SeedBasicLevelsAsync(CancellationToken cancellationToken) =>
        ValueTask.FromResult(SeededCount);
}
