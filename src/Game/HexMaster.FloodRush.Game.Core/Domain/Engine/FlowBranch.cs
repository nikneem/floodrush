using HexMaster.FloodRush.Game.Core.Domain.Board;

namespace HexMaster.FloodRush.Game.Core.Domain.Engine;

/// <summary>
/// Internal data holder representing a single active flow front (branch).
/// All mutation is performed directly by <see cref="GameSession"/>.
/// </summary>
internal sealed class FlowBranch
{
    internal FlowBranch(
        GridPosition position,
        BoardDirection pendingExitDirection,
        long requiredMilliseconds,
        int speedModifierPercent = 100)
    {
        Position = position;
        PendingExitDirection = pendingExitDirection;
        RequiredMilliseconds = requiredMilliseconds;
        SpeedModifierPercent = speedModifierPercent;
        Status = FlowBranchStatus.Active;
        AccumulatedMilliseconds = 0;
    }

    /// <summary>Current tile position of this flow front.</summary>
    internal GridPosition Position { get; set; }

    /// <summary>
    /// The direction this branch will move when its timer expires.
    /// Updated each time the branch successfully enters a new tile.
    /// </summary>
    internal BoardDirection PendingExitDirection { get; set; }

    internal FlowBranchStatus Status { get; set; }

    /// <summary>Milliseconds accumulated toward completing the current tile transit.</summary>
    internal long AccumulatedMilliseconds { get; set; }

    /// <summary>
    /// Milliseconds required to complete the current transit (includes basin fill delay if applicable).
    /// </summary>
    internal long RequiredMilliseconds { get; set; }

    /// <summary>
    /// Speed modifier inherited from the last split section encountered (100 = normal speed).
    /// Values above 100 make flow faster; below 100 make it slower.
    /// </summary>
    internal int SpeedModifierPercent { get; set; }
}
