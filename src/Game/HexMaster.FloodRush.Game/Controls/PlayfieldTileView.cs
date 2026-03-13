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
    private readonly Image backgroundImage;
    private readonly BoxView overlay;
    private readonly Label titleLabel;
    private readonly Label subtitleLabel;

    public PlayfieldTileView()
    {
        backgroundImage = new Image
        {
            Aspect = Aspect.AspectFill
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

        var tileVisual = new Grid();
        tileVisual.Children.Add(backgroundImage);
        tileVisual.Children.Add(overlay);
        tileVisual.Children.Add(tileContent);

        tileBorder = new Border
        {
            Padding = new Thickness(8),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(12)
            },
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

        backgroundImage.Source = tile.BackgroundImage;
        titleLabel.Text = tile.Title;
        titleLabel.TextColor = GetColor(titleColorKey);
        subtitleLabel.Text = tile.Subtitle;
        subtitleLabel.TextColor = GetColor(subtitleColorKey);
        tileBorder.Stroke = GetTileStroke(tile.Kind);

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
            _ => new SolidColorBrush(GetColor("TextMuted"))
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
}
