using HexMaster.FloodRush.Game.ViewModels;

namespace HexMaster.FloodRush.Game.Pages;

public partial class GameplayPage : ContentPage
{
    private readonly GameplayViewModel viewModel;
    private CancellationTokenSource? loadLevelCancellationTokenSource;

    public GameplayPage(GameplayViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
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
        base.OnDisappearing();
    }
}
