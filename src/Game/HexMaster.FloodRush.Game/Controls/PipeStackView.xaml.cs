using System.Collections;
using Microsoft.Maui.Controls.Shapes;

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
        PipeStackViewport.SizeChanged += OnPipeStackViewportSizeChanged;
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
    }

    private void OnPipeStackViewportSizeChanged(object? sender, EventArgs e)
    {
        if (PipeStackViewport.Width <= 0d || PipeStackViewport.Height <= 0d)
        {
            return;
        }

        PipeStackViewport.Clip = new RectangleGeometry
        {
            Rect = new Rect(0d, 0d, PipeStackViewport.Width, PipeStackViewport.Height)
        };
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
