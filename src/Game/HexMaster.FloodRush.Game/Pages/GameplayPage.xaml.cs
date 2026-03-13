using System.ComponentModel;
using HexMaster.FloodRush.Game.Controls;
using HexMaster.FloodRush.Game.ViewModels;

namespace HexMaster.FloodRush.Game.Pages;

public partial class GameplayPage : ContentPage
{
    private readonly GameplayViewModel viewModel;
    private CancellationTokenSource? loadLevelCancellationTokenSource;
    private CancellationTokenSource? preStartPresentationCancellationTokenSource;

    public GameplayPage(GameplayViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        this.viewModel.PropertyChanged += OnViewModelPropertyChanged;
        this.viewModel.BeginTileFlow += OnBeginTileFlow;
        BoardView.TileFlowCompleted += OnTileFlowCompleted;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
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
    }

    // ── Flow animation bridge ────────────────────────────────────────────────────

    private void OnBeginTileFlow(object? sender, BeginTileFlowEventArgs e)
    {
        // Animations must run on the UI thread. BeginTileFlow may fire from a
        // background thread (countdown task), so we always marshal here.
        MainThread.BeginInvokeOnMainThread(async () => await BoardView.AnimateTileFlowAsync(e));
    }

    private void OnTileFlowCompleted(object? sender, TileFlowCompletedEventArgs e)
    {
        // Already on the UI thread (raised by MAUI animation continuations).
        viewModel.OnTileFlowCompleted(e);
    }
}
