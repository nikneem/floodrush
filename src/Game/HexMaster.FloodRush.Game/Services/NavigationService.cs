namespace HexMaster.FloodRush.Game.Services;

public sealed class NavigationService : INavigationService
{
    public Task NavigateToLevelSelectionAsync() =>
        Shell.Current.GoToAsync(AppRoutes.LevelSelection);

    public Task NavigateToGameplayAsync(string levelId) =>
        Shell.Current.GoToAsync($"{AppRoutes.Gameplay}?levelId={levelId}");

    public Task NavigateToSettingsAsync() =>
        Shell.Current.GoToAsync(AppRoutes.Settings);

    public Task GoBackAsync() =>
        Shell.Current.GoToAsync("..");
}
