using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Engine;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Pipes;
using HexMaster.FloodRush.Game.Core.Domain.Rules;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Engine;

public sealed class PlacedPipeTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var pos = new GridPosition(2, 3);
        var pipe = new PlacedPipe(pos, PipeSectionType.Horizontal, 10);

        Assert.Equal(PipeSectionType.Horizontal, pipe.PipeType);
        Assert.Equal(10, pipe.BasePoints);
        Assert.Equal(0, pipe.SecondaryTraversalBonusPoints);
    }

    [Fact]
    public void Constructor_RejectsSecondaryBonusForNonCross()
    {
        Assert.Throws<ArgumentException>(() =>
            new PlacedPipe(new GridPosition(0, 0), PipeSectionType.Vertical, 10, 5));
    }

    [Fact]
    public void Constructor_AcceptsSecondaryBonusForCross()
    {
        var pipe = new PlacedPipe(new GridPosition(0, 0), PipeSectionType.Cross, 20, 8);
        Assert.Equal(8, pipe.SecondaryTraversalBonusPoints);
    }

    [Theory]
    [InlineData(PipeSectionType.Horizontal, BoardDirection.Left, true)]
    [InlineData(PipeSectionType.Horizontal, BoardDirection.Right, true)]
    [InlineData(PipeSectionType.Horizontal, BoardDirection.Top, false)]
    [InlineData(PipeSectionType.Vertical, BoardDirection.Top, true)]
    [InlineData(PipeSectionType.Vertical, BoardDirection.Left, false)]
    [InlineData(PipeSectionType.Cross, BoardDirection.Left, true)]
    [InlineData(PipeSectionType.Cross, BoardDirection.Top, true)]
    public void CanAcceptFlowFrom_RespectsGeometry(PipeSectionType type, BoardDirection dir, bool expected)
    {
        var pipe = new PlacedPipe(new GridPosition(0, 0), type, 10);
        Assert.Equal(expected, pipe.CanAcceptFlowFrom(dir));
    }

    [Theory]
    [InlineData(PipeSectionType.Horizontal, BoardDirection.Left, BoardDirection.Right)]
    [InlineData(PipeSectionType.Horizontal, BoardDirection.Right, BoardDirection.Left)]
    [InlineData(PipeSectionType.Vertical, BoardDirection.Top, BoardDirection.Bottom)]
    [InlineData(PipeSectionType.Vertical, BoardDirection.Bottom, BoardDirection.Top)]
    [InlineData(PipeSectionType.CornerLeftToTop, BoardDirection.Left, BoardDirection.Top)]
    [InlineData(PipeSectionType.CornerLeftToTop, BoardDirection.Top, BoardDirection.Left)]
    [InlineData(PipeSectionType.CornerRightToBottom, BoardDirection.Right, BoardDirection.Bottom)]
    [InlineData(PipeSectionType.Cross, BoardDirection.Left, BoardDirection.Right)]
    [InlineData(PipeSectionType.Cross, BoardDirection.Top, BoardDirection.Bottom)]
    public void GetExitDirection_ReturnsCorrectDirection(PipeSectionType type, BoardDirection enteredFrom, BoardDirection expectedExit)
    {
        var pipe = new PlacedPipe(new GridPosition(0, 0), type, 10);
        Assert.Equal(expectedExit, pipe.GetExitDirection(enteredFrom));
    }

    [Fact]
    public void GetExitDirection_ThrowsWhenEntryNotOpen()
    {
        var pipe = new PlacedPipe(new GridPosition(0, 0), PipeSectionType.Horizontal, 10);
        Assert.Throws<InvalidOperationException>(() => pipe.GetExitDirection(BoardDirection.Top));
    }
}
