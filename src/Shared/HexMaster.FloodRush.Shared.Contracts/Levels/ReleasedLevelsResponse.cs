namespace HexMaster.FloodRush.Shared.Contracts.Levels;

public sealed record ReleasedLevelsResponse(IReadOnlyCollection<ReleasedLevelSummaryDto> Levels);
