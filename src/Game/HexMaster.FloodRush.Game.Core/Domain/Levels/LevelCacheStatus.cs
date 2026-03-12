namespace HexMaster.FloodRush.Game.Core.Domain.Levels;

public enum LevelCacheStatus
{
    /// <summary>The level has never been downloaded to this device.</summary>
    NotDownloaded = 0,

    /// <summary>The cached revision matches the latest server revision.</summary>
    Cached = 1,

    /// <summary>A newer revision has been released; the cached copy is stale.</summary>
    Obsolete = 2
}
