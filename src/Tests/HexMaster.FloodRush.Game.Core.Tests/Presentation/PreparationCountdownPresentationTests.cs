using HexMaster.FloodRush.Game.Core.Presentation.Gameplay;

namespace HexMaster.FloodRush.Game.Core.Tests.Presentation;

public sealed class PreparationCountdownPresentationTests
{
    [Theory]
    [InlineData(30, PreparationCountdownUrgency.Normal)]
    [InlineData(20, PreparationCountdownUrgency.Warning)]
    [InlineData(11, PreparationCountdownUrgency.Warning)]
    [InlineData(10, PreparationCountdownUrgency.Critical)]
    [InlineData(0, PreparationCountdownUrgency.Critical)]
    public void ResolveUrgency_ReturnsExpectedUrgency(int remainingSeconds, PreparationCountdownUrgency expected)
    {
        var urgency = PreparationCountdownPresentation.ResolveUrgency(remainingSeconds);

        Assert.Equal(expected, urgency);
    }

    [Theory]
    [InlineData(30, false)]
    [InlineData(11, false)]
    [InlineData(10, true)]
    [InlineData(0, true)]
    public void ShouldBlink_ReturnsExpectedValue(int remainingSeconds, bool expected)
    {
        var shouldBlink = PreparationCountdownPresentation.ShouldBlink(remainingSeconds);

        Assert.Equal(expected, shouldBlink);
    }

    [Fact]
    public void ResolveOpacity_ReturnsReducedOpacity_WhenBlinkPhaseIsHidden()
    {
        var opacity = PreparationCountdownPresentation.ResolveOpacity(5, isBlinkPhaseVisible: false);

        Assert.Equal(PreparationCountdownPresentation.HiddenBlinkOpacity, opacity, 3);
    }
}
