using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Tiles;

public sealed class WallTileTests
{
    [Fact]
    public void Constructor_SetsPosition()
    {
        var pos = new GridPosition(3, 7);
        var wall = new WallTile(pos);
        Assert.Equal(pos, wall.Position);
    }

    [Fact]
    public void FixedTileType_IsWall()
    {
        var wall = new WallTile(new GridPosition(0, 0));
        Assert.Equal(FixedTileType.Wall, wall.FixedTileType);
    }

    [Theory]
    [InlineData(BoardDirection.Left)]
    [InlineData(BoardDirection.Right)]
    [InlineData(BoardDirection.Top)]
    [InlineData(BoardDirection.Bottom)]
    public void CanAcceptFlowFrom_AlwaysReturnsFalse(BoardDirection direction)
    {
        var wall = new WallTile(new GridPosition(1, 1));
        Assert.False(wall.CanAcceptFlowFrom(direction));
    }

    [Fact]
    public void GetOutgoingDirections_ReturnsEmpty()
    {
        var wall = new WallTile(new GridPosition(2, 2));
        Assert.Empty(wall.GetOutgoingDirections());
    }

    [Fact]
    public void Clone_ReturnsNewInstanceAtSamePosition()
    {
        var original = new WallTile(new GridPosition(4, 6));
        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.IsType<WallTile>(clone);
        Assert.Equal(original.Position, clone.Position);
    }

    [Fact]
    public void BonusPoints_IsZero()
    {
        var wall = new WallTile(new GridPosition(0, 0));
        Assert.Equal(0, wall.BonusPoints);
    }
}
