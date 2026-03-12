namespace HexMaster.FloodRush.Game.Core.Domain.Board;

internal static class BoardDirectionExtensions
{
    public static IReadOnlyCollection<BoardDirection> AllDirections { get; } =
    [
        BoardDirection.Left,
        BoardDirection.Top,
        BoardDirection.Right,
        BoardDirection.Bottom
    ];

    public static BoardDirection Opposite(this BoardDirection direction) =>
        direction switch
        {
            BoardDirection.Left => BoardDirection.Right,
            BoardDirection.Top => BoardDirection.Bottom,
            BoardDirection.Right => BoardDirection.Left,
            BoardDirection.Bottom => BoardDirection.Top,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
}
