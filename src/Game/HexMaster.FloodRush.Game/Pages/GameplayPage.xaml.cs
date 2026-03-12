using HexMaster.FloodRush.Game.ViewModels;

namespace HexMaster.FloodRush.Game.Pages;

public partial class GameplayPage : ContentPage
{
    public GameplayPage(GameplayViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
