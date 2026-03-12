namespace HexMaster.FloodRush.Game.Services;

public interface ILocalStateService
{
    bool HasActiveProgress { get; }
    string? CurrentLevelId { get; }
    void ClearProgress();
}
