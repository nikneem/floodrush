using System.Diagnostics;
using System.Diagnostics.Metrics;
using HexMaster.FloodRush.Game.Core.Presentation.Welcome;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Game.Services;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.ViewModels;

public sealed class WelcomeViewModel : BaseViewModel
{
    private readonly INavigationService navigation;
    private readonly ILocalStateService localState;
    private readonly IApplicationExitService applicationExit;
    private readonly ILogger<WelcomeViewModel> logger;
    private bool hasProgress;
    private bool isQuitConfirmationVisible;
    private bool doNotShowQuitDialogAgain;

    public bool HasProgress
    {
        get => hasProgress;
        private set
        {
            SetField(ref hasProgress, value);
            OnPropertyChanged(nameof(PlayButtonText));
        }
    }

    public string PlayButtonText => HasProgress ? "Continue" : "Play";

    public bool IsQuitConfirmationVisible
    {
        get => isQuitConfirmationVisible;
        set => SetField(ref isQuitConfirmationVisible, value);
    }

    public bool DoNotShowQuitDialogAgain
    {
        get => doNotShowQuitDialogAgain;
        set => SetField(ref doNotShowQuitDialogAgain, value);
    }

    public Command PlayCommand { get; }
    public Command LoadLevelCommand { get; }
    public Command SettingsCommand { get; }
    public Command QuitCommand { get; }
    public Command ConfirmQuitCommand { get; }
    public Command CancelQuitCommand { get; }

    public WelcomeViewModel(
        INavigationService navigation,
        ILocalStateService localState,
        IApplicationExitService applicationExit,
        ILogger<WelcomeViewModel> logger)
    {
        this.navigation = navigation;
        this.localState = localState;
        this.applicationExit = applicationExit;
        this.logger = logger;

        PlayCommand = new Command(async () =>
        {
            RecordUserAction(HasProgress ? "continue" : "play");

            if (HasProgress && localState.CurrentLevelId is { } levelId)
            {
                logger.LogInformation("Continuing from level {LevelId}.", levelId);
                await navigation.NavigateToGameplayAsync(levelId);
                return;
            }

            logger.LogInformation("Opening level selection from the welcome screen.");
            await navigation.NavigateToLevelSelectionAsync(refreshLevels: true);
        });

        LoadLevelCommand = new Command(async () =>
        {
            RecordUserAction("load-level");
            logger.LogInformation("Opening level selection from the welcome screen.");
            await navigation.NavigateToLevelSelectionAsync(refreshLevels: true);
        });

        SettingsCommand = new Command(async () =>
        {
            RecordUserAction("open-settings");
            logger.LogInformation("Opening the settings screen from the welcome screen.");
            await navigation.NavigateToSettingsAsync();
        });

        QuitCommand = new Command(RequestQuit);
        ConfirmQuitCommand = new Command(ConfirmQuit);
        CancelQuitCommand = new Command(CancelQuit);
    }

    public void RefreshState()
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("welcome.refresh", ActivityKind.Internal);
        HasProgress = localState.HasActiveProgress;
        DoNotShowQuitDialogAgain = false;
        IsQuitConfirmationVisible = false;
        logger.LogInformation("Refreshed welcome screen state. HasProgress={HasProgress}.", HasProgress);
    }

    private void RequestQuit()
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("welcome.request-quit", ActivityKind.Internal);
        RecordUserAction("request-quit");

        if (QuitConfirmationPreferenceDecisions.ShouldShowConfirmation(localState.SkipQuitConfirmation))
        {
            DoNotShowQuitDialogAgain = false;
            IsQuitConfirmationVisible = true;
            logger.LogInformation("Showing quit confirmation dialog.");
            return;
        }

        logger.LogInformation("Quit confirmation is skipped by player preference.");
        applicationExit.Exit();
    }

    private void ConfirmQuit()
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("welcome.confirm-quit", ActivityKind.Internal);
        RecordUserAction("confirm-quit");

        if (QuitConfirmationPreferenceDecisions.ShouldPersistSkipConfirmationPreference(
            quitWasConfirmed: true,
            doNotShowDialogAgainSelected: DoNotShowQuitDialogAgain))
        {
            localState.SetSkipQuitConfirmation(true);
        }

        IsQuitConfirmationVisible = false;
        logger.LogInformation("Quit confirmed from the welcome screen.");
        applicationExit.Exit();
    }

    private void CancelQuit()
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("welcome.cancel-quit", ActivityKind.Internal);
        RecordUserAction("cancel-quit");
        DoNotShowQuitDialogAgain = false;
        IsQuitConfirmationVisible = false;
        logger.LogInformation("Quit confirmation was canceled.");
    }

    private void RecordUserAction(string action)
    {
        FloodRushTelemetry.UserActions.Add(1, new TagList
        {
            { "screen", "welcome" },
            { "action", action }
        });
    }
}
