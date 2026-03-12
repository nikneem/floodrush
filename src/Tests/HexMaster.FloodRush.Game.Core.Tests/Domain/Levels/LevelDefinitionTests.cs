using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Rules;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Levels;

public sealed class LevelDefinitionTests
{
    [Fact]
    public void Constructor_RejectsLevelsWithoutFinishPoints()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new LevelDefinition(
            "level-1",
            new BoardDimensions(3, 3),
            2000,
            new FlowSpeedIndicator(25),
            [new StartPointTile(new GridPosition(0, 0), BoardDirection.Right)]));

        Assert.Contains("finish point", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_RejectsDuplicateFixedTilePositions()
    {
        var sharedPosition = new GridPosition(1, 1);

        var exception = Assert.Throws<InvalidOperationException>(() => new LevelDefinition(
            "level-1",
            new BoardDimensions(3, 3),
            2000,
            new FlowSpeedIndicator(25),
            [
                new StartPointTile(new GridPosition(0, 1), BoardDirection.Right),
                new FinishPointTile(sharedPosition, BoardDirection.Left),
                new FluidBasinTile(sharedPosition, BoardDirection.Left, BoardDirection.Right, 500, 20)
            ]));

        Assert.Contains("cannot occupy", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_RejectsUnreachableFinishPoints()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new LevelDefinition(
            "level-2",
            new BoardDimensions(3, 1),
            1000,
            new FlowSpeedIndicator(40),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FinishPointTile(new GridPosition(2, 0), BoardDirection.Right)
            ]));

        Assert.Contains("reachable", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_AcceptsValidLevels()
    {
        var level = CreateValidLevel();

        Assert.Equal("level-valid", level.LevelId);
        Assert.Equal(5, level.FixedTiles.Count);
        Assert.Equal(30, level.FlowSpeedIndicator.Value);
    }

    [Fact]
    public void SetBoardDimensions_RejectsShrinkingBoardBelowExistingTiles()
    {
        var level = CreateValidLevel();

        Assert.Throws<InvalidOperationException>(() => level.SetBoardDimensions(new BoardDimensions(2, 1)));

        Assert.Equal(4, level.BoardDimensions.Width);
        Assert.Equal(2, level.BoardDimensions.Height);
    }

    [Fact]
    public void SetFlowSpeedIndicator_UpdatesWhenValueIsValid()
    {
        var level = CreateValidLevel();

        level.SetFlowSpeedIndicator(new FlowSpeedIndicator(55));

        Assert.Equal(55, level.FlowSpeedIndicator.Value);
    }

    [Fact]
    public void SetStartDelayMilliseconds_UpdatesWhenValueIsValid()
    {
        var level = CreateValidLevel();

        level.SetStartDelayMilliseconds(2200);

        Assert.Equal(2200, level.StartDelayMilliseconds);
    }

    [Fact]
    public void AddFixedTile_AddsAnotherReachableFinishPoint()
    {
        var level = new LevelDefinition(
            "level-start",
            new BoardDimensions(4, 2),
            1500,
            new FlowSpeedIndicator(30),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ]);

        level.AddFixedTile(new FinishPointTile(new GridPosition(2, 1), BoardDirection.Top));

        Assert.Equal(3, level.FixedTiles.Count);
    }

    private static LevelDefinition CreateValidLevel() =>
        new(
            "level-valid",
            new BoardDimensions(4, 2),
            1500,
            new FlowSpeedIndicator(30),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FluidBasinTile(new GridPosition(1, 0), BoardDirection.Left, BoardDirection.Right, 400, 20),
                new SplitSectionTile(new GridPosition(2, 0), BoardDirection.Left, BoardDirection.Right, BoardDirection.Bottom, 85, 30),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left),
                new FinishPointTile(new GridPosition(2, 1), BoardDirection.Top)
            ]);
}
