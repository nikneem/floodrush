using System.Collections.Specialized;
using HexMaster.FloodRush.Game.ViewModels;
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
            Padding = new Thickness(8),
            WidthRequest = CalculateBoardAxisLength(BoardWidth),
            HeightRequest = CalculateBoardAxisLength(BoardHeight),
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
                    : new PlayfieldTileItem(x, y, PlayfieldTileKind.Empty, "empty_tile_background_01.png", string.Empty, string.Empty);

                grid.Add(new PlayfieldTileView
                {
                    Tile = tile,
                    TileSize = TileSize
                }, x, y);
            }
        }

        Content = grid;
    }

    private double CalculateBoardAxisLength(int cellCount) =>
        (Math.Max(0, cellCount) * TileSize) +
        (Math.Max(0, cellCount - 1) * TileSpacing) +
        16d;
}
