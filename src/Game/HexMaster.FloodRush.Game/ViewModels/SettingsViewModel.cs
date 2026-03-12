using HexMaster.FloodRush.Game.Services;

namespace HexMaster.FloodRush.Game.ViewModels;

public sealed class SettingsViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;
    private readonly ILocalStateService _localState;
    private bool _soundEnabled;
    private bool _musicEnabled;
    private string _playerName = string.Empty;

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set
        {
            SetField(ref _soundEnabled, value);
            Preferences.Default.Set("settings_sound_enabled", value);
        }
    }

    public bool MusicEnabled
    {
        get => _musicEnabled;
        set
        {
            SetField(ref _musicEnabled, value);
            Preferences.Default.Set("settings_music_enabled", value);
        }
    }

    public string PlayerName
    {
        get => _playerName;
        set => SetField(ref _playerName, value);
    }

    public Command BackCommand { get; }
    public Command SavePlayerNameCommand { get; }
    public Command ResetProgressCommand { get; }

    public SettingsViewModel(INavigationService navigation, ILocalStateService localState)
    {
        _navigation = navigation;
        _localState = localState;

        BackCommand = new Command(async () => await _navigation.GoBackAsync());

        SavePlayerNameCommand = new Command(() =>
        {
            if (!string.IsNullOrWhiteSpace(PlayerName))
                Preferences.Default.Set("settings_player_name", PlayerName.Trim());
        });

        ResetProgressCommand = new Command(() =>
        {
            _localState.ClearProgress();
        });
    }

    public void LoadSettings()
    {
        SoundEnabled = Preferences.Default.Get("settings_sound_enabled", true);
        MusicEnabled = Preferences.Default.Get("settings_music_enabled", true);
        PlayerName = Preferences.Default.Get("settings_player_name", string.Empty);
    }
}
