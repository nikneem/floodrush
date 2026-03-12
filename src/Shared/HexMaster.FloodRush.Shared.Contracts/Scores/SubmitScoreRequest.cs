namespace HexMaster.FloodRush.Shared.Contracts.Scores;

public sealed record SubmitScoreRequest(
    string LevelId,
    string LevelRevision,
    int Points,
    DateTimeOffset AchievedAtUtc);
