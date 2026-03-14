using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Engine;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Pipes;
using HexMaster.FloodRush.Game.Core.Domain.Rules;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Engine;

/// <summary>
/// End-to-end simulation tests for the game engine.
///
/// Transit formula: (101 - speedValue) * 10 ms per tile at 100% speed modifier.
/// At speed=50: (101-50)*10 = 510 ms per tile.
/// At speed=100: 10 ms per tile.
/// </summary>
public sealed class GameSessionTests
{
    // ── Phase management ────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsLevelLoadedPhase()
    {
        var session = CreateSession(CreateStraightLevel());
        Assert.Equal(GamePhase.LevelLoaded, session.Phase);
    }

    [Fact]
    public void StartPlacementPhase_TransitionsToPlacementWindow()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        Assert.Equal(GamePhase.PlacementWindow, session.Phase);
    }

    [Fact]
    public void StartPlacementPhase_ThrowsWhenNotInLevelLoaded()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        Assert.Throws<InvalidOperationException>(() => session.StartPlacementPhase());
    }

    [Fact]
    public void StartFlow_TransitionsToFlowActive()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        PlaceStraightPipes(session);
        session.StartFlow();
        Assert.Equal(GamePhase.FlowActive, session.Phase);
    }

    [Fact]
    public void StartFlow_ThrowsWhenNotInPlacementWindow()
    {
        var session = CreateSession(CreateStraightLevel());
        Assert.Throws<InvalidOperationException>(() => session.StartFlow());
    }

    [Fact]
    public void Tick_DoesNothingWhenNotFlowActive()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        PlaceStraightPipes(session);
        // Don't call StartFlow — Tick should be a no-op
        session.Tick(99999);
        Assert.Equal(GamePhase.PlacementWindow, session.Phase);
    }

    // ── Pipe placement ──────────────────────────────────────────────────────

    [Fact]
    public void PlacePipe_ThrowsWhenNotInPlacementWindow()
    {
        var session = CreateSession(CreateStraightLevel());
        Assert.Throws<InvalidOperationException>(() =>
            session.PlacePipe(new GridPosition(1, 0), PipeSectionType.Horizontal));
    }

    [Fact]
    public void PlacePipe_ThrowsWhenPositionOutsideBoard()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        Assert.Throws<InvalidOperationException>(() =>
            session.PlacePipe(new GridPosition(99, 0), PipeSectionType.Horizontal));
    }

    [Fact]
    public void PlacePipe_ThrowsWhenPositionHasFixedTile()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        Assert.Throws<InvalidOperationException>(() =>
            session.PlacePipe(new GridPosition(0, 0), PipeSectionType.Horizontal)); // start tile
    }

    [Fact]
    public void PlacePipe_AllowsOverwritingExistingPipe()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        session.PlacePipe(new GridPosition(1, 0), PipeSectionType.Horizontal);
        session.PlacePipe(new GridPosition(1, 0), PipeSectionType.Vertical); // overwrite

        var pipe = session.Board.GetPlacedPipe(new GridPosition(1, 0));
        Assert.NotNull(pipe);
        Assert.Equal(PipeSectionType.Vertical, pipe!.PipeType);
    }

    [Fact]
    public void RemovePipe_RemovesExistingPipe()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        session.PlacePipe(new GridPosition(1, 0), PipeSectionType.Horizontal);
        session.RemovePipe(new GridPosition(1, 0));
        Assert.Null(session.Board.GetPlacedPipe(new GridPosition(1, 0)));
    }

    [Fact]
    public void RemovePipe_ThrowsWhenNotInPlacementWindow()
    {
        var session = CreateSession(CreateStraightLevel());
        Assert.Throws<InvalidOperationException>(() =>
            session.RemovePipe(new GridPosition(1, 0)));
    }

    // ── Simulation: success ─────────────────────────────────────────────────

    [Fact]
    public void Tick_StraightLevel_Succeeds()
    {
        // 4×1 level: Start(0,0,Right) → H(1,0) → H(2,0) → Finish(3,0,Left)
        // Speed=100 → 10 ms per tile; 3 transitions needed
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        PlaceStraightPipes(session);
        session.StartFlow();

        // Should succeed after 3 × 10 ms = 30 ms
        session.Tick(30);

        Assert.Equal(GamePhase.Succeeded, session.Phase);
    }

    [Fact]
    public void Tick_Succeeds_AwardsCompletionBonus()
    {
        var session = CreateSession(CreateStraightLevel(), completionBonus: 500);
        session.StartPlacementPhase();
        PlaceStraightPipes(session);
        session.StartFlow();
        session.Tick(30);

        Assert.Equal(GamePhase.Succeeded, session.Phase);
        Assert.Equal(500, session.Score.CompletionBonus);
    }

    [Fact]
    public void Tick_Succeeds_PipePointsAccurate()
    {
        // Default Horizontal = 10 pts; 2 horizontal pipes traversed
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        PlaceStraightPipes(session);
        session.StartFlow();
        session.Tick(30);

        Assert.Equal(20, session.Score.PipeScore);  // 2 pipes × 10 pts
    }

    [Fact]
    public void Tick_MultipleSmallTicks_ProducesSameResultAsOneLargeTick()
    {
        // Single large tick
        var s1 = CreateSession(CreateStraightLevel());
        s1.StartPlacementPhase();
        PlaceStraightPipes(s1);
        s1.StartFlow();
        s1.Tick(30);

        // Multiple small ticks
        var s2 = CreateSession(CreateStraightLevel());
        s2.StartPlacementPhase();
        PlaceStraightPipes(s2);
        s2.StartFlow();
        s2.Tick(10);
        s2.Tick(10);
        s2.Tick(10);

        Assert.Equal(s1.Phase, s2.Phase);
        Assert.Equal(s1.Score.Total, s2.Score.Total);
    }

    // ── Simulation: failure ─────────────────────────────────────────────────

    [Fact]
    public void Tick_DeadEnd_FailsWhenNoPipeAtNextCell()
    {
        // No pipes placed → flow immediately hits empty cell
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        session.StartFlow();
        session.Tick(10);

        Assert.Equal(GamePhase.Failed, session.Phase);
    }

    [Fact]
    public void Tick_WrongDirection_FailsWhenPipeCannotAcceptFlow()
    {
        // Place a Vertical pipe where Horizontal is needed
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        session.PlacePipe(new GridPosition(1, 0), PipeSectionType.Vertical);
        session.StartFlow();
        session.Tick(10);

        Assert.Equal(GamePhase.Failed, session.Phase);
    }

    [Fact]
    public void Tick_FlowExitsBoard_Fails()
    {
        // Place pipes that route flow off the bottom edge of the board
        // Level: 4×1, Start(0,0,Right). Place Corner LeftToBottom at (1,0) → exits bottom
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        session.PlacePipe(new GridPosition(1, 0), PipeSectionType.CornerLeftToBottom);
        session.StartFlow();
        session.Tick(20); // Start transit + corner transit

        Assert.Equal(GamePhase.Failed, session.Phase);
    }

    // ── Simulation: start delay ─────────────────────────────────────────────

    [Fact]
    public void Tick_WithStartDelay_DoesNotAdvanceUntilDelayExpires()
    {
        // Level with 500ms start delay, speed=100
        var level = new LevelDefinition(
            "delay-test",
            "Delay Test",
            new BoardDimensions(4, 1),
            500, // 500ms start delay
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ]);

        var session = CreateSession(level);
        session.StartPlacementPhase();
        PlaceStraightPipes(session);
        session.StartFlow();

        // Tick 499ms — still in start delay, not yet succeeded
        session.Tick(499);
        Assert.Equal(GamePhase.FlowActive, session.Phase);
        Assert.Empty(session.Score.Traversals);

        // Tick 1ms to expire delay + 30ms to advance flow
        session.Tick(31);
        Assert.Equal(GamePhase.Succeeded, session.Phase);
    }

    // ── Simulation: fluid basin ─────────────────────────────────────────────

    [Fact]
    public void Tick_FluidBasin_PausesFlowWithTripleTransitTime()
    {
        // Layout: Start(0,0,R) → Basin(1,0,L entry,R exit,0ms fill,20pts) → H(2,0) → Finish(3,0,L)
        // Speed=100 → 10ms transit; basin takes 3× transit = 30ms
        var level = new LevelDefinition(
            "basin-test",
            "Basin Test",
            new BoardDimensions(4, 1),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FluidBasinTile(new GridPosition(1, 0), BoardDirection.Left, BoardDirection.Right, 0, 20),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ]);

        var session = CreateSession(level);
        session.StartPlacementPhase();
        session.PlacePipe(new GridPosition(2, 0), PipeSectionType.Horizontal);
        session.StartFlow();

        // After 10ms: start tile transit done, enters basin. Basin bonus IS credited on entry.
        // RequiredMs becomes 10ms × 3 = 30ms (3× transit rule).
        session.Tick(10);
        Assert.Equal(GamePhase.FlowActive, session.Phase);
        Assert.Equal(20, session.Score.BasinBonus); // credited when basin is entered

        session.Tick(30); // basin 3× transit
        session.Tick(10);  // final pipe + finish
        Assert.Equal(GamePhase.Succeeded, session.Phase);
        Assert.Equal(20, session.Score.BasinBonus);
    }

    [Fact]
    public void Tick_MandatoryBasin_FailsWhenNotTraversed()
    {
        // Layout: Start(0,0,R) → mandatory Basin(2,0) but player routes past it → H(1,0) → Finish(3,0,L)
        // Basin at (2,0) is mandatory but the player's pipe skips it.
        var level = new LevelDefinition(
            "mandatory-basin-test",
            "Mandatory Basin Test",
            new BoardDimensions(4, 1),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FluidBasinTile(new GridPosition(2, 0), BoardDirection.Left, BoardDirection.Right, 0, 50, isMandatory: true),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ]);

        // Note: pipe placed at (1,0) bridges Start→Finish via (1,0)→(2,0 mandatory basin)→(3,0 finish)
        // To skip the basin we'd need a different layout, but here the only path goes through (2,0).
        // Let's test the mandatory-basin-not-traversed fail by routing to finish WITHOUT the basin:
        // We use a 5-wide board and skip the basin column.
        var level2 = new LevelDefinition(
            "mandatory-basin-skip-test",
            "Mandatory Basin Skip Test",
            new BoardDimensions(6, 3),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 1), BoardDirection.Right),
                new FluidBasinTile(new GridPosition(3, 0), BoardDirection.Left, BoardDirection.Right, 0, 50, isMandatory: true),
                new FinishPointTile(new GridPosition(5, 1), BoardDirection.Left)
            ]);

        var session = CreateSession(level2);
        session.StartPlacementPhase();
        // Route straight across row 1, bypassing the mandatory basin at row 0
        session.PlacePipe(new GridPosition(1, 1), PipeSectionType.Horizontal);
        session.PlacePipe(new GridPosition(2, 1), PipeSectionType.Horizontal);
        session.PlacePipe(new GridPosition(3, 1), PipeSectionType.Horizontal);
        session.PlacePipe(new GridPosition(4, 1), PipeSectionType.Horizontal);
        session.StartFlow();

        // Advance until flow reaches finish — the mandatory basin was bypassed
        for (var i = 0; i < 60; i++) session.Tick(10);

        Assert.Equal(GamePhase.Failed, session.Phase);
    }

    [Fact]
    public void Tick_MandatoryBasin_SucceedsWhenTraversed()
    {
        // Layout on 6×3 board:
        // Start(0,1,R) → (1,1) CornerLeftToTop → (1,0) CornerRightToBottom → (2,0) H
        //   → Basin(3,0, entry L, exit R) → (4,0) CornerLeftToBottom → (4,1) CornerRightToTop → Finish(5,1,L)
        var level = new LevelDefinition(
            "mandatory-basin-traverse-test",
            "Mandatory Basin Traverse Test",
            new BoardDimensions(6, 3),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 1), BoardDirection.Right),
                new FluidBasinTile(new GridPosition(3, 0), BoardDirection.Left, BoardDirection.Right, 0, 50, isMandatory: true),
                new FinishPointTile(new GridPosition(5, 1), BoardDirection.Left)
            ]);

        var session = CreateSession(level);
        session.StartPlacementPhase();
        session.PlacePipe(new GridPosition(1, 1), PipeSectionType.CornerLeftToTop);    // Left→Top
        session.PlacePipe(new GridPosition(1, 0), PipeSectionType.CornerRightToBottom); // Bottom→Right
        session.PlacePipe(new GridPosition(2, 0), PipeSectionType.Horizontal);
        // basin at (3,0) is fixed
        session.PlacePipe(new GridPosition(4, 0), PipeSectionType.CornerLeftToBottom); // Left→Bottom
        session.PlacePipe(new GridPosition(4, 1), PipeSectionType.CornerRightToTop);   // Top→Right
        session.StartFlow();

        // Advance generously – basin takes 30ms; total path ~7 tiles at 10ms each
        for (var i = 0; i < 200; i++) session.Tick(10);

        Assert.Equal(GamePhase.Succeeded, session.Phase);
        Assert.Equal(50, session.Score.BasinBonus);
    }

    // ── Simulation: split section ───────────────────────────────────────────

    [Fact]
    public void Tick_SplitSection_CreatesTwoBranches()
    {
        // 5×2 board:
        // Start(0,0,R) → Split(1,0: entry=L, exits=R+Bottom, 100%, 30pts) → Finish1(4,0,L)
        //                                                                   ↓ H(1,1) → Finish2(2,1,L)
        var level = new LevelDefinition(
            "split-test",
            "Split Test",
            new BoardDimensions(5, 2),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new SplitSectionTile(new GridPosition(1, 0), BoardDirection.Left, BoardDirection.Right, BoardDirection.Bottom, 100, 30),
                new FinishPointTile(new GridPosition(4, 0), BoardDirection.Left),
                new FinishPointTile(new GridPosition(2, 1), BoardDirection.Left),
            ]);

        var session = CreateSession(level);
        session.StartPlacementPhase();
        // Right branch: (2,0)→(3,0) = 2 horizontal pipes
        session.PlacePipe(new GridPosition(2, 0), PipeSectionType.Horizontal);
        session.PlacePipe(new GridPosition(3, 0), PipeSectionType.Horizontal);
        // Down branch: (1,1) = corner that turns Bottom→Right so flow can reach (2,1,Left entry)
        session.PlacePipe(new GridPosition(1, 1), PipeSectionType.CornerRightToTop);
        session.StartFlow();

        // Speed=100 → 10ms per tile.
        // Steps to succeed:
        //   10ms: Start → Split (split bonus awarded, two new branches created)
        //   10ms: Branch1: Split(1,0) → Pipe(2,0); Branch2: Split(1,0) → Pipe(1,1)
        //   10ms: Branch1: Pipe(2,0) → Pipe(3,0); Branch2: Pipe(1,1) → Finish(2,1) ← Branch2 done
        //   10ms: Branch1: Pipe(3,0) → Finish(4,0) ← Branch1 done
        session.Tick(10); // split entered
        Assert.Equal(30, session.Score.SplitBonus);

        session.Tick(30); // both branches advance and complete
        Assert.Equal(GamePhase.Succeeded, session.Phase);
        Assert.Equal(2, session.ReachedFinishPoints.Count);
    }

    // ── Simulation: cross section ───────────────────────────────────────────

    [Fact]
    public void Tick_CrossSection_AwardsBasePtsFirstTraversal()
    {
        // 4×1: Start(0,0,R) → Cross(1,0) → Cross → Finish
        // Simple: only horizontal traversal
        var session = CreateSession(CreateStraightLevel(crossAtPosition1: true));
        session.StartPlacementPhase();
        PlaceStraightPipes(session, crossAtPosition1: true);
        session.StartFlow();
        session.Tick(30);

        Assert.Equal(GamePhase.Succeeded, session.Phase);
        // Position 1: Cross base points = 10; Position 2: Horizontal base points = 10
        Assert.Equal(20, session.Score.PipeScore);
    }

    [Fact]
    public void Tick_CrossSection_SecondTraversalAwardsBonus()
    {
        // 3×3 board; flow enters cross horizontally then vertically
        // (0,1,R) → Cross(1,1) → (2,1,L) finish
        //  AND
        // (1,0,B) → Cross(1,1) → (1,2,T) finish
        var level = new LevelDefinition(
            "cross-double",
            "Cross Double",
            new BoardDimensions(3, 3),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 1), BoardDirection.Right),
                new StartPointTile(new GridPosition(1, 0), BoardDirection.Bottom),
                new FinishPointTile(new GridPosition(2, 1), BoardDirection.Left),
                new FinishPointTile(new GridPosition(1, 2), BoardDirection.Top),
            ]);

        var session = CreateSession(level);
        session.StartPlacementPhase();
        session.PlacePipe(new GridPosition(1, 1), PipeSectionType.Cross);
        session.StartFlow();

        // Speed=100 → 10ms per tile
        // Step 1 (10ms): Both starts transit; branches at (0,1) and (1,0) ready to move
        // Step 2 (10ms): Both enter Cross(1,1) - first branch awards base points,
        //                second awards secondary bonus
        // Step 3 (10ms): Both exit to finish points
        session.Tick(30);

        Assert.Equal(GamePhase.Succeeded, session.Phase);

        // One traversal should be base points (10) and one secondary bonus (50)
        var crossPoints = session.Score.Traversals.Select(t => t.PointsAwarded).OrderBy(p => p).ToArray();
        Assert.Contains(10, crossPoints);   // base points
        Assert.Contains(50, crossPoints);   // secondary bonus
    }

    // ── Simulation: wall section ─────────────────────────────────────────────

    [Fact]
    public void Tick_FlowIntoWall_MarksBranchFailed()
    {
        // 3×2 board: Start(0,1,R), Wall(1,0), Finish(2,1,L).
        // BFS succeeds: (0,1) → (1,1) → (2,1) finish, so the level is valid.
        // At runtime, player routes flow upward from (1,1) via CornerLeftToTop into the wall at (1,0).
        var level = new LevelDefinition(
            "wall-runtime-fail",
            "Wall Runtime Fail",
            new BoardDimensions(3, 2),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 1), BoardDirection.Right),
                new WallTile(new GridPosition(1, 0)),
                new FinishPointTile(new GridPosition(2, 1), BoardDirection.Left)
            ]);

        var session = CreateSession(level);
        session.StartPlacementPhase();
        // CornerLeftToTop at (1,1): flow enters from Left, exits upward into the wall
        session.PlacePipe(new GridPosition(1, 1), PipeSectionType.CornerLeftToTop);
        session.StartFlow();

        for (var i = 0; i < 50; i++) session.Tick(10);

        Assert.Equal(GamePhase.Failed, session.Phase);
    }

    [Fact]
    public void LevelDefinition_RejectsWallBlockingAllPaths()
    {
        // 3×1 board: Start(0,0,R) → Wall(1,0) → Finish(2,0,L). Single row, wall blocks the only path.
        Assert.Throws<InvalidOperationException>(() => new LevelDefinition(
            "wall-full-block",
            "Wall Full Block",
            new BoardDimensions(3, 1),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new WallTile(new GridPosition(1, 0)),
                new FinishPointTile(new GridPosition(2, 0), BoardDirection.Left)
            ]));
    }

    [Fact]
    public void LevelDefinition_AcceptsWallThatDoesNotBlockAllPaths()
    {
        // 3×2 board: Start(0,1,R), Wall(1,0), Finish(2,1,L).
        // Wall blocks the cell above (1,1) but the finish is still reachable through (1,1) → (2,1).
        var level = new LevelDefinition(
            "wall-partial-block",
            "Wall Partial Block",
            new BoardDimensions(3, 2),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 1), BoardDirection.Right),
                new WallTile(new GridPosition(1, 0)),
                new FinishPointTile(new GridPosition(2, 1), BoardDirection.Left)
            ]);

        Assert.NotNull(level);
    }

    [Fact]
    public void Tick_NegativeElapsedMs_Throws()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        PlaceStraightPipes(session);
        session.StartFlow();
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Tick(-1));
    }

    // ── Scoring: scoring overrides ──────────────────────────────────────────

    [Fact]
    public void PlacePipe_UsesScoringOverridesWhenPresent()
    {
        // Override horizontal pipe to 50 pts
        var level = new LevelDefinition(
            "override-test",
            "Override Test",
            new BoardDimensions(4, 1),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ],
            scoringOverrides:
            [
                new PipeScoringOverride(PipeSectionType.Horizontal, 50)
            ]);

        var session = CreateSession(level);
        session.StartPlacementPhase();
        session.PlacePipe(new GridPosition(1, 0), PipeSectionType.Horizontal);
        session.PlacePipe(new GridPosition(2, 0), PipeSectionType.Horizontal);
        session.StartFlow();
        session.Tick(30);

        Assert.Equal(GamePhase.Succeeded, session.Phase);
        Assert.Equal(100, session.Score.PipeScore); // 2 × 50 pts
    }

    // ── Tick idempotency after terminal phase ───────────────────────────────

    [Fact]
    public void Tick_AfterSucceeded_DoesNothing()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        PlaceStraightPipes(session);
        session.StartFlow();
        session.Tick(30);
        Assert.Equal(GamePhase.Succeeded, session.Phase);

        session.Tick(1000); // Should not throw or change phase
        Assert.Equal(GamePhase.Succeeded, session.Phase);
    }

    [Fact]
    public void Tick_AfterFailed_DoesNothing()
    {
        var session = CreateSession(CreateStraightLevel());
        session.StartPlacementPhase();
        session.StartFlow();
        session.Tick(10);
        Assert.Equal(GamePhase.Failed, session.Phase);

        session.Tick(1000);
        Assert.Equal(GamePhase.Failed, session.Phase);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static GameSession CreateSession(LevelDefinition level, int completionBonus = 1000) =>
        new(level, completionBonus);

    /// <summary>
    /// Straight 4×1 level: Start(0,0,Right) → [player pipes at 1,2] → Finish(3,0,Left).
    /// Speed=100 → 10ms per tile.
    /// </summary>
    private static LevelDefinition CreateStraightLevel(bool crossAtPosition1 = false) =>
        new(
            "straight",
            "Straight Level",
            new BoardDimensions(4, 1),
            0,
            new FlowSpeedIndicator(100),
            [
                new StartPointTile(new GridPosition(0, 0), BoardDirection.Right),
                new FinishPointTile(new GridPosition(3, 0), BoardDirection.Left)
            ]);

    private static void PlaceStraightPipes(GameSession session, bool crossAtPosition1 = false)
    {
        session.PlacePipe(new GridPosition(1, 0), crossAtPosition1 ? PipeSectionType.Cross : PipeSectionType.Horizontal);
        session.PlacePipe(new GridPosition(2, 0), PipeSectionType.Horizontal);
    }
}
