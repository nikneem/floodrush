using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;
using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.Core.Domain.Engine;

/// <summary>
/// A player-placed pipe section on the board. Carries resolved scoring and
/// provides geometry helpers (open directions, exit direction lookup).
/// </summary>
public sealed class PlacedPipe
{
    private static readonly IReadOnlyDictionary<PipeSectionType, BoardDirection[]> GeometryByType =
        new Dictionary<PipeSectionType, BoardDirection[]>
        {
            [PipeSectionType.Horizontal] = [BoardDirection.Left, BoardDirection.Right],
            [PipeSectionType.Vertical] = [BoardDirection.Top, BoardDirection.Bottom],
            [PipeSectionType.CornerLeftToTop] = [BoardDirection.Left, BoardDirection.Top],
            [PipeSectionType.CornerRightToTop] = [BoardDirection.Right, BoardDirection.Top],
            [PipeSectionType.CornerLeftToBottom] = [BoardDirection.Left, BoardDirection.Bottom],
            [PipeSectionType.CornerRightToBottom] = [BoardDirection.Right, BoardDirection.Bottom],
            [PipeSectionType.Cross] = [BoardDirection.Left, BoardDirection.Top, BoardDirection.Right, BoardDirection.Bottom],
        };

    public PlacedPipe(
        GridPosition position,
        PipeSectionType pipeType,
        int basePoints,
        int secondaryTraversalBonusPoints = 0)
    {
        Position = Guard.AgainstNull(position, nameof(position));

        if (!Enum.IsDefined(pipeType))
        {
            throw new ArgumentOutOfRangeException(nameof(pipeType), "Unknown pipe section type.");
        }

        if (pipeType != PipeSectionType.Cross && secondaryTraversalBonusPoints != 0)
        {
            throw new ArgumentException(
                "Only the Cross pipe may have a secondary traversal bonus.",
                nameof(secondaryTraversalBonusPoints));
        }

        PipeType = pipeType;
        BasePoints = Guard.AgainstNegative(basePoints, nameof(basePoints));
        SecondaryTraversalBonusPoints = Guard.AgainstNegative(secondaryTraversalBonusPoints, nameof(secondaryTraversalBonusPoints));
        OpenDirections = GeometryByType[pipeType];
    }

    public GridPosition Position { get; }
    public PipeSectionType PipeType { get; }
    public int BasePoints { get; }

    /// <summary>Extra points for the second traversal of a Cross pipe. Always 0 for non-Cross types.</summary>
    public int SecondaryTraversalBonusPoints { get; }

    /// <summary>The directions in which this pipe section is open to fluid flow.</summary>
    public IReadOnlyCollection<BoardDirection> OpenDirections { get; }

    /// <summary>Returns true when fluid can enter this pipe from the given direction.</summary>
    public bool CanAcceptFlowFrom(BoardDirection direction) => OpenDirections.Contains(direction);

    /// <summary>
    /// Returns the exit direction given the direction from which fluid entered.
    /// Throws if <paramref name="enteredFrom"/> is not an open direction of this pipe.
    /// </summary>
    public BoardDirection GetExitDirection(BoardDirection enteredFrom)
    {
        if (!CanAcceptFlowFrom(enteredFrom))
        {
            throw new InvalidOperationException(
                $"Pipe type {PipeType} at ({Position.X},{Position.Y}) cannot accept flow from {enteredFrom}.");
        }

        // Cross always exits opposite to the entry direction.
        if (PipeType == PipeSectionType.Cross)
        {
            return enteredFrom.Opposite();
        }

        // For all other types, there are exactly two open directions; the exit is the other one.
        return OpenDirections.First(d => d != enteredFrom);
    }
}
