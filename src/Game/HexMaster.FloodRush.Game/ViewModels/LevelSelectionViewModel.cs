using HexMaster.FloodRush.Game.Services;

namespace HexMaster.FloodRush.Game.ViewModels;

public sealed class LevelSelectionViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;

    public List<LevelListItem> Levels { get; } = [];

    public Command<LevelListItem> SelectLevelCommand { get; }
    public Command BackCommand { get; }

    public LevelSelectionViewModel(INavigationService navigation)
    {
        _navigation = navigation;

        SelectLevelCommand = new Command<LevelListItem>(async item =>
        {
            if (item is not null)
                await _navigation.NavigateToGameplayAsync(item.LevelId);
        });

        BackCommand = new Command(async () => await _navigation.GoBackAsync());
    }

    public void LoadLevels()
    {
        Levels.Clear();
        Levels.Add(new LevelListItem("level-001", "The First Drop", "Easy", false));
        Levels.Add(new LevelListItem("level-002", "Double Basin", "Medium", false));
        Levels.Add(new LevelListItem("level-003", "Split Decision", "Hard", false));
    }
}

public sealed record LevelListItem(string LevelId, string DisplayName, string Difficulty, bool IsCompleted);
