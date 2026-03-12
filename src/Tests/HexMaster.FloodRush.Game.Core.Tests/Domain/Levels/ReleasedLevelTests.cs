using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Rules;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Levels;

public sealed class ReleasedLevelTests
{
    [Fact]
    public void Constructor_SetsInitialState()
    {
        var token = LevelRevisionToken.New();
        var released = new ReleasedLevel("level-1", token);

        Assert.Equal("level-1", released.LevelId);
        Assert.Same(token, released.LatestRevisionToken);
        Assert.Null(released.CachedRevision);
        Assert.Equal(LevelCacheStatus.NotDownloaded, released.CacheStatus);
        Assert.False(released.IsPlayable);
    }

    [Fact]
    public void Constructor_RejectsBlankLevelId()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new ReleasedLevel("", LevelRevisionToken.New()));
    }

    [Fact]
    public void Constructor_RejectsNullToken()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReleasedLevel("level-1", null!));
    }

    [Fact]
    public void SetCachedRevision_WhenRevisionMatchesLatest_StatusIsCached()
    {
        var token = LevelRevisionToken.New();
        var released = new ReleasedLevel("level-1", token);
        var revision = CreateRevision("level-1", token);

        released.SetCachedRevision(revision);

        Assert.Equal(LevelCacheStatus.Cached, released.CacheStatus);
        Assert.True(released.IsPlayable);
    }

    [Fact]
    public void SetCachedRevision_WhenLatestUpdated_StatusBecomesObsolete()
    {
        var oldToken = new LevelRevisionToken("rev-1");
        var released = new ReleasedLevel("level-1", oldToken);
        var revision = CreateRevision("level-1", oldToken);
        released.SetCachedRevision(revision);

        var newToken = new LevelRevisionToken("rev-2");
        released.UpdateLatestRevision(newToken);

        Assert.Equal(LevelCacheStatus.Obsolete, released.CacheStatus);
        Assert.True(released.IsPlayable); // still playable with old cached revision
    }

    [Fact]
    public void SetCachedRevision_RejectsRevisionForDifferentLevel()
    {
        var token = LevelRevisionToken.New();
        var released = new ReleasedLevel("level-1", token);
        var wrongRevision = CreateRevision("level-WRONG", token);

        Assert.Throws<InvalidOperationException>(() => released.SetCachedRevision(wrongRevision));
    }

    [Fact]
    public void SetCachedRevision_RejectsNull()
    {
        var released = new ReleasedLevel("level-1", LevelRevisionToken.New());
        Assert.Throws<ArgumentNullException>(() => released.SetCachedRevision(null!));
    }

    [Fact]
    public void UpdateLatestRevision_RejectsNull()
    {
        var released = new ReleasedLevel("level-1", LevelRevisionToken.New());
        Assert.Throws<ArgumentNullException>(() => released.UpdateLatestRevision(null!));
    }

    private static LevelRevision CreateRevision(string levelId, LevelRevisionToken token)
    {
        var definition = new LevelDefinition(
            levelId,
            "Test Level",
            new BoardDimensions(4, 2),
            1000,
            new FlowSpeedIndicator(50),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ]);

        return new LevelRevision(token, definition, new LevelMetadata("Test Level"));
    }
}
