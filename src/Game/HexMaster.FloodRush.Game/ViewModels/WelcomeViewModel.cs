using HexMaster.FloodRush.Game.Services;

namespace HexMaster.FloodRush.Game.ViewModels;

public sealed class WelcomeViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;
    private readonly ILocalStateService _localState;
    private bool _hasProgress;

    public bool HasProgress
    {
        get => _hasProgress;
        private set
        {
            SetField(ref _hasProgress, value);
            OnPropertyChanged(nameof(PlayButtonText));
        }
    }

    public string PlayButtonText => HasProgress ? "Continue" : "Play";

    public Command PlayCommand { get; }
    public Command LoadLevelCommand { get; }
    public Command SettingsCommand { get; }

    public WelcomeViewModel(INavigationService navigation, ILocalStateService localState)
    {
        _navigation = navigation;
        _localState = localState;

        PlayCommand = new Command(async () =>
        {
            if (HasProgress && _localState.CurrentLevelId is { } levelId)
                await _navigation.NavigateToGameplayAsync(levelId);
            else
                await _navigation.NavigateToLevelSelectionAsync();
        });

        LoadLevelCommand = new Command(async () =>
            await _navigation.NavigateToLevelSelectionAsync());

        SettingsCommand = new Command(async () =>
            await _navigation.NavigateToSettingsAsync());
    }

    public void RefreshState()
    {
        HasProgress = _localState.HasActiveProgress;
    }
}
