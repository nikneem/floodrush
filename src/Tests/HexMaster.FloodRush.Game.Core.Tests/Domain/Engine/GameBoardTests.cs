using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Engine;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Pipes;
using HexMaster.FloodRush.Game.Core.Domain.Rules;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Engine;

public sealed class GameBoardTests
{
    [Fact]
    public void PlacePipe_StoredAndRetrievable()
    {
        var board = GameBoard.FromLevel(CreateMinimalLevel());
        var pos = new GridPosition(1, 0);
        var pipe = new PlacedPipe(pos, PipeSectionType.Horizontal, 10);

        board.PlacePipe(pipe);

        Assert.Same(pipe, board.GetPlacedPipe(pos));
    }

    [Fact]
    public void RemovePipe_RemovesExistingPipe()
    {
        var board = GameBoard.FromLevel(CreateMinimalLevel());
        var pos = new GridPosition(1, 0);
        board.PlacePipe(new PlacedPipe(pos, PipeSectionType.Horizontal, 10));

        board.RemovePipe(pos);

        Assert.Null(board.GetPlacedPipe(pos));
    }

    [Fact]
    public void GetFixedTile_ReturnsCorrectTile()
    {
        var level = CreateMinimalLevel();
        var board = GameBoard.FromLevel(level);

        var tile = board.GetFixedTile(new GridPosition(0, 0));
        Assert.NotNull(tile);
        Assert.IsType<StartPointTile>(tile);
    }

    [Fact]
    public void GetFixedTile_ReturnsNullForEmptyCell()
    {
        var board = GameBoard.FromLevel(CreateMinimalLevel());
        Assert.Null(board.GetFixedTile(new GridPosition(1, 0)));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(3, 0, true)]
    [InlineData(4, 0, false)] // width=4, so x=4 is out
    [InlineData(0, 1, false)] // height=1, so y=1 is out
    public void IsWithinBounds_ReturnsCorrectValue(int x, int y, bool expected)
    {
        var board = GameBoard.FromLevel(CreateMinimalLevel());
        Assert.Equal(expected, board.IsWithinBounds(new GridPosition(x, y)));
    }

    private static LevelDefinition CreateMinimalLevel() =>
        new(
            "test",
            "Test Level",
            new BoardDimensions(4, 1),
            0,
            new FlowSpeedIndicator(50),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ]);
}
