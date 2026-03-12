using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Board;

public sealed class BoardDimensions : IEquatable<BoardDimensions>
{
    public BoardDimensions(int width, int height)
    {
        SetWidth(width);
        SetHeight(height);
    }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public void SetWidth(int width) =>
        Width = Guard.AgainstOutOfRange(width, 1, 500, nameof(width));

    public void SetHeight(int height) =>
        Height = Guard.AgainstOutOfRange(height, 1, 500, nameof(height));

    public void SetDimensions(int width, int height)
    {
        var candidateWidth = Guard.AgainstOutOfRange(width, 1, 500, nameof(width));
        var candidateHeight = Guard.AgainstOutOfRange(height, 1, 500, nameof(height));

        Width = candidateWidth;
        Height = candidateHeight;
    }

    public bool Contains(GridPosition position) =>
        position.X >= 0 &&
        position.Y >= 0 &&
        position.X < Width &&
        position.Y < Height;

    public BoardDimensions Clone() => new(Width, Height);

    public bool Equals(BoardDimensions? other) =>
        other is not null &&
        Width == other.Width &&
        Height == other.Height;

    public override bool Equals(object? obj) => Equals(obj as BoardDimensions);

    public override int GetHashCode() => HashCode.Combine(Width, Height);
}
