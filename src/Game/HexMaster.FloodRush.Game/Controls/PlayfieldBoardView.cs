using System.Collections.Specialized;
using HexMaster.FloodRush.Game.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace HexMaster.FloodRush.Game.Controls;

public sealed class PlayfieldBoardView : ContentView
{
    public static readonly BindableProperty BoardWidthProperty = BindableProperty.Create(
        nameof(BoardWidth),
        typeof(int),
        typeof(PlayfieldBoardView),
        0,
        propertyChanged: OnBoardPropertyChanged);

    public static readonly BindableProperty BoardHeightProperty = BindableProperty.Create(
        nameof(BoardHeight),
        typeof(int),
        typeof(PlayfieldBoardView),
        0,
        propertyChanged: OnBoardPropertyChanged);

    public static readonly BindableProperty TilesProperty = BindableProperty.Create(
        nameof(Tiles),
        typeof(IEnumerable<PlayfieldTileItem>),
        typeof(PlayfieldBoardView),
        default(IEnumerable<PlayfieldTileItem>),
        propertyChanged: OnTilesChanged);

    public static readonly BindableProperty TileSizeProperty = BindableProperty.Create(
        nameof(TileSize),
        typeof(double),
        typeof(PlayfieldBoardView),
        120d,
        propertyChanged: OnBoardPropertyChanged);

    public static readonly BindableProperty TileSpacingProperty = BindableProperty.Create(
        nameof(TileSpacing),
        typeof(double),
        typeof(PlayfieldBoardView),
        8d,
        propertyChanged: OnBoardPropertyChanged);

    private INotifyCollectionChanged? observableTiles;

    public int BoardWidth
    {
        get => (int)GetValue(BoardWidthProperty);
        set => SetValue(BoardWidthProperty, value);
    }

    public int BoardHeight
    {
        get => (int)GetValue(BoardHeightProperty);
        set => SetValue(BoardHeightProperty, value);
    }

    public IEnumerable<PlayfieldTileItem>? Tiles
    {
        get => (IEnumerable<PlayfieldTileItem>?)GetValue(TilesProperty);
        set => SetValue(TilesProperty, value);
    }

    public double TileSize
    {
        get => (double)GetValue(TileSizeProperty);
        set => SetValue(TileSizeProperty, value);
    }

    public double TileSpacing
    {
        get => (double)GetValue(TileSpacingProperty);
        set => SetValue(TileSpacingProperty, value);
    }

    private static void OnBoardPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((PlayfieldBoardView)bindable).Rebuild();
    }

    private static void OnTilesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (PlayfieldBoardView)bindable;
        control.DetachTileCollectionNotifications(oldValue as INotifyCollectionChanged);
        control.AttachTileCollectionNotifications(newValue as INotifyCollectionChanged);
        control.Rebuild();
    }

    private void AttachTileCollectionNotifications(INotifyCollectionChanged? collection)
    {
        observableTiles = collection;
        if (observableTiles is not null)
        {
            observableTiles.CollectionChanged += OnTilesCollectionChanged;
        }
    }

    private void DetachTileCollectionNotifications(INotifyCollectionChanged? collection)
    {
        if (collection is not null)
        {
            collection.CollectionChanged -= OnTilesCollectionChanged;
        }

        if (ReferenceEquals(observableTiles, collection))
        {
            observableTiles = null;
        }
    }

    private void OnTilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        if (BoardWidth <= 0 || BoardHeight <= 0)
        {
            Content = null;
            return;
        }

        var tileLookup = (Tiles ?? [])
            .ToDictionary(tile => (tile.X, tile.Y));

        var grid = new Grid
        {
            RowSpacing = TileSpacing,
            ColumnSpacing = TileSpacing,
            Padding = new Thickness(24),
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start
        };

        for (var column = 0; column < BoardWidth; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        for (var row = 0; row < BoardHeight; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        for (var y = 0; y < BoardHeight; y++)
        {
            for (var x = 0; x < BoardWidth; x++)
            {
                var tile = tileLookup.TryGetValue((x, y), out var mappedTile)
                    ? mappedTile
                    : new PlayfieldTileItem(x, y, PlayfieldTileKind.Empty, string.Empty, string.Empty);

                grid.Add(CreateTile(tile), x, y);
            }
        }

        Content = grid;
    }

    private View CreateTile(PlayfieldTileItem tile)
    {
        var titleColorKey = tile.Kind == PlayfieldTileKind.Empty
            ? "TextPrimary"
            : tile.Kind == PlayfieldTileKind.FinishPoint
                ? "TextOnPrimary"
                : "TextOnSecondary";

        var subtitleColorKey = tile.Kind == PlayfieldTileKind.Empty
            ? "TextMuted"
            : titleColorKey;

        var tileContent = new Grid
        {
            RowDefinitions =
            [
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star }
            ]
        };

        var titleLabel = new Label
        {
            Text = tile.Title,
            FontFamily = "Peralta",
            FontSize = 18,
            TextColor = GetColor(titleColorKey),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var subtitleLabel = new Label
        {
            Text = tile.Subtitle,
            FontFamily = "PatrickHand",
            FontSize = 14,
            TextColor = GetColor(subtitleColorKey),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };

        Grid.SetRow(subtitleLabel, 1);
        tileContent.Children.Add(titleLabel);
        tileContent.Children.Add(subtitleLabel);

        return new Border
        {
            WidthRequest = TileSize,
            HeightRequest = TileSize,
            Padding = new Thickness(8),
            Background = GetTileBackground(tile.Kind),
            Stroke = GetTileStroke(tile.Kind),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(12)
            },
            Content = tileContent
        };
    }

    private static Brush GetTileBackground(PlayfieldTileKind kind) =>
        kind switch
        {
            PlayfieldTileKind.StartPoint => new SolidColorBrush(GetColor("Success")),
            PlayfieldTileKind.FinishPoint => new SolidColorBrush(GetColor("BrandAmber")),
            PlayfieldTileKind.FluidBasin => new SolidColorBrush(GetColor("Info")),
            PlayfieldTileKind.SplitSection => new SolidColorBrush(GetColor("Warning")),
            _ => GetBrush("CardBackgroundBrush")
        };

    private static Brush GetTileStroke(PlayfieldTileKind kind) =>
        kind switch
        {
            PlayfieldTileKind.StartPoint or
            PlayfieldTileKind.FinishPoint or
            PlayfieldTileKind.FluidBasin or
            PlayfieldTileKind.SplitSection => new SolidColorBrush(GetColor("White")),
            _ => new SolidColorBrush(GetColor("TextMuted"))
        };

    private static Brush GetBrush(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var value) == true &&
            value is Brush brush)
        {
            return brush;
        }

        throw new InvalidOperationException($"Resource '{key}' was not found.");
    }

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
