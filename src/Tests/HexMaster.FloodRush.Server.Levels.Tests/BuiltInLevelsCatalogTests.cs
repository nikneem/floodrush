using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Tests;

public sealed class BuiltInLevelsCatalogTests
{
    private readonly BuiltInLevelsCatalog catalog = new();

    [Fact]
    public void GetReleasedLevels_ReturnsFirstReleasedLevelSummary()
    {
        var releasedLevels = catalog.GetReleasedLevels();

        var level = Assert.Single(releasedLevels);
        Assert.Equal(BuiltInLevelsCatalog.FirstLevelId, level.LevelId);
        Assert.Equal(BuiltInLevelsCatalog.FirstLevelRevision, level.Revision);
        Assert.Equal("Level 1 - First Flow", level.DisplayName);
        Assert.Equal(BuiltInLevelsCatalog.FirstLevelDifficulty, level.Difficulty);
        Assert.Equal(1, level.FlowSpeedIndicator);
    }

    [Fact]
    public void GetLevelRevision_ReturnsFirstLevelWithRequestedShape()
    {
        var level = catalog.GetLevelRevision(
            BuiltInLevelsCatalog.FirstLevelId,
            BuiltInLevelsCatalog.FirstLevelRevision);

        Assert.NotNull(level);
        Assert.Equal(BuiltInLevelsCatalog.FirstLevelDifficulty, level.Difficulty);
        Assert.Equal(10, level.BoardWidth);
        Assert.Equal(6, level.BoardHeight);
        Assert.Equal(60000, level.StartDelayMilliseconds);
        Assert.Equal(1, level.FlowSpeedIndicator);

        var startTile = Assert.Single(level.FixedTiles, tile => tile.TileType == LevelFixedTileTypeDto.StartPoint);
        Assert.Equal(0, startTile.X);
        Assert.Equal(2, startTile.Y);
        Assert.Equal(BoardDirectionDto.Right, startTile.OutputDirection);

        var finishTile = Assert.Single(level.FixedTiles, tile => tile.TileType == LevelFixedTileTypeDto.FinishPoint);
        Assert.Equal(9, finishTile.X);
        Assert.Equal(2, finishTile.Y);
        Assert.Equal(BoardDirectionDto.Left, finishTile.EntryDirection);
    }

    [Fact]
    public void GetLevelRevision_ReturnsNull_WhenRevisionDoesNotMatch()
    {
        var level = catalog.GetLevelRevision(BuiltInLevelsCatalog.FirstLevelId, "different-revision");

        Assert.Null(level);
    }
}
