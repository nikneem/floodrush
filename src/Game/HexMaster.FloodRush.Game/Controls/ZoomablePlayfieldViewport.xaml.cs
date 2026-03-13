using HexMaster.FloodRush.Game.Core.Presentation.Viewports;
#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
#endif

namespace HexMaster.FloodRush.Game.Controls;

public partial class ZoomablePlayfieldViewport : ContentView
{
    public static readonly BindableProperty PlayfieldContentProperty = BindableProperty.Create(
        nameof(PlayfieldContent),
        typeof(View),
        typeof(ZoomablePlayfieldViewport));

    public static readonly BindableProperty ContentWidthProperty = BindableProperty.Create(
        nameof(ContentWidth),
        typeof(double),
        typeof(ZoomablePlayfieldViewport),
        0d,
        propertyChanged: OnViewportPropertyChanged);

    public static readonly BindableProperty ContentHeightProperty = BindableProperty.Create(
        nameof(ContentHeight),
        typeof(double),
        typeof(ZoomablePlayfieldViewport),
        0d,
        propertyChanged: OnViewportPropertyChanged);

    public static readonly BindableProperty MinZoomProperty = BindableProperty.Create(
        nameof(MinZoom),
        typeof(double),
        typeof(ZoomablePlayfieldViewport),
        PlayfieldViewportMath.DefaultMinZoom,
        propertyChanged: OnViewportPropertyChanged);

    public static readonly BindableProperty MaxZoomProperty = BindableProperty.Create(
        nameof(MaxZoom),
        typeof(double),
        typeof(ZoomablePlayfieldViewport),
        PlayfieldViewportMath.DefaultMaxZoom,
        propertyChanged: OnViewportPropertyChanged);

    private double _pinchStartZoom = PlayfieldViewportMath.DefaultMinZoom;
    private double _currentZoom = PlayfieldViewportMath.DefaultMinZoom;
#if WINDOWS
    private FrameworkElement? _windowsPlatformView;
#endif

    public ZoomablePlayfieldViewport()
    {
        InitializeComponent();
        HandlerChanged += OnViewportHandlerChanged;
        HandlerChanging += OnViewportHandlerChanging;
        UpdateZoomBadge();
    }

    public View? PlayfieldContent
    {
        get => (View?)GetValue(PlayfieldContentProperty);
        set => SetValue(PlayfieldContentProperty, value);
    }

    public double ContentWidth
    {
        get => (double)GetValue(ContentWidthProperty);
        set => SetValue(ContentWidthProperty, value);
    }

    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        set => SetValue(ContentHeightProperty, value);
    }

    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public double CurrentZoom
    {
        get => _currentZoom;
        private set
        {
            if (Math.Abs(_currentZoom - value) < 0.0001d)
            {
                return;
            }

            _currentZoom = value;
            OnPropertyChanged(nameof(CurrentZoom));
            OnPropertyChanged(nameof(ScaledContentWidth));
            OnPropertyChanged(nameof(ScaledContentHeight));
            UpdateZoomBadge();
        }
    }

    public double ScaledContentWidth => PlayfieldViewportMath.ScaleLength(ContentWidth, CurrentZoom);

    public double ScaledContentHeight => PlayfieldViewportMath.ScaleLength(ContentHeight, CurrentZoom);

    private static void OnViewportPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((ZoomablePlayfieldViewport)bindable).RefreshViewport();
    }

    private void RefreshViewport()
    {
        CurrentZoom = PlayfieldViewportMath.ClampZoom(CurrentZoom, MinZoom, MaxZoom);
        OnPropertyChanged(nameof(ScaledContentWidth));
        OnPropertyChanged(nameof(ScaledContentHeight));
        UpdateZoomBadge();
    }

    private void UpdateZoomBadge()
    {
        if (ZoomBadgeLabel is null)
        {
            return;
        }

        ZoomBadgeLabel.Text = $"Zoom {(CurrentZoom * 100d):F0}%";
    }

    private void OnViewportHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        AttachWindowsPointerWheelHandler();
#endif
    }

    private void OnViewportHandlerChanging(object? sender, HandlerChangingEventArgs e)
    {
#if WINDOWS
        DetachWindowsPointerWheelHandler();
#endif
    }

    private async void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (ContentWidth <= 0d || ContentHeight <= 0d)
        {
            return;
        }

        switch (e.Status)
        {
            case GestureStatus.Started:
                _pinchStartZoom = CurrentZoom;
                break;
            case GestureStatus.Running:
            {
                var targetZoom = PlayfieldViewportMath.ClampZoom(_pinchStartZoom * e.Scale, MinZoom, MaxZoom);
                await ApplyZoomAsync(targetZoom, e.ScaleOrigin);
                break;
            }
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _pinchStartZoom = CurrentZoom;
                break;
        }
    }

    private async Task ApplyZoomAsync(double targetZoom, Point scaleOrigin)
    {
        var previousZoom = CurrentZoom;
        if (Math.Abs(previousZoom - targetZoom) < 0.0001d)
        {
            return;
        }

        var scrollPosition = PlayfieldViewportMath.CalculateAnchoredScrollPosition(
            PlayfieldScrollView.ScrollX,
            PlayfieldScrollView.ScrollY,
            PlayfieldScrollView.Width,
            PlayfieldScrollView.Height,
            ContentWidth,
            ContentHeight,
            previousZoom,
            targetZoom,
            scaleOrigin.X,
            scaleOrigin.Y);

        CurrentZoom = targetZoom;

        await Task.Yield();
        await PlayfieldScrollView.ScrollToAsync(scrollPosition.HorizontalOffset, scrollPosition.VerticalOffset, false);
    }

#if WINDOWS
    private void AttachWindowsPointerWheelHandler()
    {
        if (_windowsPlatformView is not null)
        {
            return;
        }

        if (Handler?.PlatformView is FrameworkElement platformView)
        {
            _windowsPlatformView = platformView;
            _windowsPlatformView.PointerWheelChanged += OnWindowsPointerWheelChanged;
        }
    }

    private void DetachWindowsPointerWheelHandler()
    {
        if (_windowsPlatformView is null)
        {
            return;
        }

        _windowsPlatformView.PointerWheelChanged -= OnWindowsPointerWheelChanged;
        _windowsPlatformView = null;
    }

    private async void OnWindowsPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (ContentWidth <= 0d || ContentHeight <= 0d)
        {
            return;
        }

        var delta = e.GetCurrentPoint((UIElement)sender).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        e.Handled = true;

        var maxHorizontalOffset = Math.Max(0d, ScaledContentWidth - PlayfieldScrollView.Width);
        var maxVerticalOffset = Math.Max(0d, ScaledContentHeight - PlayfieldScrollView.Height);
        var targetVerticalOffset = Math.Clamp(PlayfieldScrollView.ScrollY - delta, 0d, maxVerticalOffset);
        var targetHorizontalOffset = Math.Clamp(PlayfieldScrollView.ScrollX, 0d, maxHorizontalOffset);

        await PlayfieldScrollView.ScrollToAsync(targetHorizontalOffset, targetVerticalOffset, false);
    }
#endif
}
