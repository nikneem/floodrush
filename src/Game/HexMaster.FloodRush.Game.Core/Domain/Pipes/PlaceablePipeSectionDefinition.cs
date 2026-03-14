using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Pipes;

public sealed class PlaceablePipeSectionDefinition
{
    private readonly List<BoardDirection> openDirections = [];
    private readonly HashSet<FlowTraversalAxis> supportedAxes = [];

    public PlaceablePipeSectionDefinition(
        PipeSectionType pipeSectionType,
        int basePoints,
        int secondaryTraversalBonusPoints = 0)
    {
        SetPipeSectionType(pipeSectionType);
        SetBasePoints(basePoints);
        SetSecondaryTraversalBonusPoints(secondaryTraversalBonusPoints);
    }

    public PipeSectionType PipeSectionType { get; private set; }

    public int BasePoints { get; private set; }

    public int SecondaryTraversalBonusPoints { get; private set; }

    public IReadOnlyCollection<BoardDirection> OpenDirections => openDirections.AsReadOnly();

    public void SetPipeSectionType(PipeSectionType pipeSectionType)
    {
        if (pipeSectionType != PipeSectionType.Cross && SecondaryTraversalBonusPoints != 0)
        {
            throw new InvalidOperationException(
                "Only the cross section may define a secondary traversal bonus.");
        }

        PipeSectionType = pipeSectionType;
        ConfigureGeometry();
    }

    public void SetBasePoints(int basePoints) =>
        BasePoints = Guard.AgainstNegative(basePoints, nameof(basePoints));

    public void SetSecondaryTraversalBonusPoints(int secondaryTraversalBonusPoints)
    {
        if (PipeSectionType != PipeSectionType.Cross && secondaryTraversalBonusPoints != 0)
        {
            throw new ArgumentException(
                "Only the cross section may define a secondary traversal bonus.",
                nameof(secondaryTraversalBonusPoints));
        }

        SecondaryTraversalBonusPoints =
            Guard.AgainstNegative(secondaryTraversalBonusPoints, nameof(secondaryTraversalBonusPoints));
    }

    public bool SupportsAxis(FlowTraversalAxis axis) => supportedAxes.Contains(axis);

    public PlaceablePipeSectionDefinition Clone() =>
        new(PipeSectionType, BasePoints, SecondaryTraversalBonusPoints);

    public static IReadOnlyCollection<PlaceablePipeSectionDefinition> CreateRequiredSections() =>
    [
        new PlaceablePipeSectionDefinition(PipeSectionType.Horizontal, 10),
        new PlaceablePipeSectionDefinition(PipeSectionType.Vertical, 10),
        new PlaceablePipeSectionDefinition(PipeSectionType.CornerLeftToTop, 12),
        new PlaceablePipeSectionDefinition(PipeSectionType.CornerRightToTop, 12),
        new PlaceablePipeSectionDefinition(PipeSectionType.CornerLeftToBottom, 12),
        new PlaceablePipeSectionDefinition(PipeSectionType.CornerRightToBottom, 12),
        new PlaceablePipeSectionDefinition(PipeSectionType.Cross, 10, 50)
    ];

    private void ConfigureGeometry()
    {
        openDirections.Clear();
        supportedAxes.Clear();

        switch (PipeSectionType)
        {
            case PipeSectionType.Horizontal:
                openDirections.AddRange([BoardDirection.Left, BoardDirection.Right]);
                supportedAxes.Add(FlowTraversalAxis.Horizontal);
                break;
            case PipeSectionType.Vertical:
                openDirections.AddRange([BoardDirection.Top, BoardDirection.Bottom]);
                supportedAxes.Add(FlowTraversalAxis.Vertical);
                break;
            case PipeSectionType.CornerLeftToTop:
                openDirections.AddRange([BoardDirection.Left, BoardDirection.Top]);
                supportedAxes.UnionWith([FlowTraversalAxis.Horizontal, FlowTraversalAxis.Vertical]);
                break;
            case PipeSectionType.CornerRightToTop:
                openDirections.AddRange([BoardDirection.Right, BoardDirection.Top]);
                supportedAxes.UnionWith([FlowTraversalAxis.Horizontal, FlowTraversalAxis.Vertical]);
                break;
            case PipeSectionType.CornerLeftToBottom:
                openDirections.AddRange([BoardDirection.Left, BoardDirection.Bottom]);
                supportedAxes.UnionWith([FlowTraversalAxis.Horizontal, FlowTraversalAxis.Vertical]);
                break;
            case PipeSectionType.CornerRightToBottom:
                openDirections.AddRange([BoardDirection.Right, BoardDirection.Bottom]);
                supportedAxes.UnionWith([FlowTraversalAxis.Horizontal, FlowTraversalAxis.Vertical]);
                break;
            case PipeSectionType.Cross:
                openDirections.AddRange(
                [
                    BoardDirection.Left,
                    BoardDirection.Top,
                    BoardDirection.Right,
                    BoardDirection.Bottom
                ]);
                supportedAxes.UnionWith([FlowTraversalAxis.Horizontal, FlowTraversalAxis.Vertical]);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
