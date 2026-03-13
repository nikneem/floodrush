using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace HexMaster.FloodRush.Game.Controls;

public sealed class PlayfieldTileView : ContentView
{
    public static readonly BindableProperty TileProperty = BindableProperty.Create(
        nameof(Tile),
        typeof(PlayfieldTileItem),
        typeof(PlayfieldTileView),
        default(PlayfieldTileItem),
        propertyChanged: OnTilePropertyChanged);

    public static readonly BindableProperty TileSizeProperty = BindableProperty.Create(
        nameof(TileSize),
        typeof(double),
        typeof(PlayfieldTileView),
        120d,
        propertyChanged: OnTileSizePropertyChanged);

    private readonly Border tileBorder;
    private readonly BoxView baseLayer;
    private readonly Image backgroundImage;
    private readonly Image pipeOverlayImage;
    private readonly BoxView overlay;
    private readonly Label titleLabel;
    private readonly Label subtitleLabel;
    private readonly BoxView pipeFloodFill;
    private readonly BoxView fluidBlob;
    private readonly Label pointsLabel;
    private readonly BoxView illegalFlash;

    public event EventHandler<TileFlowCompletedEventArgs>? FlowCompleted;

    public PlayfieldTileView()
    {
        baseLayer = new BoxView
        {
            Color = GetColor("CardBackgroundElevated")
        };

        backgroundImage = new Image
        {
            Aspect = Aspect.AspectFill,
            Opacity = 0.96d
        };

        pipeOverlayImage = new Image
        {
            Aspect = Aspect.AspectFit,
            IsVisible = false
        };

        overlay = new BoxView();

        titleLabel = new Label
        {
            FontFamily = "Peralta",
            FontSize = 18,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };

        subtitleLabel = new Label
        {
            FontFamily = "PatrickHand",
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var tileContent = new Grid
        {
            RowDefinitions =
            [
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            ]
        };
        Grid.SetRow(subtitleLabel, 1);
        tileContent.Children.Add(titleLabel);
        tileContent.Children.Add(subtitleLabel);

        // Fluid animation blob – centred in the tile, offset via TranslationX/Y
        fluidBlob = new BoxView
        {
            IsVisible = false,
            InputTransparent = true,
            Color = GetColor("FluidWater"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        // Permanent water fill – fades in once the blob has passed through so the
        // pipe looks visibly full rather than empty again after the drop moves on.
        pipeFloodFill = new BoxView
        {
            IsVisible = false,
            Opacity = 0,
            InputTransparent = true,
            Color = GetColor("FluidWater"),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        // "+N pts" pop-up label shown after the blob reaches its destination
        pointsLabel = new Label
        {
            IsVisible = false,
            InputTransparent = true,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };

        // Topmost flash layer – briefly turns red when the player taps a tile that
        // fluid has already traversed (illegal placement).
        illegalFlash = new BoxView
        {
            IsVisible = false,
            Opacity = 0,
            InputTransparent = true,
            Color = GetColor("Danger"),
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        var tileVisual = new Grid();
        tileVisual.Children.Add(baseLayer);
        tileVisual.Children.Add(backgroundImage);
        tileVisual.Children.Add(pipeOverlayImage);
        tileVisual.Children.Add(pipeFloodFill);  // water fill – above pipe PNG
        tileVisual.Children.Add(overlay);
        tileVisual.Children.Add(tileContent);
        tileVisual.Children.Add(fluidBlob);    // on top of content
        tileVisual.Children.Add(pointsLabel);  // "+N pts" popup
        tileVisual.Children.Add(illegalFlash); // topmost – illegal move feedback

        tileBorder = new Border
        {
            Padding = new Thickness(0),
            StrokeThickness = 0,
            Background = new SolidColorBrush(GetColor("CardBackground")),
            StrokeShape = new Rectangle(),
            Content = tileVisual
        };

        Content = tileBorder;
        ApplyTileSize();
    }

    public PlayfieldTileItem? Tile
    {
        get => (PlayfieldTileItem?)GetValue(TileProperty);
        set => SetValue(TileProperty, value);
    }

    public double TileSize
    {
        get => (double)GetValue(TileSizeProperty);
        set => SetValue(TileSizeProperty, value);
    }

    private static void OnTilePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((PlayfieldTileView)bindable).ApplyTile();
    }

    private static void OnTileSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((PlayfieldTileView)bindable).ApplyTileSize();
    }

    private void ApplyTile()
    {
        var tile = Tile;
        if (tile is null)
        {
            return;
        }

        var titleColorKey = tile.Kind == PlayfieldTileKind.Empty
            ? "TextPrimary"
            : tile.Kind == PlayfieldTileKind.FinishPoint
                ? "TextOnPrimary"
                : "TextOnSecondary";

        var subtitleColorKey = tile.Kind == PlayfieldTileKind.Empty
            ? "TextMuted"
            : titleColorKey;

        backgroundImage.Source = ImageSource.FromFile(tile.BackgroundImage);
        titleLabel.Text = tile.Title;
        titleLabel.TextColor = GetColor(titleColorKey);
        subtitleLabel.Text = tile.Subtitle;
        subtitleLabel.TextColor = GetColor(subtitleColorKey);
        tileBorder.Stroke = GetTileStroke(tile.Kind);

        // Reset water fill whenever the tile data changes (level load or pipe swap).
        pipeFloodFill.IsVisible = false;
        pipeFloodFill.Opacity = 0;

        if (!string.IsNullOrEmpty(tile.PipeOverlayImage))
        {
            pipeOverlayImage.Source = ImageSource.FromFile(tile.PipeOverlayImage);
            pipeOverlayImage.Rotation = tile.PipeImageRotation;
            pipeOverlayImage.IsVisible = true;
        }
        else
        {
            pipeOverlayImage.IsVisible = false;
            pipeOverlayImage.Source = null;
        }

        if (tile.Kind == PlayfieldTileKind.Empty)
        {
            baseLayer.Color = GetColor("CardBackgroundElevated");
        }
        else
        {
            baseLayer.Color = GetColor("SurfaceBackground");
        }

        if (TryGetTileOverlay(tile.Kind, out var overlayColor))
        {
            overlay.IsVisible = true;
            overlay.Color = overlayColor;
            overlay.Opacity = 0.56d;
        }
        else
        {
            overlay.IsVisible = false;
        }
    }

    private void ApplyTileSize()
    {
        tileBorder.WidthRequest = TileSize;
        tileBorder.HeightRequest = TileSize;
    }

    private static bool TryGetTileOverlay(PlayfieldTileKind kind, out Color overlayColor)
    {
        switch (kind)
        {
            case PlayfieldTileKind.StartPoint:
                overlayColor = GetColor("Success");
                return true;
            case PlayfieldTileKind.FinishPoint:
                overlayColor = GetColor("BrandAmber");
                return true;
            case PlayfieldTileKind.FluidBasin:
                overlayColor = GetColor("Info");
                return true;
            case PlayfieldTileKind.SplitSection:
                overlayColor = GetColor("Warning");
                return true;
            default:
                overlayColor = Colors.Transparent;
                return false;
        }
    }

    private static Brush GetTileStroke(PlayfieldTileKind kind) =>
        kind switch
        {
            PlayfieldTileKind.StartPoint or
            PlayfieldTileKind.FinishPoint or
            PlayfieldTileKind.FluidBasin or
            PlayfieldTileKind.SplitSection => new SolidColorBrush(GetColor("White")),
            _ => new SolidColorBrush(GetColor("CardBackgroundElevated"))
        };

    private static Color GetColor(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true &&
            value is Color color)
        {
            return color;
        }

        throw new InvalidOperationException($"Resource '{key}' was not found.");
    }

    /// <summary>
    /// Animates fluid flowing from <paramref name="entryDirection"/> through this tile to
    /// <paramref name="exitDirection"/>, then shows a "+points" pop-up and raises
    /// <see cref="FlowCompleted"/>.
    /// </summary>
    public async Task BeginFlowAsync(
        BoardDirection entryDirection,
        BoardDirection exitDirection,
        int points,
        int durationMs,
        bool isTerminal = false)
    {
        var size = TileSize > 0 ? TileSize : 64d;
        var blobSize = Math.Max(8d, size * 0.26);

        fluidBlob.WidthRequest = blobSize;
        fluidBlob.HeightRequest = blobSize;
        fluidBlob.CornerRadius = new CornerRadius(blobSize / 2d);

        var (entryTx, entryTy) = GetEdgeOffset(entryDirection, size);
        var (exitTx, exitTy) = GetEdgeOffset(exitDirection, size);
        var halfDuration = (uint)Math.Max(80, durationMs / 2);

        // Start the blob at the entry edge
        fluidBlob.TranslationX = entryTx;
        fluidBlob.TranslationY = entryTy;
        fluidBlob.Opacity = 0.92d;
        fluidBlob.IsVisible = true;

        // Phase 1: entry edge → tile centre
        await fluidBlob.TranslateTo(0, 0, halfDuration, Easing.CubicIn);

        if (!isTerminal)
        {
            // Phase 2: tile centre → exit edge
            await fluidBlob.TranslateTo(exitTx, exitTy, halfDuration, Easing.CubicOut);
        }

        fluidBlob.IsVisible = false;
        fluidBlob.TranslationX = 0;
        fluidBlob.TranslationY = 0;

        // Reveal the permanent water fill so the pipe looks full after the drop passes.
        pipeFloodFill.IsVisible = true;
        await pipeFloodFill.FadeTo(0.52, (uint)Math.Max(60, durationMs / 3), Easing.CubicOut);

        // Show "+N pts" pop-up when points were earned
        if (points > 0)
        {
            pointsLabel.Text = $"+{points}";
            pointsLabel.FontSize = Math.Max(10d, size * 0.22);
            pointsLabel.Opacity = 0;
            pointsLabel.Scale = 0.5;
            pointsLabel.TranslationY = 0;
            pointsLabel.IsVisible = true;

            await Task.WhenAll(
                pointsLabel.FadeTo(1.0, 200, Easing.CubicOut),
                pointsLabel.ScaleTo(1.1, 200, Easing.CubicOut));

            await Task.Delay(300);

            await Task.WhenAll(
                pointsLabel.TranslateTo(0, -size * 0.45, 350, Easing.CubicIn),
                pointsLabel.FadeTo(0, 350, Easing.CubicIn));

            pointsLabel.IsVisible = false;
            pointsLabel.TranslationY = 0;
        }

        FlowCompleted?.Invoke(this, new TileFlowCompletedEventArgs(
            Tile?.X ?? 0,
            Tile?.Y ?? 0,
            isTerminal ? entryDirection : exitDirection,
            points,
            isTerminal));
    }

    // Returns the translation offset from the tile centre to the centre of each edge.
    private static (double x, double y) GetEdgeOffset(BoardDirection direction, double tileSize)
    {
        var half = tileSize / 2d;
        return direction switch
        {
            BoardDirection.Left => (-half, 0),
            BoardDirection.Right => (half, 0),
            BoardDirection.Top => (0, -half),
            BoardDirection.Bottom => (0, half),
            _ => (0, 0)
        };
    }

    /// <summary>
    /// Plays the 3-second pipe-removal penalty animation on the current pipe overlay:
    /// <list type="bullet">
    ///   <item>Phase 1 (0.6 s) – rapid side-to-side shake to signal something is being removed.</item>
    ///   <item>Phase 2 (2.4 s) – simultaneous spin, shrink, and fade-out (disintegration).</item>
    /// </list>
    /// After the method returns the overlay is hidden and all transforms are reset so the
    /// tile view is ready to display the incoming replacement pipe.
    /// </summary>
    public async Task AnimatePipeRemovalAsync()
    {
        if (!pipeOverlayImage.IsVisible) return;

        // ── Phase 1: Shake (8 × 75 ms = 600 ms) ────────────────────────────────
        const double amplitude = 9d;
        const uint stepMs = 75u;
        for (var i = 0; i < 8; i++)
        {
            var dx = (i % 2 == 0) ? amplitude : -amplitude;
            await pipeOverlayImage.TranslateTo(dx, 0, stepMs, Easing.Linear);
        }
        await pipeOverlayImage.TranslateTo(0, 0, stepMs, Easing.Linear);

        // ── Phase 2: Disintegrate (2 400 ms) ────────────────────────────────────
        await Task.WhenAll(
            pipeOverlayImage.RotateTo(360, 2400, Easing.CubicIn),
            pipeOverlayImage.ScaleTo(0, 2400, Easing.CubicIn),
            pipeOverlayImage.FadeTo(0, 2000, Easing.Linear));

        // Reset so ApplyTile() can immediately show the replacement pipe.
        pipeOverlayImage.IsVisible = false;
        pipeOverlayImage.Rotation = 0;
        pipeOverlayImage.Scale = 1;
        pipeOverlayImage.Opacity = 1;
        pipeOverlayImage.TranslationX = 0;
        pipeOverlayImage.TranslationY = 0;
    }

    /// <summary>
    /// Briefly flashes the tile red to signal that placing a pipe here is illegal
    /// (fluid has already flowed through this tile).
    /// </summary>
    public async Task FlashIllegalMoveAsync()
    {
        illegalFlash.Opacity = 0;
        illegalFlash.IsVisible = true;

        await illegalFlash.FadeTo(0.72, 80, Easing.CubicOut);
        await illegalFlash.FadeTo(0, 220, Easing.CubicIn);

        illegalFlash.IsVisible = false;
    }
}
