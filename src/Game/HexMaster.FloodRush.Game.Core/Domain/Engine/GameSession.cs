using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Common;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Pipes;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Domain.Engine;

/// <summary>
/// The main simulation engine for a single level play-through.
///
/// Lifecycle:
///   <see cref="StartPlacementPhase"/> → <see cref="PlacePipe"/> / <see cref="RemovePipe"/>
///   → <see cref="StartFlow"/> → <see cref="Tick"/> (repeated)
///   → <see cref="GamePhase.Succeeded"/> or <see cref="GamePhase.Failed"/>
///
/// Time is supplied externally via <see cref="Tick(long)"/>, keeping the engine
/// fully deterministic and testable without a real clock.
/// </summary>
public sealed class GameSession
{
    private static readonly IReadOnlyCollection<PlaceablePipeSectionDefinition> DefaultPipeDefinitions =
        PlaceablePipeSectionDefinition.CreateRequiredSections();

    private readonly LevelDefinition level;
    private readonly HashSet<GridPosition> requiredFinishPositions;
    private readonly HashSet<GridPosition> mandatoryBasinPositions;
    private readonly HashSet<GridPosition> reachedFinishPoints = [];
    private readonly HashSet<GridPosition> traversedFixedTiles = [];
    private readonly Dictionary<GridPosition, HashSet<FlowTraversalAxis>> traversedAxesByPosition = [];
    private readonly List<FlowBranch> branches = [];
    private readonly int completionBonusPoints;
    private long startDelayRemainingMs;

    public GameSession(LevelDefinition level, int completionBonusPoints = 1000)
    {
        this.level = Guard.AgainstNull(level, nameof(level));
        this.completionBonusPoints = Guard.AgainstNegative(completionBonusPoints, nameof(completionBonusPoints));
        Board = GameBoard.FromLevel(level);
        Score = new ScoreBreakdown();
        requiredFinishPositions = [.. level.FixedTiles.OfType<FinishPointTile>().Select(t => t.Position)];
        mandatoryBasinPositions = [.. level.FixedTiles.OfType<FluidBasinTile>().Where(b => b.IsMandatory).Select(t => t.Position)];
        Phase = GamePhase.LevelLoaded;
    }

    public GamePhase Phase { get; private set; }

    /// <summary>The board for this session (fixed tiles + player-placed pipes).</summary>
    public GameBoard Board { get; }

    /// <summary>Running score breakdown for this session.</summary>
    public ScoreBreakdown Score { get; }

    /// <summary>Finish-point positions that have been reached so far.</summary>
    public IReadOnlyCollection<GridPosition> ReachedFinishPoints => reachedFinishPoints;

    // ── Phase transitions ────────────────────────────────────────────────────

    /// <summary>
    /// Transitions from <see cref="GamePhase.LevelLoaded"/> to
    /// <see cref="GamePhase.PlacementWindow"/>, allowing pipe placement.
    /// </summary>
    public void StartPlacementPhase()
    {
        if (Phase != GamePhase.LevelLoaded)
        {
            throw new InvalidOperationException(
                "Placement phase can only be started from the LevelLoaded state.");
        }

        Phase = GamePhase.PlacementWindow;
    }

    /// <summary>
    /// Transitions to <see cref="GamePhase.FlowActive"/> and begins the start-delay countdown.
    /// If the level has no start delay, flow branches are activated immediately.
    /// </summary>
    public void StartFlow()
    {
        if (Phase != GamePhase.PlacementWindow)
        {
            throw new InvalidOperationException(
                "Flow can only be started during the placement window.");
        }

        Phase = GamePhase.FlowActive;

        if (level.StartDelayMilliseconds == 0)
        {
            ActivateStartBranches();
        }
        else
        {
            startDelayRemainingMs = level.StartDelayMilliseconds;
        }
    }

    // ── Pipe placement ───────────────────────────────────────────────────────

    /// <summary>
    /// Places a pipe of the given type at <paramref name="position"/>.
    /// Overwrites any previously placed pipe at that position.
    /// </summary>
    public void PlacePipe(GridPosition position, PipeSectionType pipeType)
    {
        if (Phase != GamePhase.PlacementWindow)
        {
            throw new InvalidOperationException("Pipes can only be placed during the placement window.");
        }

        Guard.AgainstNull(position, nameof(position));

        if (!Board.IsWithinBounds(position))
        {
            throw new InvalidOperationException(
                $"Position ({position.X},{position.Y}) is outside the board boundaries.");
        }

        if (Board.GetFixedTile(position) is not null)
        {
            throw new InvalidOperationException(
                $"Position ({position.X},{position.Y}) is occupied by a fixed tile.");
        }

        var (basePoints, secondaryBonus) = ResolvePoints(pipeType);
        Board.PlacePipe(new PlacedPipe(position, pipeType, basePoints, secondaryBonus));
    }

