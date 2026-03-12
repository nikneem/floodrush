namespace HexMaster.FloodRush.Game.Core.Domain.Engine;

public enum GamePhase
{
    /// <summary>Level is loaded and ready. No pipes have been placed yet.</summary>
    LevelLoaded = 0,

    /// <summary>The player is placing pipes before flow begins.</summary>
    PlacementWindow = 1,

    /// <summary>Fluid is actively flowing through the board.</summary>
    FlowActive = 2,

    /// <summary>All required finish points were reached. The level is won.</summary>
    Succeeded = 3,

    /// <summary>Flow reached a dead end or an invalid connection before all finish points were fulfilled.</summary>
    Failed = 4
}
