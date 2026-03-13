namespace HexMaster.FloodRush.Game.Controls;

/// <summary>
/// Raised by <c>GameplayViewModel</c> when a player replaces an existing pipe and
/// the 3-second shake-and-disintegrate penalty animation must play before the
/// new pipe is committed.
/// </summary>
public sealed class PipeRemovalEventArgs : EventArgs
{
    private readonly Action completionCallback;

    /// <summary>Board column of the tile being replaced.</summary>
    public int X { get; }

    /// <summary>Board row of the tile being replaced.</summary>
    public int Y { get; }

    public PipeRemovalEventArgs(int x, int y, Action completionCallback)
    {
        X = x;
        Y = y;
        this.completionCallback = completionCallback;
    }

    /// <summary>
    /// Invoke once the removal animation finishes.  The ViewModel will then
    /// clear the penalty lock, commit the new pipe to <c>BoardTiles</c>, and
    /// update the pipe stack.
    /// </summary>
    public void Complete() => completionCallback();
}
