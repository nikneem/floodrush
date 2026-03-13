using System.ComponentModel;
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
}
