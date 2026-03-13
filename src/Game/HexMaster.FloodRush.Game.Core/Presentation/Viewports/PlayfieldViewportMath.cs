namespace HexMaster.FloodRush.Game.Core.Presentation.Viewports;

public static class PlayfieldViewportMath
{
    public const double DefaultMinZoom = 1d;
    public const double DefaultMaxZoom = 3d;
    public const double DefaultMaxTileRenderSize = 128d;

    public static double ClampZoom(double requestedZoom, double minZoom = DefaultMinZoom, double maxZoom = DefaultMaxZoom)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minZoom);

        if (maxZoom < minZoom)
        {
            throw new ArgumentOutOfRangeException(nameof(maxZoom), "The maximum zoom must be greater than or equal to the minimum zoom.");
        }

        return Math.Clamp(requestedZoom, minZoom, maxZoom);
    }

    public static double ScaleLength(double baseLength, double zoom)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseLength);

        if (zoom <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(zoom), "Zoom must be greater than zero.");
        }

        return baseLength * zoom;
    }

    public static double CalculateMaxZoomForTileSize(
        double baseTileRenderSize,
        double maxTileRenderSize = DefaultMaxTileRenderSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseTileRenderSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxTileRenderSize);

        return Math.Max(DefaultMinZoom, maxTileRenderSize / baseTileRenderSize);
    }

    public static PlayfieldViewportScrollPosition CalculateAnchoredScrollPosition(
        double currentHorizontalOffset,
        double currentVerticalOffset,
        double viewportWidth,
        double viewportHeight,
        double contentWidth,
        double contentHeight,
        double currentZoom,
        double targetZoom,
        double horizontalOriginRatio,
        double verticalOriginRatio)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(currentHorizontalOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(currentVerticalOffset);
        ArgumentOutOfRangeException.ThrowIfNegative(viewportWidth);
        ArgumentOutOfRangeException.ThrowIfNegative(viewportHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(contentWidth);
        ArgumentOutOfRangeException.ThrowIfNegative(contentHeight);

        if (currentZoom <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(currentZoom), "Current zoom must be greater than zero.");
        }

        if (targetZoom <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(targetZoom), "Target zoom must be greater than zero.");
        }

        var clampedHorizontalOrigin = Math.Clamp(horizontalOriginRatio, 0d, 1d);
        var clampedVerticalOrigin = Math.Clamp(verticalOriginRatio, 0d, 1d);
        var zoomRatio = targetZoom / currentZoom;

        var anchoredHorizontalPoint = currentHorizontalOffset + (clampedHorizontalOrigin * viewportWidth);
        var anchoredVerticalPoint = currentVerticalOffset + (clampedVerticalOrigin * viewportHeight);

        var targetHorizontalOffset = (anchoredHorizontalPoint * zoomRatio) - (clampedHorizontalOrigin * viewportWidth);
        var targetVerticalOffset = (anchoredVerticalPoint * zoomRatio) - (clampedVerticalOrigin * viewportHeight);

        var maxHorizontalOffset = Math.Max(0d, ScaleLength(contentWidth, targetZoom) - viewportWidth);
        var maxVerticalOffset = Math.Max(0d, ScaleLength(contentHeight, targetZoom) - viewportHeight);

        return new PlayfieldViewportScrollPosition(
            Math.Clamp(targetHorizontalOffset, 0d, maxHorizontalOffset),
            Math.Clamp(targetVerticalOffset, 0d, maxVerticalOffset));
    }
}
