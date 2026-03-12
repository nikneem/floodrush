using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Pipes;

/// <summary>
/// Defines how many of a given pipe section type a player may place in a level.
/// A null MaxCount means the type is unlimited.
/// </summary>
public sealed class PipeInventoryRule : IEquatable<PipeInventoryRule>
{
    public PipeSectionType PipeSectionType { get; private set; }

    /// <summary>Maximum number of this pipe type the player may place. Null means unlimited.</summary>
    public int? MaxCount { get; private set; }

    public PipeInventoryRule(PipeSectionType pipeSectionType, int? maxCount)
    {
        SetPipeSectionType(pipeSectionType);
        SetMaxCount(maxCount);
    }

    public void SetPipeSectionType(PipeSectionType pipeSectionType)
    {
        if (!Enum.IsDefined(pipeSectionType))
        {
            throw new ArgumentOutOfRangeException(nameof(pipeSectionType), "Unknown pipe section type.");
        }

        PipeSectionType = pipeSectionType;
    }

    public void SetMaxCount(int? maxCount)
    {
        if (maxCount.HasValue)
        {
            Guard.AgainstOutOfRange(maxCount.Value, 1, int.MaxValue, nameof(maxCount));
        }

        MaxCount = maxCount;
    }

    public PipeInventoryRule Clone() => new(PipeSectionType, MaxCount);

    public bool Equals(PipeInventoryRule? other) =>
        other is not null &&
        PipeSectionType == other.PipeSectionType &&
        MaxCount == other.MaxCount;

    public override bool Equals(object? obj) => Equals(obj as PipeInventoryRule);

    public override int GetHashCode() => HashCode.Combine(PipeSectionType, MaxCount);
}
