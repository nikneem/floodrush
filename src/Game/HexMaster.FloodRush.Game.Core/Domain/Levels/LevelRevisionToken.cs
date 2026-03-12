using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Levels;

public sealed class LevelRevisionToken : IEquatable<LevelRevisionToken>
{
    public string Value { get; private set; }

    public LevelRevisionToken(string value)
    {
        Value = Guard.AgainstNullOrWhiteSpace(value, nameof(value));
    }

    /// <summary>Creates a new unique revision token.</summary>
    public static LevelRevisionToken New() => new(Guid.NewGuid().ToString("N"));

    public bool Equals(LevelRevisionToken? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as LevelRevisionToken);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;
}
