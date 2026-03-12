using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Features.GetReleasedLevels;

internal sealed class GetReleasedLevelsQueryHandler(ILevelsRepository repository)
    : IQueryHandler<GetReleasedLevelsQuery, ReleasedLevelsResponse>
{
    public async ValueTask<ReleasedLevelsResponse> HandleAsync(
        GetReleasedLevelsQuery query,
        CancellationToken cancellationToken)
    {
        var levels = await repository.GetReleasedLevelsAsync(query.ProfileId, cancellationToken);
        return new ReleasedLevelsResponse(levels);
    }
}
