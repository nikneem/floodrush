using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Tiles;

public sealed class FluidBasinTileTests
{
    [Fact]
    public void Constructor_RejectsMatchingEntryAndExitDirections()
    {
        Assert.Throws<InvalidOperationException>(() => new FluidBasinTile(
            new GridPosition(1, 1),
            BoardDirection.Left,
            BoardDirection.Left,
            300,
            25));
    }

    [Fact]
    public void SetEntryDirection_RejectsInvalidStateWithoutMutatingTheTile()
    {
        var tile = new FluidBasinTile(
            new GridPosition(1, 1),
            BoardDirection.Left,
            BoardDirection.Right,
            300,
            25);

        Assert.Throws<InvalidOperationException>(() => tile.SetEntryDirection(BoardDirection.Right));
        Assert.Equal(BoardDirection.Left, tile.EntryDirection);
        Assert.Equal(BoardDirection.Right, tile.ExitDirection);
    }
}
