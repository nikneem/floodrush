using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Board;

public sealed class GridPosition : IEquatable<GridPosition>
{
    public GridPosition(int x, int y)
    {
        SetX(x);
        SetY(y);
    }

    public int X { get; private set; }

    public int Y { get; private set; }

    public void SetX(int x) =>
        X = Guard.AgainstNegative(x, nameof(x));

    public void SetY(int y) =>
        Y = Guard.AgainstNegative(y, nameof(y));

    public void SetCoordinates(int x, int y)
    {
        var candidateX = Guard.AgainstNegative(x, nameof(x));
        var candidateY = Guard.AgainstNegative(y, nameof(y));

        X = candidateX;
        Y = candidateY;
    }

    public GridPosition Move(BoardDirection direction) =>
        direction switch
        {
            BoardDirection.Left => new GridPosition(X - 1, Y),
            BoardDirection.Top => new GridPosition(X, Y - 1),
            BoardDirection.Right => new GridPosition(X + 1, Y),
            BoardDirection.Bottom => new GridPosition(X, Y + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };

    public GridPosition Clone() => new(X, Y);

    public bool Equals(GridPosition? other) =>
        other is not null &&
        X == other.X &&
        Y == other.Y;

    public override bool Equals(object? obj) => Equals(obj as GridPosition);

    public override int GetHashCode() => HashCode.Combine(X, Y);
}
