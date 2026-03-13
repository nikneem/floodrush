using HexMaster.FloodRush.Game.ViewModels;

namespace HexMaster.FloodRush.Game.Pages;

public partial class LevelSelectionPage : ContentPage
{
    private readonly LevelSelectionViewModel viewModel;
    private CancellationTokenSource? loadLevelsCancellationTokenSource;

    public LevelSelectionPage(LevelSelectionViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        loadLevelsCancellationTokenSource?.Cancel();
        loadLevelsCancellationTokenSource?.Dispose();
        loadLevelsCancellationTokenSource = new CancellationTokenSource();
        _ = viewModel.LoadLevelsAsync(loadLevelsCancellationTokenSource.Token);
    }

    protected override void OnDisappearing()
    {
        loadLevelsCancellationTokenSource?.Cancel();
        loadLevelsCancellationTokenSource?.Dispose();
        loadLevelsCancellationTokenSource = null;
        base.OnDisappearing();
    }
}
