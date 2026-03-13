using HexMaster.FloodRush.Game.Core.Presentation.Viewports;

namespace HexMaster.FloodRush.Game.Core.Tests.Presentation;

public sealed class PlayfieldViewportMathTests
{
    [Fact]
    public void ClampZoom_ReturnsRequestedZoom_WhenWithinBounds()
    {
        var zoom = PlayfieldViewportMath.ClampZoom(1.75d);

        Assert.Equal(1.75d, zoom, 3);
    }

    [Fact]
    public void ClampZoom_ClampsRequestedZoom_WhenOutsideBounds()
    {
        var tooSmall = PlayfieldViewportMath.ClampZoom(0.5d, 1d, 3d);
        var tooLarge = PlayfieldViewportMath.ClampZoom(4d, 1d, 3d);

        Assert.Equal(1d, tooSmall, 3);
        Assert.Equal(3d, tooLarge, 3);
    }

    [Fact]
    public void CalculateAnchoredScrollPosition_PreservesPinchAnchor_WhenZoomingIn()
    {
        var position = PlayfieldViewportMath.CalculateAnchoredScrollPosition(
            currentHorizontalOffset: 100d,
            currentVerticalOffset: 50d,
            viewportWidth: 800d,
            viewportHeight: 450d,
            contentWidth: 1600d,
            contentHeight: 900d,
            currentZoom: 1d,
            targetZoom: 2d,
            horizontalOriginRatio: 0.5d,
            verticalOriginRatio: 0.5d);

        Assert.Equal(600d, position.HorizontalOffset, 3);
        Assert.Equal(325d, position.VerticalOffset, 3);
    }

    [Fact]
    public void CalculateAnchoredScrollPosition_ClampsToVisibleBounds_WhenZoomingOut()
    {
        var position = PlayfieldViewportMath.CalculateAnchoredScrollPosition(
            currentHorizontalOffset: 2400d,
            currentVerticalOffset: 1350d,
            viewportWidth: 800d,
            viewportHeight: 450d,
            contentWidth: 1600d,
            contentHeight: 900d,
            currentZoom: 2d,
            targetZoom: 1d,
            horizontalOriginRatio: 1d,
            verticalOriginRatio: 1d);

        Assert.Equal(800d, position.HorizontalOffset, 3);
        Assert.Equal(450d, position.VerticalOffset, 3);
    }

    [Fact]
    public void CalculateAnchoredScrollPosition_ClampsOutOfRangeOrigins()
    {
        var position = PlayfieldViewportMath.CalculateAnchoredScrollPosition(
            currentHorizontalOffset: 100d,
            currentVerticalOffset: 100d,
            viewportWidth: 500d,
            viewportHeight: 500d,
            contentWidth: 1200d,
            contentHeight: 1200d,
            currentZoom: 1d,
            targetZoom: 1.5d,
            horizontalOriginRatio: 4d,
            verticalOriginRatio: -2d);

        Assert.Equal(400d, position.HorizontalOffset, 3);
        Assert.Equal(150d, position.VerticalOffset, 3);
    }
}
