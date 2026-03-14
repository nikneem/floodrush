using System.ComponentModel;
using HexMaster.FloodRush.Game.Controls;
using HexMaster.FloodRush.Game.ViewModels;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.Pages;

public partial class GameplayPage : ContentPage
{
    private readonly GameplayViewModel viewModel;
    private readonly ILogger<GameplayPage> logger;
    private CancellationTokenSource? loadLevelCancellationTokenSource;
    private CancellationTokenSource? preStartPresentationCancellationTokenSource;

    public GameplayPage(GameplayViewModel viewModel, ILogger<GameplayPage> logger)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        this.logger = logger;
        this.viewModel.PropertyChanged += OnViewModelPropertyChanged;
        this.viewModel.BeginTileFlow += OnBeginTileFlow;
        this.viewModel.PipeRemovalStarted += OnPipeRemovalStarted;
        this.viewModel.CancelCurrentTileFlow += OnCancelCurrentTileFlow;
        BoardView.TileFlowCompleted += OnTileFlowCompleted;
        BoardView.TileTapped += OnTileTapped;
        BindingContext = viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        // OnNavigatedTo fires after Shell has applied all [QueryProperty] values, so
        // LevelId is guaranteed to be set here. We trigger loading here as the primary
        // trigger and keep OnAppearing as a fallback.
        logger.LogInformation("GameplayPage.OnNavigatedTo. LevelId='{LevelId}'.", viewModel.LevelId);
        TriggerLevelLoad();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        logger.LogInformation(
            "GameplayPage.OnAppearing. LevelId='{LevelId}', HasLevelLoaded={HasLevelLoaded}.",
            viewModel.LevelId, viewModel.HasLevelLoaded);
        TriggerLevelLoad();
    }

    private void TriggerLevelLoad()
    {
        if (viewModel.IsBusy || viewModel.HasLevelLoaded)
        {
            return;
        }

        loadLevelCancellationTokenSource?.Cancel();
        loadLevelCancellationTokenSource?.Dispose();
        loadLevelCancellationTokenSource = new CancellationTokenSource();
        _ = viewModel.LoadLevelAsync(loadLevelCancellationTokenSource.Token);
    }

    protected override void OnDisappearing()
    {
        loadLevelCancellationTokenSource?.Cancel();
        loadLevelCancellationTokenSource?.Dispose();
        loadLevelCancellationTokenSource = null;
        preStartPresentationCancellationTokenSource?.Cancel();
        preStartPresentationCancellationTokenSource?.Dispose();
        preStartPresentationCancellationTokenSource = null;
        base.OnDisappearing();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameplayViewModel.PipeStackAnimationVersion))
        {
            preStartPresentationCancellationTokenSource?.Cancel();
            preStartPresentationCancellationTokenSource?.Dispose();
            preStartPresentationCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = preStartPresentationCancellationTokenSource.Token;
            MainThread.BeginInvokeOnMainThread(async () => await PresentLoadedLevelAsync(cancellationToken));
        }
    }

    private async Task PresentLoadedLevelAsync(CancellationToken cancellationToken)
    {
        while (viewModel.IsBusy && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(50, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await Task.Delay(50, cancellationToken);
        await PipeStackView.AnimateItemsAsync(cancellationToken);

        if (cancellationToken.IsCancellationRequested || !viewModel.HasLevelLoaded)
        {
            return;
        }

        await Task.Delay(120, cancellationToken);
        viewModel.IsPreStartModalVisible = true;
        viewModel.IsRetrying = false;
    }

    // ── Tile tap – pipe placement ────────────────────────────────────────────────

    private async void OnTileTapped(object? sender, (int X, int Y) pos)
    {
        if (viewModel.TryPlacePipe(pos.X, pos.Y, out bool penaltyStarted) && !penaltyStarted)
        {
            // Immediate placement: animate the stack right away.
            await PipeStackView.AnimateNewItemAsync();
        }
        else if (!penaltyStarted && viewModel.IsVisitedTile(pos.X, pos.Y))
        {
            // Fluid already flowed through this tile – flash it red.
            await BoardView.FlashIllegalMoveAsync(pos.X, pos.Y);
        }
        // When penaltyStarted = true the stack animation is triggered from
        // OnPipeRemovalStarted after the 3-second penalty animation completes.
    }

    // ── Pipe removal penalty animation ──────────────────────────────────────────

    /// <summary>
    /// Drives the 3-second shake-and-disintegrate penalty, then commits the
    /// replacement and animates the pipe stack.
    /// </summary>
    private async void OnPipeRemovalStarted(object? sender, PipeRemovalEventArgs e)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await BoardView.AnimatePipeRemovalAsync(e.X, e.Y);
            e.Complete();                           // ViewModel commits the new pipe
            await PipeStackView.AnimateNewItemAsync(); // stack slides in the replacement
        });
    }

    // ── Flow animation bridge ────────────────────────────────────────────────────

    private void OnBeginTileFlow(object? sender, BeginTileFlowEventArgs e)
    {
        // Animations must run on the UI thread. BeginTileFlow may fire from a
        // background thread (countdown task), so we always marshal here.
        MainThread.BeginInvokeOnMainThread(async () => await BoardView.AnimateTileFlowAsync(e));
    }

    private void OnCancelCurrentTileFlow(object? sender, EventArgs e)
    {
        // Cancel the in-progress tile animation so fast-forward takes effect immediately.
        MainThread.BeginInvokeOnMainThread(() => BoardView.CancelCurrentFlowAnimation());
    }

    private void OnTileFlowCompleted(object? sender, TileFlowCompletedEventArgs e)
    {
        // Already on the UI thread (raised by MAUI animation continuations).
        viewModel.OnTileFlowCompleted(e);
    }
}
