using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Pipes;

public sealed class PipeScoringOverrideTests
{
    [Fact]
    public void Constructor_AcceptsValidNonCrossOverride()
    {
        var o = new PipeScoringOverride(PipeSectionType.Horizontal, 15);
        Assert.Equal(PipeSectionType.Horizontal, o.PipeSectionType);
        Assert.Equal(15, o.BasePoints);
        Assert.Equal(0, o.SecondaryTraversalBonusPoints);
    }

    [Fact]
    public void Constructor_AcceptsValidCrossOverrideWithBonus()
    {
        var o = new PipeScoringOverride(PipeSectionType.Cross, 25, 10);
        Assert.Equal(25, o.BasePoints);
        Assert.Equal(10, o.SecondaryTraversalBonusPoints);
    }

    [Fact]
    public void Constructor_RejectsSecondaryBonusForNonCross()
    {
        Assert.Throws<ArgumentException>(() =>
            new PipeScoringOverride(PipeSectionType.Vertical, 10, 5));
    }

    [Fact]
    public void Constructor_RejectsNegativeBasePoints()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new PipeScoringOverride(PipeSectionType.Horizontal, -1));
    }

    [Fact]
    public void Constructor_RejectsInvalidPipeType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PipeScoringOverride((PipeSectionType)99, 10));
    }

    [Fact]
    public void Clone_ProducesEqualInstance()
    {
        var original = new PipeScoringOverride(PipeSectionType.Cross, 20, 8);
        var clone = original.Clone();

        Assert.Equal(original, clone);
        Assert.NotSame(original, clone);
    }

    [Fact]
    public void Equality_IsByAllFields()
    {
        var a = new PipeScoringOverride(PipeSectionType.Horizontal, 15);
        var b = new PipeScoringOverride(PipeSectionType.Horizontal, 15);
        var c = new PipeScoringOverride(PipeSectionType.Horizontal, 20);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
