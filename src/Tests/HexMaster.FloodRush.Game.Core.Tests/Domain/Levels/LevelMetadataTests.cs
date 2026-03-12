using HexMaster.FloodRush.Game.Core.Domain.Levels;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Levels;

public sealed class LevelMetadataTests
{
    [Fact]
    public void Constructor_SetsDefaultsCorrectly()
    {
        var meta = new LevelMetadata("Flood Rush Level 1");

        Assert.Equal("Flood Rush Level 1", meta.DisplayName);
        Assert.Equal(DifficultyLabel.Medium, meta.Difficulty);
        Assert.Null(meta.ParScore);
        Assert.Null(meta.ReleasedFrom);
        Assert.Null(meta.ReleasedUntil);
        Assert.Empty(meta.TutorialHints);
        Assert.Empty(meta.Tags);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SetDisplayName_RejectsBlankName(string? name)
    {
        var meta = new LevelMetadata("Valid Name");
        Assert.ThrowsAny<ArgumentException>(() => meta.SetDisplayName(name!));
    }

    [Fact]
    public void SetDifficulty_RejectsInvalidEnum()
    {
        var meta = new LevelMetadata("Level");
        Assert.Throws<ArgumentOutOfRangeException>(() => meta.SetDifficulty((DifficultyLabel)99));
    }

    [Fact]
    public void SetDifficulty_UpdatesValue()
    {
        var meta = new LevelMetadata("Level");
        meta.SetDifficulty(DifficultyLabel.Expert);
        Assert.Equal(DifficultyLabel.Expert, meta.Difficulty);
    }

    [Fact]
    public void SetParScore_AcceptsPositiveValue()
    {
        var meta = new LevelMetadata("Level");
        meta.SetParScore(5000);
        Assert.Equal(5000, meta.ParScore);
    }

    [Fact]
    public void SetParScore_RejectsZeroOrNegative()
    {
        var meta = new LevelMetadata("Level");
        Assert.ThrowsAny<ArgumentException>(() => meta.SetParScore(0));
        Assert.ThrowsAny<ArgumentException>(() => meta.SetParScore(-1));
    }

    [Fact]
    public void SetParScore_AcceptsNull()
    {
        var meta = new LevelMetadata("Level", parScore: 999);
        meta.SetParScore(null);
        Assert.Null(meta.ParScore);
    }

    [Fact]
    public void SetReleaseWindow_AcceptsValidWindow()
    {
        var meta = new LevelMetadata("Level");
        var from = DateTimeOffset.UtcNow;
        var until = from.AddDays(30);

        meta.SetReleaseWindow(from, until);

        Assert.Equal(from, meta.ReleasedFrom);
        Assert.Equal(until, meta.ReleasedUntil);
    }

    [Fact]
    public void SetReleaseWindow_RejectsUntilBeforeFrom()
    {
        var meta = new LevelMetadata("Level");
        var from = DateTimeOffset.UtcNow;
        var until = from.AddDays(-1);

        Assert.ThrowsAny<ArgumentException>(() => meta.SetReleaseWindow(from, until));
    }

    [Fact]
    public void SetReleaseWindow_RejectsUntilWithoutFrom()
    {
        var meta = new LevelMetadata("Level");
        Assert.ThrowsAny<ArgumentException>(() =>
            meta.SetReleaseWindow(null, DateTimeOffset.UtcNow.AddDays(10)));
    }

    [Fact]
    public void SetTutorialHints_StoresHints()
    {
        var meta = new LevelMetadata("Level");
        meta.SetTutorialHints(["Hint one", "Hint two"]);
        Assert.Equal(2, meta.TutorialHints.Count);
    }

    [Fact]
    public void SetTags_StoresTags()
    {
        var meta = new LevelMetadata("Level");
        meta.SetTags(["tutorial", "featured"]);
        Assert.Contains("tutorial", meta.Tags);
        Assert.Contains("featured", meta.Tags);
    }

    [Fact]
    public void Clone_ProducesIndependentCopy()
    {
        var original = new LevelMetadata("Original", DifficultyLabel.Hard, 10000);
        var clone = original.Clone();

        Assert.Equal(original.DisplayName, clone.DisplayName);
        Assert.Equal(original.Difficulty, clone.Difficulty);
        Assert.Equal(original.ParScore, clone.ParScore);

        clone.SetDisplayName("Modified");
        Assert.Equal("Original", original.DisplayName);
    }
}
