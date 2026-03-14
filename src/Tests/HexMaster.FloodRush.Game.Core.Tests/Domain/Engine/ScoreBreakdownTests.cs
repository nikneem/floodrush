using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Engine;
using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.Core.Tests.Domain.Engine;

public sealed class ScoreBreakdownTests
{
    [Fact]
    public void AddTraversal_AccumulatesPipeScore()
    {
        var score = new ScoreBreakdown();
        score.AddTraversal(new TraversalRecord(new GridPosition(0, 0), FlowTraversalAxis.Horizontal, 10));
        score.AddTraversal(new TraversalRecord(new GridPosition(1, 0), FlowTraversalAxis.Horizontal, 12));

        Assert.Equal(22, score.PipeScore);
        Assert.Equal(2, score.Traversals.Count);
    }

    [Fact]
    public void AddBasinBonus_AccumulatesBonus()
    {
        var score = new ScoreBreakdown();
        score.AddBasinBonus(50);
        score.AddBasinBonus(30);
        Assert.Equal(80, score.BasinBonus);
    }

    [Fact]
    public void AddSplitBonus_AccumulatesBonus()
    {
        var score = new ScoreBreakdown();
        score.AddSplitBonus(40);
        Assert.Equal(40, score.SplitBonus);
    }

    [Fact]
    public void SetCompletionBonus_SetsBonus()
    {
        var score = new ScoreBreakdown();
        score.SetCompletionBonus(1000);
        Assert.Equal(1000, score.CompletionBonus);
    }

    [Fact]
    public void Total_SumsAllComponents()
    {
        var score = new ScoreBreakdown();
        score.AddTraversal(new TraversalRecord(new GridPosition(0, 0), FlowTraversalAxis.Horizontal, 10));
        score.AddBasinBonus(50);
        score.AddSplitBonus(30);
        score.SetCompletionBonus(1000);

        Assert.Equal(1090, score.Total);
    }

    [Fact]
    public void InitialState_AllZero()
    {
        var score = new ScoreBreakdown();
        Assert.Equal(0, score.Total);
        Assert.Empty(score.Traversals);
    }

    // ── Unused pipe penalty ──────────────────────────────────────────────────────

    [Fact]
    public void SetUnusedPipePenalty_Zero_NoEffect()
    {
        var score = new ScoreBreakdown();
        score.SetCompletionBonus(1000);
        score.SetUnusedPipePenalty(0);

        Assert.Equal(0, score.UnusedPipePenalty);
        Assert.Equal(1000, score.Total);
    }

    [Fact]
    public void SetUnusedPipePenalty_AppliesPenalty()
    {
        var score = new ScoreBreakdown();
        score.AddTraversal(new TraversalRecord(new GridPosition(0, 0), FlowTraversalAxis.Horizontal, 10));
        score.SetCompletionBonus(1000);
        score.SetUnusedPipePenalty(-6); // 3 unused pipes × –2

        Assert.Equal(-6, score.UnusedPipePenalty);
        Assert.Equal(1004, score.Total); // 10 + 1000 – 6
    }

    [Fact]
    public void Total_ClampedToZero_WhenPenaltyExceedsScore()
    {
        var score = new ScoreBreakdown();
        score.AddTraversal(new TraversalRecord(new GridPosition(0, 0), FlowTraversalAxis.Horizontal, 10));
        score.SetUnusedPipePenalty(-100); // large penalty

        Assert.Equal(0, score.Total); // clamped, never negative
    }

    [Fact]
    public void SetUnusedPipePenalty_IgnoresPositiveValue()
    {
        // penalty must always be ≤ 0; passing a positive value is silently clamped.
        var score = new ScoreBreakdown();
        score.SetUnusedPipePenalty(50);

        Assert.Equal(0, score.UnusedPipePenalty);
    }
}
