namespace HexMaster.FloodRush.Shared.Contracts.Scores;

public sealed record TopScoresResponse(string LevelId, IReadOnlyCollection<LevelScoreDto> Scores);
