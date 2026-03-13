using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Tests;

public sealed class BasicLevelsSeedCatalogTests
{
    private readonly BasicLevelsSeedCatalog catalog = new();

    [Fact]
    public void GetLevels_ReturnsThreeBasicEasyLevels()
    {
        var levels = catalog.GetLevels();

        Assert.Collection(
            levels,
            level => AssertBasicLevel(level, "level-002", "basic-release-002", "Level 2 - Basic Flow", 2),
            level => AssertBasicLevel(level, "level-003", "basic-release-003", "Level 3 - Basic Flow", 5),
            level => AssertBasicLevel(level, "level-004", "basic-release-004", "Level 4 - Basic Flow", 7));
    }

    [Fact]
    public void GetReleasedLevels_ReturnsMatchingReleasedSummaries()
    {
        var releasedLevels = catalog.GetReleasedLevels();

        Assert.Equal(3, releasedLevels.Count);
        Assert.All(releasedLevels, level =>
        {
            Assert.Equal("Easy", level.Difficulty);
            Assert.Equal(1, level.FlowSpeedIndicator);
        });
    }

    private static void AssertBasicLevel(
        LevelRevisionDto level,
        string levelId,
        string revision,
        string displayName,
        int row)
    {
        Assert.Equal(levelId, level.LevelId);
        Assert.Equal(revision, level.Revision);
        Assert.Equal(displayName, level.DisplayName);
        Assert.Equal("Easy", level.Difficulty);
        Assert.Equal(10, level.BoardWidth);
        Assert.Equal(10, level.BoardHeight);
        Assert.Equal(30000, level.StartDelayMilliseconds);
        Assert.Equal(1, level.FlowSpeedIndicator);

        var startTile = Assert.Single(level.FixedTiles, tile => tile.TileType == LevelFixedTileTypeDto.StartPoint);
        Assert.Equal(0, startTile.X);
        Assert.Equal(row, startTile.Y);
        Assert.Equal(BoardDirectionDto.Right, startTile.OutputDirection);

        var finishTile = Assert.Single(level.FixedTiles, tile => tile.TileType == LevelFixedTileTypeDto.FinishPoint);
        Assert.Equal(9, finishTile.X);
        Assert.Equal(row, finishTile.Y);
        Assert.Equal(BoardDirectionDto.Left, finishTile.EntryDirection);
    }
}
