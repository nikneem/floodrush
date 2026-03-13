namespace HexMaster.FloodRush.Shared.Contracts.Levels;

public sealed record ReleasedLevelSummaryDto(
    string LevelId,
    string Revision,
    string DisplayName,
    string Difficulty,
    int FlowSpeedIndicator,
    DateTimeOffset ReleasedAtUtc);
