using HexMaster.FloodRush.Game.Services;

namespace HexMaster.FloodRush.Game.ViewModels;

[QueryProperty(nameof(LevelId), "levelId")]
public sealed class GameplayViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;
    private string _levelId = string.Empty;
    private bool _isPaused;
    private bool _isGameOver;
    private bool _isSuccess;
    private int _score;
    private int _remainingPrepSeconds;

    public string LevelId
    {
        get => _levelId;
        set => SetField(ref _levelId, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        set => SetField(ref _isPaused, value);
    }

    public bool IsGameOver
    {
        get => _isGameOver;
        set => SetField(ref _isGameOver, value);
    }

    public bool IsSuccess
    {
        get => _isSuccess;
        set => SetField(ref _isSuccess, value);
    }

    public int Score
    {
        get => _score;
        set => SetField(ref _score, value);
    }

    public int RemainingPrepSeconds
    {
        get => _remainingPrepSeconds;
        set => SetField(ref _remainingPrepSeconds, value);
    }

    public Command PauseCommand { get; }
    public Command ResumeCommand { get; }
    public Command QuitCommand { get; }
    public Command RetryCommand { get; }

    public GameplayViewModel(INavigationService navigation)
    {
        _navigation = navigation;

        PauseCommand = new Command(() => IsPaused = true);
        ResumeCommand = new Command(() => IsPaused = false);
        QuitCommand = new Command(async () =>
        {
            IsPaused = false;
            await _navigation.GoBackAsync();
        });
        RetryCommand = new Command(() =>
        {
            IsGameOver = false;
            IsSuccess = false;
            Score = 0;
        });
    }
}
