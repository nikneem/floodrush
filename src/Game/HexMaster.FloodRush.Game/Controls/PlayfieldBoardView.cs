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
    private readonly Dictionary<(int X, int Y), PlayfieldTileView> tileViews = new();

    /// <summary>Raised when any tile's flow animation completes.</summary>
    public event EventHandler<TileFlowCompletedEventArgs>? TileFlowCompleted;

    /// <summary>
    /// Raised when the player taps a tile. Payload is the board coordinate (X, Y).
    /// </summary>
    public event EventHandler<(int X, int Y)>? TileTapped;

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

    private void OnTilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Fast path: a single tile was replaced – update only that tile view.
        if (e.Action == NotifyCollectionChangedAction.Replace &&
            e.NewItems?.Count == 1 &&
            e.NewItems[0] is PlayfieldTileItem updatedTile &&
            tileViews.TryGetValue((updatedTile.X, updatedTile.Y), out var replacedView))
        {
            replacedView.Tile = updatedTile;
            return;
        }

        Rebuild();
    }

    private void Rebuild()
    {
        if (BoardWidth <= 0 || BoardHeight <= 0)
        {
            ClearTileViews();
            Content = null;
            return;
        }

        ClearTileViews();

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

                var view = new PlayfieldTileView
                {
                    Tile = tile,
                    TileSize = TileSize
                };
                view.FlowCompleted += OnTileFlowCompleted;

                // Capture position so the closure stays valid after tile updates.
                var capturedX = x;
                var capturedY = y;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => TileTapped?.Invoke(this, (capturedX, capturedY));
                view.GestureRecognizers.Add(tap);

                tileViews[(x, y)] = view;
                grid.Add(view, x, y);
            }
        }

        Content = grid;
    }

    private void ClearTileViews()
    {
        foreach (var view in tileViews.Values)
        {
            view.FlowCompleted -= OnTileFlowCompleted;
        }

        tileViews.Clear();
    }

    private void OnTileFlowCompleted(object? sender, TileFlowCompletedEventArgs e) =>
        TileFlowCompleted?.Invoke(this, e);

    /// <summary>
    /// Cancels any in-flight fluid animation on every tile so the next tile
    /// can start immediately at the new (e.g. fast-forward) speed.
    /// </summary>
    public void CancelCurrentFlowAnimation()
    {
        foreach (var view in tileViews.Values)
            view.CancelCurrentFlow();
    }

    /// <summary>
    /// Triggers a fluid-flow animation on the tile at the given position.
    /// Returns immediately if no tile view exists at that position.
    /// </summary>
    public Task AnimateTileFlowAsync(BeginTileFlowEventArgs args)
    {
        if (tileViews.TryGetValue((args.X, args.Y), out var view))
        {
            return view.BeginFlowAsync(
                args.EntryDirection,
                args.ExitDirection,
                args.Points,
                args.DurationMs,
                args.IsTerminal);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Plays the shake-and-disintegrate removal animation on the tile at
    /// (<paramref name="x"/>, <paramref name="y"/>).  Must be called on the UI thread.
    /// </summary>
    public Task AnimatePipeRemovalAsync(int x, int y)
    {
        if (tileViews.TryGetValue((x, y), out var view))
        {
            return view.AnimatePipeRemovalAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Plays a brief red flash on the tile at (<paramref name="x"/>, <paramref name="y"/>)
    /// to signal that the tap was an illegal move.
    /// </summary>
    public Task FlashIllegalMoveAsync(int x, int y)
    {
        if (tileViews.TryGetValue((x, y), out var view))
        {
            return view.FlashIllegalMoveAsync();
        }

        return Task.CompletedTask;
    }

    private double CalculateBoardAxisLength(int cellCount) =>
        (Math.Max(0, cellCount) * TileSize) +
        (Math.Max(0, cellCount - 1) * TileSpacing) +
        16d;
}
