using HexMaster.FloodRush.Game.ViewModels;

namespace HexMaster.FloodRush.Game.Pages;

public partial class LevelSelectionPage : ContentPage
{
    public LevelSelectionPage(LevelSelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        viewModel.LoadLevels();
    }
}
