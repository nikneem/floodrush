using HexMaster.FloodRush.Game.Core.Presentation.Welcome;

namespace HexMaster.FloodRush.Game.Core.Tests.Presentation;

public sealed class QuitConfirmationPreferenceDecisionsTests
{
    [Fact]
    public void ShouldShowConfirmation_ReturnsTrue_WhenPreferenceIsNotSuppressed()
    {
        Assert.True(QuitConfirmationPreferenceDecisions.ShouldShowConfirmation(skipQuitConfirmationPreference: false));
    }

    [Fact]
    public void ShouldShowConfirmation_ReturnsFalse_WhenPreferenceIsSuppressed()
    {
        Assert.False(QuitConfirmationPreferenceDecisions.ShouldShowConfirmation(skipQuitConfirmationPreference: true));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void ShouldPersistSkipConfirmationPreference_OnlyReturnsTrueForConfirmedOptOut(
        bool quitWasConfirmed,
        bool doNotShowDialogAgainSelected,
        bool expected)
    {
        var actual = QuitConfirmationPreferenceDecisions.ShouldPersistSkipConfirmationPreference(
            quitWasConfirmed,
            doNotShowDialogAgainSelected);

        Assert.Equal(expected, actual);
    }
}
