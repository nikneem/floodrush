using HexMaster.FloodRush.Game.Core.Domain.Rules;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Rules;

public sealed class FlowSpeedIndicatorTests
{
    [Fact]
    public void Constructor_RejectsValuesBelowRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlowSpeedIndicator(0));
    }

    [Fact]
    public void SetValue_AcceptsValuesInsideRange()
    {
        var indicator = new FlowSpeedIndicator(50);

        indicator.SetValue(100);

        Assert.Equal(100, indicator.Value);
    }

    [Fact]
    public void SetValue_RejectsValuesAboveRange()
    {
        var indicator = new FlowSpeedIndicator(50);

        Assert.Throws<ArgumentOutOfRangeException>(() => indicator.SetValue(101));
        Assert.Equal(50, indicator.Value);
    }
}
