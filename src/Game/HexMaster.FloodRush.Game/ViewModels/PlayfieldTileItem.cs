namespace HexMaster.FloodRush.Game.ViewModels;

public enum PlayfieldTileKind
{
    Empty = 0,
    StartPoint = 1,
    FinishPoint = 2,
    FluidBasin = 3,
    SplitSection = 4
}

public sealed record PlayfieldTileItem(
    int X,
    int Y,
    PlayfieldTileKind Kind,
    string BackgroundImage,
    string Title,
    string Subtitle,
    string PipeOverlayImage = "",
    double PipeImageRotation = 0d);
