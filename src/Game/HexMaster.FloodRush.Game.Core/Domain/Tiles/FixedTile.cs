using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Tiles;

public abstract class FixedTile
{
    private GridPosition position;

    protected FixedTile(GridPosition position, int bonusPoints)
    {
        this.position = Guard.AgainstNull(position, nameof(position)).Clone();
        SetBonusPoints(bonusPoints);
    }

    public abstract FixedTileType FixedTileType { get; }

    public GridPosition Position
    {
        get => position.Clone();
        private set => position = value.Clone();
    }

    public int BonusPoints { get; private set; }

    public void SetPosition(GridPosition position) =>
        Position = Guard.AgainstNull(position, nameof(position)).Clone();

    public void SetBonusPoints(int bonusPoints) =>
        BonusPoints = Guard.AgainstNegative(bonusPoints, nameof(bonusPoints));

    public abstract FixedTile Clone();

    internal abstract bool CanAcceptFlowFrom(BoardDirection direction);

    internal abstract IReadOnlyCollection<BoardDirection> GetOutgoingDirections();
}
