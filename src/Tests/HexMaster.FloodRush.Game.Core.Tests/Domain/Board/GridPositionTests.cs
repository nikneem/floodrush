using HexMaster.FloodRush.Game.Core.Domain.Board;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Board;

public sealed class GridPositionTests
{
    [Fact]
    public void Constructor_RejectsNegativeCoordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GridPosition(-1, 0));
    }

    [Fact]
    public void SetCoordinates_UpdatesPositionWhenValuesAreValid()
    {
        var position = new GridPosition(1, 2);

        position.SetCoordinates(3, 4);

        Assert.Equal(3, position.X);
        Assert.Equal(4, position.Y);
    }
}
