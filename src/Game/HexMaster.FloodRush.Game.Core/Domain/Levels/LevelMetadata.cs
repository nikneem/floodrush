using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Levels;

/// <summary>
/// Optional presentational and release metadata for a level revision.
/// All fields are optional except DisplayName.
/// </summary>
public sealed class LevelMetadata
{
    private readonly List<string> tutorialHints = [];
    private readonly List<string> tags = [];

    public LevelMetadata(
        string displayName,
        DifficultyLabel difficulty = DifficultyLabel.Medium,
        int? parScore = null,
        DateTimeOffset? releasedFrom = null,
        DateTimeOffset? releasedUntil = null,
        IEnumerable<string>? tutorialHints = null,
        IEnumerable<string>? tags = null)
    {
        SetDisplayName(displayName);
        SetDifficulty(difficulty);
        SetParScore(parScore);
        SetReleaseWindow(releasedFrom, releasedUntil);
        SetTutorialHints(tutorialHints ?? []);
        SetTags(tags ?? []);
    }

    public string DisplayName { get; private set; } = string.Empty;

    public DifficultyLabel Difficulty { get; private set; }

    /// <summary>Target score that represents a "par" completion. Null means no par is defined.</summary>
    public int? ParScore { get; private set; }

    /// <summary>Earliest date/time from which this level is available to players. Null means always available.</summary>
    public DateTimeOffset? ReleasedFrom { get; private set; }

    /// <summary>Latest date/time until which this level is available. Null means no expiry.</summary>
    public DateTimeOffset? ReleasedUntil { get; private set; }

    /// <summary>In-game hint strings shown during the tutorial overlay.</summary>
    public IReadOnlyCollection<string> TutorialHints => tutorialHints.AsReadOnly();

    /// <summary>Free-form tags used for filtering and discovery (e.g. "tutorial", "featured").</summary>
    public IReadOnlyCollection<string> Tags => tags.AsReadOnly();

    public void SetDisplayName(string displayName) =>
        DisplayName = Guard.AgainstNullOrWhiteSpace(displayName, nameof(displayName));

    public void SetDifficulty(DifficultyLabel difficulty)
    {
        if (!Enum.IsDefined(difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty), "Unknown difficulty label.");
        }

        Difficulty = difficulty;
    }

    public void SetParScore(int? parScore)
    {
        if (parScore.HasValue)
        {
            Guard.AgainstOutOfRange(parScore.Value, 1, int.MaxValue, nameof(parScore));
        }

        ParScore = parScore;
    }

    public void SetReleaseWindow(DateTimeOffset? releasedFrom, DateTimeOffset? releasedUntil)
    {
        if (releasedFrom.HasValue && releasedUntil.HasValue && releasedUntil <= releasedFrom)
        {
            throw new ArgumentException("ReleasedUntil must be after ReleasedFrom.");
        }

        if (releasedUntil.HasValue && !releasedFrom.HasValue)
        {
            throw new ArgumentException("ReleasedFrom must be set when ReleasedUntil is specified.");
        }

        ReleasedFrom = releasedFrom;
        ReleasedUntil = releasedUntil;
    }

    public void SetTutorialHints(IEnumerable<string> hints)
    {
        ArgumentNullException.ThrowIfNull(hints);
        var validated = hints.Select(h => Guard.AgainstNullOrWhiteSpace(h, "hint")).ToList();
        tutorialHints.Clear();
        tutorialHints.AddRange(validated);
    }

    public void SetTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        var validated = tags.Select(t => Guard.AgainstNullOrWhiteSpace(t, "tag")).ToList();
        this.tags.Clear();
        this.tags.AddRange(validated);
    }

    public LevelMetadata Clone() => new(
        DisplayName,
        Difficulty,
        ParScore,
        ReleasedFrom,
        ReleasedUntil,
        [.. tutorialHints],
        [.. tags]);
}
