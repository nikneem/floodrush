using HexMaster.FloodRush.Game.Core.Domain.Board;

namespace HexMaster.FloodRush.Game.Core.Domain.Tiles;

public sealed class FinishPointTile : FixedTile
{
    public FinishPointTile(GridPosition position, BoardDirection entryDirection)
        : base(position, 0)
    {
        SetEntryDirection(entryDirection);
    }

    public override FixedTileType FixedTileType => FixedTileType.FinishPoint;

    public BoardDirection EntryDirection { get; private set; }

    public void SetEntryDirection(BoardDirection entryDirection) =>
        EntryDirection = entryDirection;

    public override FixedTile Clone() => new FinishPointTile(Position, EntryDirection);

    internal override bool CanAcceptFlowFrom(BoardDirection direction) => direction == EntryDirection;

    internal override IReadOnlyCollection<BoardDirection> GetOutgoingDirections() => [];
}
