namespace HexMaster.FloodRush.Game.Services;

public interface INavigationService
{
    Task NavigateToLevelSelectionAsync();
    Task NavigateToGameplayAsync(string levelId);
    Task NavigateToSettingsAsync();
    Task GoBackAsync();
}
