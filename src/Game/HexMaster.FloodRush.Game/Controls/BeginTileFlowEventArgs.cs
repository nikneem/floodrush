using HexMaster.FloodRush.Game.Core.Domain.Board;

namespace HexMaster.FloodRush.Game.Controls;

public sealed class BeginTileFlowEventArgs : EventArgs
{
    public int X { get; init; }
    public int Y { get; init; }
    public BoardDirection EntryDirection { get; init; }
    public BoardDirection ExitDirection { get; init; }
    public int Points { get; init; }
    public int DurationMs { get; init; }

    /// <summary>
    /// True for tiles where fluid terminates (e.g. the finish point).
    /// The animation enters from EntryDirection to the tile centre and stops there.
    /// </summary>
    public bool IsTerminal { get; init; }
}
