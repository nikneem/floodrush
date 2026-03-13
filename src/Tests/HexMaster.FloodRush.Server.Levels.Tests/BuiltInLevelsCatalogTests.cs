using System.Reflection;
using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;
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

    [Fact]
    public void MapFixedTile_FluidBasin_MapsCorrectly()
    {
        var fluidBasin = new FluidBasinTile(
            new Game.Core.Domain.Board.GridPosition(3, 2),
            BoardDirection.Top,
            BoardDirection.Bottom,
            5000,
            50);

        var result = InvokeMapFixedTile(fluidBasin);

        Assert.Equal(LevelFixedTileTypeDto.FluidBasin, result.TileType);
        Assert.Equal(3, result.X);
        Assert.Equal(2, result.Y);
        Assert.Equal(BoardDirectionDto.Top, result.EntryDirection);
        Assert.Equal(BoardDirectionDto.Bottom, result.OutputDirection);
        Assert.Equal(5000, result.FillDelayMilliseconds);
    }

    [Fact]
    public void MapFixedTile_SplitSection_MapsCorrectly()
    {
        var splitSection = new SplitSectionTile(
            new Game.Core.Domain.Board.GridPosition(5, 3),
            BoardDirection.Left,
            BoardDirection.Right,
            BoardDirection.Bottom,
            100,
            75);

        var result = InvokeMapFixedTile(splitSection);

        Assert.Equal(LevelFixedTileTypeDto.SplitSection, result.TileType);
        Assert.Equal(5, result.X);
        Assert.Equal(3, result.Y);
        Assert.Equal(BoardDirectionDto.Left, result.EntryDirection);
        Assert.Equal(BoardDirectionDto.Right, result.OutputDirection);
        Assert.Equal(BoardDirectionDto.Bottom, result.SecondaryOutputDirection);
        Assert.Equal(100, result.SpeedModifierPercent);
    }

    [Fact]
    public void MapDirection_InvalidDirection_ThrowsViaReflection()
    {
        var method = typeof(BuiltInLevelsCatalog)
            .GetMethod("MapDirection", BindingFlags.NonPublic | BindingFlags.Static)!;

        var ex = Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, [(object)(BoardDirection)99]));

        Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
    }

    private static LevelFixedTileDto InvokeMapFixedTile(FixedTile tile)
    {
        var method = typeof(BuiltInLevelsCatalog)
            .GetMethod("MapFixedTile", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (LevelFixedTileDto)method.Invoke(null, [tile])!;
    }
}
