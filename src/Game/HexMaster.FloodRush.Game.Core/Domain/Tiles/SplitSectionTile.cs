using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Tiles;

public sealed class SplitSectionTile : FixedTile
{
    public SplitSectionTile(
        GridPosition position,
        BoardDirection entryDirection,
        BoardDirection primaryExitDirection,
        BoardDirection secondaryExitDirection,
        int speedModifierPercent,
        int bonusPoints)
        : base(position, bonusPoints)
    {
        EnsureValidDirections(entryDirection, primaryExitDirection, secondaryExitDirection);
        EntryDirection = entryDirection;
        PrimaryExitDirection = primaryExitDirection;
        SecondaryExitDirection = secondaryExitDirection;
        SetSpeedModifierPercent(speedModifierPercent);
    }

    public override FixedTileType FixedTileType => FixedTileType.SplitSection;

    public BoardDirection EntryDirection { get; private set; }

    public BoardDirection PrimaryExitDirection { get; private set; }

    public BoardDirection SecondaryExitDirection { get; private set; }

    public int SpeedModifierPercent { get; private set; }

    public void SetEntryDirection(BoardDirection entryDirection)
    {
        EnsureValidDirections(entryDirection, PrimaryExitDirection, SecondaryExitDirection);
        EntryDirection = entryDirection;
    }

    public void SetPrimaryExitDirection(BoardDirection primaryExitDirection)
    {
        EnsureValidDirections(EntryDirection, primaryExitDirection, SecondaryExitDirection);
        PrimaryExitDirection = primaryExitDirection;
    }

    public void SetSecondaryExitDirection(BoardDirection secondaryExitDirection)
    {
        EnsureValidDirections(EntryDirection, PrimaryExitDirection, secondaryExitDirection);
        SecondaryExitDirection = secondaryExitDirection;
    }

    public void SetSpeedModifierPercent(int speedModifierPercent) =>
        SpeedModifierPercent = Guard.AgainstOutOfRange(speedModifierPercent, 1, 500, nameof(speedModifierPercent));

    public override FixedTile Clone() =>
        new SplitSectionTile(
            Position,
            EntryDirection,
            PrimaryExitDirection,
            SecondaryExitDirection,
            SpeedModifierPercent,
            BonusPoints);

    internal override bool CanAcceptFlowFrom(BoardDirection direction) => direction == EntryDirection;

    internal override IReadOnlyCollection<BoardDirection> GetOutgoingDirections() =>
        [PrimaryExitDirection, SecondaryExitDirection];

    private static void EnsureValidDirections(
        BoardDirection entryDirection,
        BoardDirection primaryExitDirection,
        BoardDirection secondaryExitDirection)
    {
        if (entryDirection == primaryExitDirection ||
            entryDirection == secondaryExitDirection ||
            primaryExitDirection == secondaryExitDirection)
        {
            throw new InvalidOperationException(
                "A split section must define one entry direction and two distinct exit directions.");
        }
    }
}
