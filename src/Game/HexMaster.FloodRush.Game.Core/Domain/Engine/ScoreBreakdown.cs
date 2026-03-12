using HexMaster.FloodRush.Game.Core.Domain.Pipes;

namespace HexMaster.FloodRush.Game.Core.Domain.Engine;

/// <summary>
/// Accumulates and exposes the score breakdown for a game session.
/// Mutation methods are internal; external callers use read-only properties.
/// </summary>
public sealed class ScoreBreakdown
{
    private readonly List<TraversalRecord> traversals = [];
    private int basinBonus;
    private int splitBonus;
    private int completionBonus;

    /// <summary>Sum of all points earned from traversing player-placed pipes.</summary>
    public int PipeScore => traversals.Sum(t => t.PointsAwarded);

    /// <summary>Total bonus points from fluid basin tiles.</summary>
    public int BasinBonus => basinBonus;

    /// <summary>Total bonus points from split section tiles.</summary>
    public int SplitBonus => splitBonus;

    /// <summary>Bonus awarded when all required finish points are reached.</summary>
    public int CompletionBonus => completionBonus;

    /// <summary>Grand total of all score components.</summary>
    public int Total => PipeScore + BasinBonus + SplitBonus + CompletionBonus;

    /// <summary>Individual pipe traversal records in traversal order.</summary>
    public IReadOnlyCollection<TraversalRecord> Traversals => traversals.AsReadOnly();

    internal void AddTraversal(TraversalRecord record) => traversals.Add(record);
    internal void AddBasinBonus(int points) => basinBonus += points;
    internal void AddSplitBonus(int points) => splitBonus += points;
    internal void SetCompletionBonus(int points) => completionBonus = points;
}
