using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Pipes;

/// <summary>
/// Per-level scoring override that replaces the default points for a specific pipe section type.
/// Only the Cross type may define a secondary traversal bonus.
/// </summary>
public sealed class PipeScoringOverride : IEquatable<PipeScoringOverride>
{
    public PipeSectionType PipeSectionType { get; private set; }

    public int BasePoints { get; private set; }

    /// <summary>Extra points awarded when the Cross section is traversed a second time. Always 0 for non-Cross types.</summary>
    public int SecondaryTraversalBonusPoints { get; private set; }

    public PipeScoringOverride(PipeSectionType pipeSectionType, int basePoints, int secondaryTraversalBonusPoints = 0)
    {
        if (!Enum.IsDefined(pipeSectionType))
        {
            throw new ArgumentOutOfRangeException(nameof(pipeSectionType), "Unknown pipe section type.");
        }

        if (pipeSectionType != PipeSectionType.Cross && secondaryTraversalBonusPoints != 0)
        {
            throw new ArgumentException(
                "Only the Cross section may define a secondary traversal bonus.",
                nameof(secondaryTraversalBonusPoints));
        }

        PipeSectionType = pipeSectionType;
        BasePoints = Guard.AgainstNegative(basePoints, nameof(basePoints));
        SecondaryTraversalBonusPoints = Guard.AgainstNegative(secondaryTraversalBonusPoints, nameof(secondaryTraversalBonusPoints));
    }

    public PipeScoringOverride Clone() => new(PipeSectionType, BasePoints, SecondaryTraversalBonusPoints);

    public bool Equals(PipeScoringOverride? other) =>
        other is not null &&
        PipeSectionType == other.PipeSectionType &&
        BasePoints == other.BasePoints &&
        SecondaryTraversalBonusPoints == other.SecondaryTraversalBonusPoints;

    public override bool Equals(object? obj) => Equals(obj as PipeScoringOverride);

    public override int GetHashCode() =>
        HashCode.Combine(PipeSectionType, BasePoints, SecondaryTraversalBonusPoints);
}
