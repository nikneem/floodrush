namespace HexMaster.FloodRush.Game.Components.Welcome;

public partial class MainMenuView : ContentView
{
    public MainMenuView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        _ = AnimateEntranceAsync();
    }

    // Stagger-fade each button in from below on first load
    private async Task AnimateEntranceAsync()
    {
        var buttons = new View[] { PlayButton, LoadButton, SettingsButton };

        foreach (var btn in buttons)
        {
            btn.Opacity = 0;
            btn.TranslationY = 24;
        }

        await Task.Delay(150);

        foreach (var btn in buttons)
        {
            _ = btn.FadeToAsync(1.0, 400, Easing.CubicOut);
            _ = btn.TranslateToAsync(0, 0, 400, Easing.CubicOut);
            await Task.Delay(80);
        }
    }
}
