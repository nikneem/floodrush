using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Scores.Data;
using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Server.Scores.Features.GetTopScores;

internal sealed class GetTopScoresQueryHandler(IScoresRepository repository)
    : IQueryHandler<GetTopScoresQuery, TopScoresResponse>
{
    public async ValueTask<TopScoresResponse> HandleAsync(
        GetTopScoresQuery query,
        CancellationToken cancellationToken)
    {
        var scores = await repository.GetTopScoresAsync(query.LevelId, query.Take, cancellationToken);
        return new TopScoresResponse(query.LevelId, scores);
    }
}
