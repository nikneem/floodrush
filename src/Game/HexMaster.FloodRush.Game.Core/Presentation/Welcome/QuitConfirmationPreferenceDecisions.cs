namespace HexMaster.FloodRush.Game.Core.Presentation.Welcome;

public static class QuitConfirmationPreferenceDecisions
{
    public static bool ShouldShowConfirmation(bool skipQuitConfirmationPreference) =>
        !skipQuitConfirmationPreference;

    public static bool ShouldPersistSkipConfirmationPreference(
        bool quitWasConfirmed,
        bool doNotShowDialogAgainSelected) =>
        quitWasConfirmed && doNotShowDialogAgainSelected;
}
