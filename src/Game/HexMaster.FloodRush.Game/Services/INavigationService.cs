namespace HexMaster.FloodRush.Game.Services;

public interface INavigationService
{
    Task NavigateToLevelSelectionAsync(bool refreshLevels = false);
    Task NavigateToGameplayAsync(string levelId);
    Task NavigateToSettingsAsync();
    Task GoBackAsync();
}