    /// <summary>Removes a player-placed pipe. No-op if no pipe exists at <paramref name="position"/>.</summary>
    public void RemovePipe(GridPosition position)
    {
        if (Phase != GamePhase.PlacementWindow)
        {
            throw new InvalidOperationException("Pipes can only be removed during the placement window.");
        }

        Guard.AgainstNull(position, nameof(position));
        Board.RemovePipe(position);
    }

    // ── Simulation tick ──────────────────────────────────────────────────────

    /// <summary>
    /// Advances the simulation by <paramref name="elapsedMilliseconds"/> milliseconds.
    /// Has no effect when the session is not in <see cref="GamePhase.FlowActive"/>.
    /// </summary>
    public void Tick(long elapsedMilliseconds)
    {
        if (Phase != GamePhase.FlowActive) return;

        if (elapsedMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds), "Elapsed time cannot be negative.");
        }

        var remainingMs = elapsedMilliseconds;

        // Process start delay
        if (startDelayRemainingMs > 0)
        {
            var consumed = Math.Min(startDelayRemainingMs, remainingMs);
            startDelayRemainingMs -= consumed;
            remainingMs -= consumed;

            if (startDelayRemainingMs > 0) return;

            ActivateStartBranches();
        }

        // Take a snapshot; advancing a branch may enqueue new branches (e.g., from splits)
        // but those are processed next tick for deterministic ordering.
        var snapshot = branches.Where(b => b.Status == FlowBranchStatus.Active).ToList();
        foreach (var branch in snapshot)
        {
            AdvanceBranch(branch, remainingMs);
        }

        CheckGameOverConditions();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void ActivateStartBranches()
    {
        foreach (var startTile in level.FixedTiles.OfType<StartPointTile>())
        {
            var transitMs = CalculateTransitMs(100);
            branches.Add(new FlowBranch(startTile.Position, startTile.OutputDirection, transitMs));
        }
    }

    private void AdvanceBranch(FlowBranch branch, long elapsedMs)
    {
        branch.AccumulatedMilliseconds += elapsedMs;

        while (branch.Status == FlowBranchStatus.Active &&
               branch.AccumulatedMilliseconds >= branch.RequiredMilliseconds)
        {
            branch.AccumulatedMilliseconds -= branch.RequiredMilliseconds;
            TryAdvanceBranchOneStep(branch);
        }
    }

    private void TryAdvanceBranchOneStep(FlowBranch branch)
    {
        // Determine next position
        GridPosition nextPos;
        try
        {
            nextPos = branch.Position.Move(branch.PendingExitDirection);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Moved to a negative coordinate → exited the board
            branch.Status = FlowBranchStatus.Failed;
            return;
        }

        if (!Board.IsWithinBounds(nextPos))
        {
            branch.Status = FlowBranchStatus.Failed;
            return;
        }

        // The direction from which the flow enters the next tile
        var enteredFrom = branch.PendingExitDirection.Opposite();
        var transitMs = CalculateTransitMs(branch.SpeedModifierPercent);

        // Fixed tile takes priority
        var fixedTile = Board.GetFixedTile(nextPos);
        if (fixedTile is not null)
        {
            HandleFixedTileEntry(branch, fixedTile, enteredFrom, transitMs);
            return;
        }

        // Player-placed pipe
        var placedPipe = Board.GetPlacedPipe(nextPos);
        if (placedPipe is not null)
        {
            HandlePlacedPipeEntry(branch, placedPipe, nextPos, enteredFrom, transitMs);
            return;
        }

        // Empty cell — no path forward
        branch.Status = FlowBranchStatus.Failed;
    }

    private void HandleFixedTileEntry(
        FlowBranch branch,
        FixedTile tile,
        BoardDirection enteredFrom,
        long transitMs)
    {
        if (!tile.CanAcceptFlowFrom(enteredFrom))
        {
            branch.Status = FlowBranchStatus.Failed;
            return;
        }

        switch (tile)
        {
            case FinishPointTile:
                reachedFinishPoints.Add(tile.Position);
                branch.Status = FlowBranchStatus.Completed;
                break;

            case FluidBasinTile basin:
                if (!traversedFixedTiles.Add(basin.Position))
                {
                    // Basin already used; circular flow
                    branch.Status = FlowBranchStatus.Failed;
                    return;
                }

                Score.AddBasinBonus(basin.BonusPoints);
                branch.Position = basin.Position;
                branch.PendingExitDirection = enteredFrom == basin.EntryDirection
                    ? basin.ExitDirection
                    : basin.EntryDirection;
                branch.RequiredMilliseconds = transitMs * 3;
                break;

            case SplitSectionTile split:
                if (!traversedFixedTiles.Add(split.Position))
                {
                    // Split already used
                    branch.Status = FlowBranchStatus.Failed;
                    return;
                }

                Score.AddSplitBonus(split.BonusPoints);

                var splitTransitMs = CalculateTransitMs(split.SpeedModifierPercent);
                branches.Add(new FlowBranch(
                    split.Position,
                    split.PrimaryExitDirection,
                    splitTransitMs,
                    split.SpeedModifierPercent));
                branches.Add(new FlowBranch(
                    split.Position,
                    split.SecondaryExitDirection,
                    splitTransitMs,
                    split.SpeedModifierPercent));

                branch.Status = FlowBranchStatus.Completed;
                break;

            default:
                branch.Status = FlowBranchStatus.Failed;
                break;
        }
    }

    private void HandlePlacedPipeEntry(
        FlowBranch branch,
        PlacedPipe pipe,
        GridPosition nextPos,
        BoardDirection enteredFrom,
        long transitMs)
    {
        if (!pipe.CanAcceptFlowFrom(enteredFrom))
        {
            branch.Status = FlowBranchStatus.Failed;
            return;
        }

        var axis = GetTraversalAxis(enteredFrom);
        var points = TryTraversePipe(nextPos, pipe, axis);

        if (points < 0)
        {
            branch.Status = FlowBranchStatus.Failed;
            return;
        }

        if (points > 0)
        {
            Score.AddTraversal(new TraversalRecord(nextPos, axis, points));
        }

        var exitDir = pipe.GetExitDirection(enteredFrom);
        branch.Position = nextPos;
        branch.PendingExitDirection = exitDir;
        branch.RequiredMilliseconds = transitMs;
    }

    /// <summary>
    /// Attempts to record a pipe traversal and returns the points awarded.
    /// Returns -1 when the traversal is invalid (already traversed on the same axis,
    /// or a non-Cross pipe is revisited).
    /// </summary>
    private int TryTraversePipe(GridPosition position, PlacedPipe pipe, FlowTraversalAxis axis)
    {
        var isCross = pipe.PipeType == PipeSectionType.Cross;

        if (traversedAxesByPosition.TryGetValue(position, out var axes))
        {
            if (axes.Contains(axis))
            {
                // Same axis traversed again — invalid regardless of pipe type
                return -1;
            }

            if (!isCross)
            {
                // Non-Cross pipe revisited on a different axis — invalid
                return -1;
            }

            // Cross pipe: second traversal on the opposite axis — award secondary bonus
            axes.Add(axis);
            return pipe.SecondaryTraversalBonusPoints;
        }

        // First traversal of this position
        traversedAxesByPosition[position] = [axis];
        return pipe.BasePoints;
    }

    private void CheckGameOverConditions()
    {
        if (requiredFinishPositions.Count > 0 &&
            requiredFinishPositions.All(reachedFinishPoints.Contains) &&
            mandatoryBasinPositions.All(traversedFixedTiles.Contains))
        {
            Score.SetCompletionBonus(completionBonusPoints);
            Phase = GamePhase.Succeeded;
            return;
        }

        // Fail when all branches have terminated without success
        if (branches.Count > 0 && branches.All(b => b.Status != FlowBranchStatus.Active))
        {
            Phase = GamePhase.Failed;
        }
    }

    private long CalculateTransitMs(int speedModifierPercent) =>
        (101L - level.FlowSpeedIndicator.Value) * 10L * 100L / speedModifierPercent;

    private static FlowTraversalAxis GetTraversalAxis(BoardDirection direction) =>
        direction is BoardDirection.Left or BoardDirection.Right
            ? FlowTraversalAxis.Horizontal
            : FlowTraversalAxis.Vertical;

    private (int basePoints, int secondaryBonus) ResolvePoints(PipeSectionType pipeType)
    {
        var levelOverride = level.ScoringOverrides
            .FirstOrDefault(o => o.PipeSectionType == pipeType);

        if (levelOverride is not null)
        {
            return (levelOverride.BasePoints, levelOverride.SecondaryTraversalBonusPoints);
        }

        var defaultDef = DefaultPipeDefinitions.First(d => d.PipeSectionType == pipeType);
        return (defaultDef.BasePoints, defaultDef.SecondaryTraversalBonusPoints);
    }
}
