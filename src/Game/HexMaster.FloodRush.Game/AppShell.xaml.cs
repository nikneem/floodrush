using HexMaster.FloodRush.Game.Pages;
using HexMaster.FloodRush.Game.Services;

namespace HexMaster.FloodRush.Game;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(AppRoutes.LevelSelection, typeof(LevelSelectionPage));
        Routing.RegisterRoute(AppRoutes.Gameplay, typeof(GameplayPage));
        Routing.RegisterRoute(AppRoutes.Settings, typeof(SettingsPage));
    }
}
