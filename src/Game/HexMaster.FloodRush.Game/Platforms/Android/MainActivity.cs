using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

namespace HexMaster.FloodRush.Game;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.SensorLandscape)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetFullScreen();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
            SetFullScreen();
    }

    private void SetFullScreen()
    {
        if (Window is null) return;

        // Extend content edge-to-edge (behind system bars)
        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        // Hide status bar and navigation bar; swipe to reveal transiently
        var decorView = Window.DecorView;
        if (decorView is null) return;
        var controller = WindowCompat.GetInsetsController(Window, decorView);
        if (controller is null) return;
        controller.Hide(WindowInsetsCompat.Type.SystemBars());
        controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
    }
}
