using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Tiles;

public sealed class FluidBasinTile : FixedTile
{
    public FluidBasinTile(
        GridPosition position,
        BoardDirection entryDirection,
        BoardDirection exitDirection,
        int fillDelayMilliseconds,
        int bonusPoints)
        : base(position, bonusPoints)
    {
        EnsureValidDirections(entryDirection, exitDirection);
        EntryDirection = entryDirection;
        ExitDirection = exitDirection;
        SetFillDelayMilliseconds(fillDelayMilliseconds);
    }

    public override FixedTileType FixedTileType => FixedTileType.FluidBasin;

    public BoardDirection EntryDirection { get; private set; }

    public BoardDirection ExitDirection { get; private set; }

    public int FillDelayMilliseconds { get; private set; }

    public void SetEntryDirection(BoardDirection entryDirection)
    {
        EnsureValidDirections(entryDirection, ExitDirection);
        EntryDirection = entryDirection;
    }

    public void SetExitDirection(BoardDirection exitDirection)
    {
        EnsureValidDirections(EntryDirection, exitDirection);
        ExitDirection = exitDirection;
    }

    public void SetFillDelayMilliseconds(int fillDelayMilliseconds) =>
        FillDelayMilliseconds = Guard.AgainstNegative(fillDelayMilliseconds, nameof(fillDelayMilliseconds));

    public override FixedTile Clone() =>
        new FluidBasinTile(Position, EntryDirection, ExitDirection, FillDelayMilliseconds, BonusPoints);

    internal override bool CanAcceptFlowFrom(BoardDirection direction) => direction == EntryDirection;

    internal override IReadOnlyCollection<BoardDirection> GetOutgoingDirections() => [ExitDirection];

    private static void EnsureValidDirections(BoardDirection entryDirection, BoardDirection exitDirection)
    {
        if (entryDirection == exitDirection)
        {
            throw new InvalidOperationException("A fluid basin must have distinct entry and exit directions.");
        }
    }
}
