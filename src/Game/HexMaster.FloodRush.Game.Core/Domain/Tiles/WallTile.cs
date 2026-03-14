using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Tiles;

/// <summary>
/// A designer-placed tile that permanently blocks a board cell.
/// Players cannot place pipes on wall tiles, and fluid can never enter a wall cell.
/// </summary>
public sealed class WallTile : FixedTile
{
    public WallTile(GridPosition position)
        : base(Guard.AgainstNull(position, nameof(position)), bonusPoints: 0)
    {
    }

    public override FixedTileType FixedTileType => FixedTileType.Wall;

    public override FixedTile Clone() => new WallTile(Position);

    /// <summary>
    /// Walls never accept flow from any direction.
    /// Any branch attempting to enter a wall is immediately failed by the engine.
    /// </summary>
    internal override bool CanAcceptFlowFrom(BoardDirection direction) => false;

    /// <summary>
    /// Walls have no outgoing directions.
    /// The BFS reachability check will not expand through wall cells.
    /// </summary>
    internal override IReadOnlyCollection<BoardDirection> GetOutgoingDirections() => [];
}
