using System.Diagnostics;
using System.Diagnostics.Metrics;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Game.Services;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.ViewModels;

public sealed class SettingsViewModel : BaseViewModel
{
    private readonly INavigationService navigation;
    private readonly ILocalStateService localState;
    private readonly ILogger<SettingsViewModel> logger;
    private bool isLoadingSettings;
    private bool soundEnabled;
    private bool musicEnabled;
    private string playerName = string.Empty;

    public bool SoundEnabled
    {
        get => soundEnabled;
        set
        {
            if (!SetField(ref soundEnabled, value) || isLoadingSettings)
            {
                return;
            }

            Preferences.Default.Set("settings_sound_enabled", value);
            RecordSettingChange("sound-enabled", value);
        }
    }

    public bool MusicEnabled
    {
        get => musicEnabled;
        set
        {
            if (!SetField(ref musicEnabled, value) || isLoadingSettings)
            {
                return;
            }

            Preferences.Default.Set("settings_music_enabled", value);
            RecordSettingChange("music-enabled", value);
        }
    }

    public string PlayerName
    {
        get => playerName;
        set => SetField(ref playerName, value);
    }

    public Command BackCommand { get; }
    public Command SavePlayerNameCommand { get; }
    public Command ResetProgressCommand { get; }

    public SettingsViewModel(
        INavigationService navigation,
        ILocalStateService localState,
        ILogger<SettingsViewModel> logger)
    {
        this.navigation = navigation;
        this.localState = localState;
        this.logger = logger;

        BackCommand = new Command(async () => await navigation.GoBackAsync());

        SavePlayerNameCommand = new Command(() =>
        {
            var trimmedName = PlayerName.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return;
            }

            using var activity = FloodRushTelemetry.ActivitySource.StartActivity("settings.save-player-name", ActivityKind.Internal);
            Preferences.Default.Set("settings_player_name", trimmedName);
            PlayerName = trimmedName;
            RecordSettingChange("player-name", "updated");
        });

        ResetProgressCommand = new Command(() =>
        {
            using var activity = FloodRushTelemetry.ActivitySource.StartActivity("settings.reset-progress", ActivityKind.Internal);
            localState.ClearProgress();
            FloodRushTelemetry.UserActions.Add(1, new TagList
            {
                { "screen", "settings" },
                { "action", "reset-progress" }
            });
            logger.LogInformation("Reset local gameplay progress from the settings screen.");
        });
    }

    public void LoadSettings()
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("settings.load", ActivityKind.Internal);

        isLoadingSettings = true;
        try
        {
            SoundEnabled = Preferences.Default.Get("settings_sound_enabled", true);
            MusicEnabled = Preferences.Default.Get("settings_music_enabled", true);
            PlayerName = Preferences.Default.Get("settings_player_name", string.Empty);
            logger.LogInformation("Loaded persisted settings into the settings screen.");
        }
        finally
        {
            isLoadingSettings = false;
        }
    }

    private void RecordSettingChange(string settingName, object value)
    {
        FloodRushTelemetry.SettingsChanges.Add(1, new TagList
        {
            { "setting", settingName }
        });

        logger.LogInformation("Updated setting {SettingName} to {Value}.", settingName, value);
    }
}
