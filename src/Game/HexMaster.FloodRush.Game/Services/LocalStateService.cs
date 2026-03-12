namespace HexMaster.FloodRush.Game.Services;

public sealed class LocalStateService : ILocalStateService
{
    private const string KeyCurrentLevelId = "local_current_level_id";

    public bool HasActiveProgress =>
        Preferences.Default.ContainsKey(KeyCurrentLevelId);

    public string? CurrentLevelId =>
        Preferences.Default.Get<string?>(KeyCurrentLevelId, null);

    public void ClearProgress() =>
        Preferences.Default.Remove(KeyCurrentLevelId);
}
