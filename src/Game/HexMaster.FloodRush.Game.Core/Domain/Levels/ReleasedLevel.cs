using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Levels;

/// <summary>
/// Tracks a server-released level and the device's local cache state.
/// The server is the authority for which levels are available and at which revision.
/// </summary>
public sealed class ReleasedLevel
{
    private LevelRevisionToken latestRevisionToken;
    private LevelRevision? cachedRevision;

    public ReleasedLevel(string levelId, LevelRevisionToken latestRevisionToken)
    {
        LevelId = Guard.AgainstNullOrWhiteSpace(levelId, nameof(levelId));
        this.latestRevisionToken = Guard.AgainstNull(latestRevisionToken, nameof(latestRevisionToken));
    }

    public string LevelId { get; }

    /// <summary>The revision token the server has most recently released for this level.</summary>
    public LevelRevisionToken LatestRevisionToken => latestRevisionToken;

    /// <summary>The locally cached revision, or null if the level has never been downloaded.</summary>
    public LevelRevision? CachedRevision => cachedRevision;

    /// <summary>
    /// Reflects the synchronisation state between the server's latest release and the local cache.
    /// </summary>
    public LevelCacheStatus CacheStatus
    {
        get
        {
            if (cachedRevision is null)
            {
                return LevelCacheStatus.NotDownloaded;
            }

            return cachedRevision.RevisionToken.Equals(latestRevisionToken)
                ? LevelCacheStatus.Cached
                : LevelCacheStatus.Obsolete;
        }
    }

    /// <summary>
    /// Updates the latest revision token when the server releases a new revision.
    /// If the device already has this revision cached, no state change occurs.
    /// </summary>
    public void UpdateLatestRevision(LevelRevisionToken newToken)
    {
        latestRevisionToken = Guard.AgainstNull(newToken, nameof(newToken));
    }

    /// <summary>
    /// Stores a downloaded level revision locally. The revision must belong to this level.
    /// </summary>
    public void SetCachedRevision(LevelRevision revision)
    {
        Guard.AgainstNull(revision, nameof(revision));

        if (!string.Equals(revision.Definition.LevelId, LevelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot cache a revision for level '{revision.Definition.LevelId}' on released level '{LevelId}'.");
        }

        cachedRevision = revision;
    }

    /// <summary>Returns true when the level can be played (has a cached revision).</summary>
    public bool IsPlayable => cachedRevision is not null;
}
