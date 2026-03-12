using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Server.Scores.Features.SubmitScore;

public sealed record SubmitScoreCommand(string ProfileId, SubmitScoreRequest Request) : ICommand<LevelScoreDto>;
