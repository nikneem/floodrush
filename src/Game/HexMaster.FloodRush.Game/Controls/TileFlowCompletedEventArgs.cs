using HexMaster.FloodRush.Game.Core.Domain.Board;

namespace HexMaster.FloodRush.Game.Controls;

public sealed class TileFlowCompletedEventArgs : EventArgs
{
    public int X { get; }
    public int Y { get; }

    /// <summary>
    /// The direction in which fluid left this tile.
    /// For terminal tiles this equals the entry direction.
    /// </summary>
    public BoardDirection ExitDirection { get; }

    public int PointsEarned { get; }

    /// <summary>True when the tile is the final destination and flow should stop.</summary>
    public bool IsTerminal { get; }

    public TileFlowCompletedEventArgs(
        int x, int y,
        BoardDirection exitDirection,
        int pointsEarned,
        bool isTerminal)
    {
        X = x;
        Y = y;
        ExitDirection = exitDirection;
        PointsEarned = pointsEarned;
        IsTerminal = isTerminal;
    }
}
