namespace HexMaster.FloodRush.Game.Controls;

/// <summary>
/// Carries the positions of unused pipes and a callback the page invokes after
/// all removal animations have completed, allowing the ViewModel to finish the
/// success transition.
/// </summary>
public sealed class UnusedPipeRemovalEventArgs : EventArgs
{
    private readonly Action onComplete;

    public IReadOnlyList<(int X, int Y)> Positions { get; }

    public UnusedPipeRemovalEventArgs(IReadOnlyList<(int X, int Y)> positions, Action onComplete)
    {
        Positions = positions;
        this.onComplete = onComplete;
    }

    /// <summary>
    /// Called by the page after all fade-out animations have finished.
    /// Triggers the ViewModel to clear unused tiles and show the level-complete dialog.
    /// </summary>
    public void Complete() => onComplete();
}
