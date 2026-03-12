using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Server.Scores.Features.GetTopScores;

public sealed record GetTopScoresQuery(string LevelId, int Take) : IQuery<TopScoresResponse>;
