using HexMaster.FloodRush.Game.Core.Diagnostics;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Game.Pages;
using HexMaster.FloodRush.Game.Services;
using HexMaster.FloodRush.Game.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
#if WINDOWS
using Microsoft.UI.Windowing;
using WinRT.Interop;
#endif

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
                fonts.AddFont("Peralta-Regular.ttf", "Peralta");
                fonts.AddFont("PatrickHand-Regular.ttf", "PatrickHand");
            })
            .ConfigureLifecycleEvents(lifecycle =>
            {
#if WINDOWS
                lifecycle.AddWindows(windows =>
                {
                    windows.OnWindowCreated(window =>
                    {
                        var windowHandle = WindowNative.GetWindowHandle(window);
                        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
                        var appWindow = AppWindow.GetFromWindowId(windowId);

                        // Full-screen presenter removes title bar and taskbar
                        appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    });
                });
#endif
            });

        // Services
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<ILocalStateService, LocalStateService>();
        builder.Services.AddSingleton<IApplicationExitService, ApplicationExitService>();
        builder.Services.AddSingleton<INetworkStatusService, NetworkStatusService>();
        builder.Services.AddSingleton<ILevelCacheService, LevelCacheService>();
        builder.Services.AddSingleton<IApiBaseUrlProvider, ApiBaseUrlProvider>();
        builder.Services.AddSingleton<IDeviceAuthenticationService, DeviceAuthenticationService>();
        builder.Services.AddSingleton<ILevelsApiService, LevelsApiService>();
        builder.Services.AddSingleton<IScoresApiService, ScoresApiService>();

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

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(FloodRushTelemetry.ServiceName))
            .WithLogging(
                configureBuilder: _ => { },
                configureOptions: logging =>
                {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                    logging.ParseStateValues = true;
                })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(FloodRushTelemetry.ActivitySourceName)
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(FloodRushTelemetry.MeterName)
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            });

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

        if (TelemetryExportDecisions.ShouldEnableOtlpExport(otlpEndpoint))
        {
            openTelemetry.UseOtlpExporter();
        }

        return builder.Build();
    }
}
