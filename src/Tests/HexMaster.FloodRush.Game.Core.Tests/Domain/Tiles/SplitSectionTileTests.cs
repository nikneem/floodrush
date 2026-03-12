using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Tiles;

public sealed class SplitSectionTileTests
{
    [Fact]
    public void Constructor_RejectsAmbiguousDirections()
    {
        Assert.Throws<InvalidOperationException>(() => new SplitSectionTile(
            new GridPosition(1, 1),
            BoardDirection.Left,
            BoardDirection.Right,
            BoardDirection.Right,
            75,
            10));
    }

    [Fact]
    public void Constructor_StoresValidatedSpeedModifier()
    {
        var tile = new SplitSectionTile(
            new GridPosition(1, 1),
            BoardDirection.Left,
            BoardDirection.Top,
            BoardDirection.Right,
            80,
            12);

        Assert.Equal(80, tile.SpeedModifierPercent);
        Assert.Equal(12, tile.BonusPoints);
    }

    [Fact]
    public void SetSecondaryExitDirection_RejectsInvalidStateWithoutMutatingTheTile()
    {
        var tile = new SplitSectionTile(
            new GridPosition(1, 1),
            BoardDirection.Left,
            BoardDirection.Top,
            BoardDirection.Right,
            80,
            12);

        Assert.Throws<InvalidOperationException>(() => tile.SetSecondaryExitDirection(BoardDirection.Top));
        Assert.Equal(BoardDirection.Right, tile.SecondaryExitDirection);
    }
}
