using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.Services;

public sealed class LocalStateService : ILocalStateService
{
    private const string KeyCurrentLevelId = "local_current_level_id";
    private const string KeySkipQuitConfirmation = "local_skip_quit_confirmation";

    private readonly ILogger<LocalStateService> logger;

    public LocalStateService(ILogger<LocalStateService> logger)
    {
        this.logger = logger;
    }

    public bool HasActiveProgress =>
        Preferences.Default.ContainsKey(KeyCurrentLevelId);

    public string? CurrentLevelId =>
        Preferences.Default.Get<string?>(KeyCurrentLevelId, null);

    public bool SkipQuitConfirmation =>
        Preferences.Default.Get(KeySkipQuitConfirmation, false);

    public void SetCurrentLevelId(string levelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);
        Preferences.Default.Set(KeyCurrentLevelId, levelId);
        logger.LogInformation("Stored current level progress for {LevelId}.", levelId);
    }

    public void SetSkipQuitConfirmation(bool value)
    {
        Preferences.Default.Set(KeySkipQuitConfirmation, value);
        logger.LogInformation("Updated skip quit confirmation preference to {Value}.", value);
    }

    public void ClearProgress()
    {
        Preferences.Default.Remove(KeyCurrentLevelId);
        logger.LogInformation("Cleared locally stored level progress.");
    }
}
