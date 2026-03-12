using HexMaster.FloodRush.Game.Core.Domain.Board;

namespace HexMaster.FloodRush.Game.Core.Domain.Tiles;

public sealed class StartPointTile : FixedTile
{
    public StartPointTile(GridPosition position, BoardDirection outputDirection)
        : base(position, 0)
    {
        SetOutputDirection(outputDirection);
    }

    public override FixedTileType FixedTileType => FixedTileType.StartPoint;

    public BoardDirection OutputDirection { get; private set; }

    public void SetOutputDirection(BoardDirection outputDirection) =>
        OutputDirection = outputDirection;

    public override FixedTile Clone() => new StartPointTile(Position, OutputDirection);

    internal override bool CanAcceptFlowFrom(BoardDirection direction) => false;

    internal override IReadOnlyCollection<BoardDirection> GetOutgoingDirections() => [OutputDirection];
}
