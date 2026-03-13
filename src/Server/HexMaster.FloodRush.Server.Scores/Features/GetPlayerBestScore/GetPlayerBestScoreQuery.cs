using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Server.Scores.Features.GetPlayerBestScore;

public sealed record GetPlayerBestScoreQuery(string ProfileId, string LevelId) : IQuery<LevelScoreDto?>;
