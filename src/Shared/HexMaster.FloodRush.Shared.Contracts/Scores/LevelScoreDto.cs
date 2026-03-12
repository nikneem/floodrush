namespace HexMaster.FloodRush.Shared.Contracts.Scores;

public sealed record LevelScoreDto(
    string LevelId,
    string LevelRevision,
    string ProfileId,
    int Points,
    DateTimeOffset AchievedAtUtc);
