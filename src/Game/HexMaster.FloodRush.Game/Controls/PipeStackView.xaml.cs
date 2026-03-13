using System.Collections;

namespace HexMaster.FloodRush.Game.Controls;

public partial class PipeStackView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(PipeStackView));

    public PipeStackView()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public async Task AnimateItemsAsync(CancellationToken cancellationToken = default)
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

        // Ensure the bottom (next-to-place) tile is always visible after the cascade.
        await PipeStackScrollView.ScrollToAsync(0, double.MaxValue, animated: false);
    }

    /// <summary>
    /// Slides the topmost stack item in from above the viewport boundary.
    /// Call this after inserting a new pipe at the front of the items source.
    /// </summary>
    public async Task AnimateNewItemAsync(CancellationToken cancellationToken = default)
    {
        // Give the BindableLayout a frame to render the new child.
        await Task.Delay(30, cancellationToken);

        var firstChild = PipeStackContainer.Children
            .OfType<VisualElement>()
            .FirstOrDefault();

        if (firstChild is null) return;

        firstChild.AbortAnimation("PipeDrop");
        var itemHeight = firstChild.Height > 0 ? firstChild.Height : 96d;
        firstChild.TranslationY = -(itemHeight + 14d);
        firstChild.Opacity = 0d;
        firstChild.Scale = 0.94d;

        await AnimatePipeStackItemAsync(firstChild, 0, cancellationToken);

        // Keep the bottom (next-to-place) tile in view after the new item drops in.
        await PipeStackScrollView.ScrollToAsync(0, double.MaxValue, animated: false);
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
