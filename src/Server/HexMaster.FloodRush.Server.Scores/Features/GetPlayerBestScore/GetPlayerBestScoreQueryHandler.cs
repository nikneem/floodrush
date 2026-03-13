using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Scores.Data;
using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Server.Scores.Features.GetPlayerBestScore;

internal sealed class GetPlayerBestScoreQueryHandler(IScoresRepository repository)
    : IQueryHandler<GetPlayerBestScoreQuery, LevelScoreDto?>
{
    public async ValueTask<LevelScoreDto?> HandleAsync(
        GetPlayerBestScoreQuery query,
        CancellationToken cancellationToken)
    {
        return await repository.GetPlayerBestScoreAsync(query.ProfileId, query.LevelId, cancellationToken);
    }
}
