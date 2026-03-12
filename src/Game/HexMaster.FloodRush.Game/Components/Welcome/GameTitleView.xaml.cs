namespace HexMaster.FloodRush.Game.Components.Welcome;

public partial class GameTitleView : ContentView
{
    public GameTitleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _ = AnimateGlowAsync();
        _ = AnimateRuleAsync();
    }

    // Outer glow breathes softly — opacity pulse
    private async Task AnimateGlowAsync()
    {
        while (true)
        {
            var outerIn  = GlowOuter.FadeToAsync(0.55, 2200, Easing.SinInOut);
            var midIn    = GlowMid.FadeToAsync(0.70, 2200, Easing.SinInOut);
            await Task.WhenAll(outerIn, midIn);

            var outerOut = GlowOuter.FadeToAsync(0.20, 2200, Easing.SinInOut);
            var midOut   = GlowMid.FadeToAsync(0.35, 2200, Easing.SinInOut);
            await Task.WhenAll(outerOut, midOut);
        }
    }

    // Accent rule fades in and slides in from the left on load
    private async Task AnimateRuleAsync()
    {
        AccentRule.Opacity = 0;
        AccentRule.TranslationX = -40;
        await Task.Delay(300);
        var fadeTask  = AccentRule.FadeToAsync(1.0, 700, Easing.CubicOut);
        var slideTask = AccentRule.TranslateToAsync(0, 0, 700, Easing.CubicOut);
        await Task.WhenAll(fadeTask, slideTask);
    }
}
