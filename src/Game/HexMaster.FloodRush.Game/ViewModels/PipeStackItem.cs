using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.ViewModels;

public sealed record PipeStackItem(
    PipeSectionType PipeType,
    string PipeImage,
    string Label,
    string QueueLabel,
    bool IsNextToPlace,
    double PreviewOpacity);
