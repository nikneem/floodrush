using HexMaster.FloodRush.Game.Core.Domain.Board;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Board;

public sealed class BoardDimensionsTests
{
    [Fact]
    public void SetDimensions_UpdatesWidthAndHeight()
    {
        var dimensions = new BoardDimensions(2, 3);

        dimensions.SetDimensions(4, 5);

        Assert.Equal(4, dimensions.Width);
        Assert.Equal(5, dimensions.Height);
    }

    [Fact]
    public void Contains_ReturnsTrueOnlyForCoordinatesInsideTheBoard()
    {
        var dimensions = new BoardDimensions(3, 2);

        Assert.True(dimensions.Contains(new GridPosition(2, 1)));
        Assert.False(dimensions.Contains(new GridPosition(3, 1)));
    }
}
