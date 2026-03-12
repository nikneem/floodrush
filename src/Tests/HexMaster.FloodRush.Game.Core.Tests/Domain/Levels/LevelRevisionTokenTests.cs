using HexMaster.FloodRush.Game.Core.Domain.Levels;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Levels;

public sealed class LevelRevisionTokenTests
{
    [Fact]
    public void Constructor_AcceptsValidToken()
    {
        var token = new LevelRevisionToken("abc123");
        Assert.Equal("abc123", token.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankValue(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new LevelRevisionToken(value!));
    }

    [Fact]
    public void New_CreatesUniqueTokens()
    {
        var t1 = LevelRevisionToken.New();
        var t2 = LevelRevisionToken.New();

        Assert.NotEqual(t1, t2);
        Assert.False(string.IsNullOrWhiteSpace(t1.Value));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var a = new LevelRevisionToken("rev-1");
        var b = new LevelRevisionToken("rev-1");
        var c = new LevelRevisionToken("rev-2");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var token = new LevelRevisionToken("my-rev");
        Assert.Equal("my-rev", token.ToString());
    }
}
