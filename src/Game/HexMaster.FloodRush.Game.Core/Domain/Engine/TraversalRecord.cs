using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;
using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.Core.Domain.Engine;

/// <summary>
/// An immutable record of a single pipe traversal, capturing the position,
/// which axis was used, and how many points were awarded.
/// </summary>
public sealed class TraversalRecord
{
    public TraversalRecord(GridPosition position, FlowTraversalAxis axis, int pointsAwarded)
    {
        Position = Guard.AgainstNull(position, nameof(position));
        Axis = axis;
        PointsAwarded = Guard.AgainstNegative(pointsAwarded, nameof(pointsAwarded));
    }

    public GridPosition Position { get; }
    public FlowTraversalAxis Axis { get; }
    public int PointsAwarded { get; }
}
