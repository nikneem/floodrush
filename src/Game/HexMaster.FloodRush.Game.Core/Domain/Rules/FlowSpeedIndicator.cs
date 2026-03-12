using HexMaster.FloodRush.Game.Core.Domain.Common;

namespace HexMaster.FloodRush.Game.Core.Domain.Rules;

public sealed class FlowSpeedIndicator : IEquatable<FlowSpeedIndicator>
{
    public FlowSpeedIndicator(int value)
    {
        SetValue(value);
    }

    public int Value { get; private set; }

    public void SetValue(int value) =>
        Value = Guard.AgainstOutOfRange(value, 1, 100, nameof(value));

    public FlowSpeedIndicator Clone() => new(Value);

    public bool Equals(FlowSpeedIndicator? other) =>
        other is not null &&
        Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as FlowSpeedIndicator);

    public override int GetHashCode() => Value;
}
