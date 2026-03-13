namespace HexMaster.FloodRush.Game.Core.Presentation.Gameplay;

public enum PreparationCountdownUrgency
{
    Normal = 0,
    Warning = 1,
    Critical = 2
}

public static class PreparationCountdownPresentation
{
    public const int WarningThresholdSeconds = 20;
    public const int CriticalThresholdSeconds = 10;
    public const double HiddenBlinkOpacity = 0.35d;
    public const double VisibleBlinkOpacity = 1d;

    public static PreparationCountdownUrgency ResolveUrgency(int remainingSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(remainingSeconds);

        if (remainingSeconds <= CriticalThresholdSeconds)
        {
            return PreparationCountdownUrgency.Critical;
        }

        if (remainingSeconds <= WarningThresholdSeconds)
        {
            return PreparationCountdownUrgency.Warning;
        }

        return PreparationCountdownUrgency.Normal;
    }

    public static bool ShouldBlink(int remainingSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(remainingSeconds);
        return remainingSeconds <= CriticalThresholdSeconds;
    }

    public static double ResolveOpacity(int remainingSeconds, bool isBlinkPhaseVisible)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(remainingSeconds);

        return ShouldBlink(remainingSeconds) && !isBlinkPhaseVisible
            ? HiddenBlinkOpacity
            : VisibleBlinkOpacity;
    }
}
