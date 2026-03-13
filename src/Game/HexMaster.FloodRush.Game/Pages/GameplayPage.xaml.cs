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
        await AnimatePipeStackAsync(cancellationToken);

        if (cancellationToken.IsCancellationRequested || !viewModel.HasLevelLoaded)
        {
            return;
        }

        await Task.Delay(120, cancellationToken);
        viewModel.IsPreStartModalVisible = true;
    }

    private async Task AnimatePipeStackAsync(CancellationToken cancellationToken)
    {
        var pipeStackChildren = PipeStackContainer.Children
            .OfType<VisualElement>()
            .ToArray();

        if (pipeStackChildren.Length == 0)
        {
            return;
        }

        await Task.Delay(50, cancellationToken);

        var launchOffset = -Math.Max(120d, PipeStackContainer.Height + 24d);
        foreach (var child in pipeStackChildren)
        {
            child.AbortAnimation("PipeDrop");
            child.TranslationY = launchOffset;
            child.Opacity = 0d;
            child.Scale = 0.94d;
        }

        var animationTasks = new List<Task>(pipeStackChildren.Length);
        for (var index = 0; index < pipeStackChildren.Length; index++)
        {
            var child = pipeStackChildren[index];
            animationTasks.Add(AnimatePipeStackItemAsync(child, (uint)(index * 70), cancellationToken));
        }

        await Task.WhenAll(animationTasks);
    }

    private static async Task AnimatePipeStackItemAsync(VisualElement child, uint delay, CancellationToken cancellationToken)
    {
        if (delay > 0)
        {
            await Task.Delay((int)delay, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        await Task.WhenAll(
            child.TranslateToAsync(0d, 0d, 550, Easing.BounceOut),
            child.FadeToAsync(1d, 180, Easing.CubicOut),
            child.ScaleToAsync(1d, 260, Easing.CubicOut));
    }
}
