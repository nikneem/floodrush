namespace HexMaster.FloodRush.Game.Services;

public interface ILocalStateService
{
    bool HasActiveProgress { get; }
    string? CurrentLevelId { get; }
    bool SkipQuitConfirmation { get; }
    IReadOnlyCollection<string> CompletedLevelIds { get; }
    void SetCurrentLevelId(string levelId);
    void SetSkipQuitConfirmation(bool value);
    void RecordLevelCompletion(string levelId);
    void ClearProgress();
}
