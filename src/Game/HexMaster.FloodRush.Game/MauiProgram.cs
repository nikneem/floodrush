using HexMaster.FloodRush.Game.Pages;
using HexMaster.FloodRush.Game.Services;
using HexMaster.FloodRush.Game.ViewModels;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ILocalStateService, LocalStateService>();

        // ViewModels
        builder.Services.AddTransient<WelcomeViewModel>();
        builder.Services.AddTransient<LevelSelectionViewModel>();
        builder.Services.AddTransient<GameplayViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<LevelSelectionPage>();
        builder.Services.AddTransient<GameplayPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
