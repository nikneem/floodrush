using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Pipes;

public sealed class PipeInventoryRuleTests
{
    [Fact]
    public void Constructor_AcceptsValidLimitedRule()
    {
        var rule = new PipeInventoryRule(PipeSectionType.Horizontal, 5);
        Assert.Equal(PipeSectionType.Horizontal, rule.PipeSectionType);
        Assert.Equal(5, rule.MaxCount);
    }

    [Fact]
    public void Constructor_AcceptsUnlimitedRule()
    {
        var rule = new PipeInventoryRule(PipeSectionType.Cross, null);
        Assert.Null(rule.MaxCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetMaxCount_RejectsZeroOrNegative(int count)
    {
        Assert.ThrowsAny<ArgumentException>(() => new PipeInventoryRule(PipeSectionType.Vertical, count));
    }

    [Fact]
    public void SetPipeSectionType_RejectsInvalidEnum()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PipeInventoryRule((PipeSectionType)99, 3));
    }

    [Fact]
    public void Equality_IsByValueAndType()
    {
        var a = new PipeInventoryRule(PipeSectionType.Horizontal, 3);
        var b = new PipeInventoryRule(PipeSectionType.Horizontal, 3);
        var c = new PipeInventoryRule(PipeSectionType.Horizontal, 5);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Clone_ProducesEqualButIndependentInstance()
    {
        var original = new PipeInventoryRule(PipeSectionType.Cross, 2);
        var clone = original.Clone();

        Assert.Equal(original, clone);
        Assert.NotSame(original, clone);
    }
}
