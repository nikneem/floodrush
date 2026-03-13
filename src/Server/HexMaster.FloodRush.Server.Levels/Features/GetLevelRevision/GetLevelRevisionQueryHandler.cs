using HexMaster.FloodRush.Server.Abstractions.Features;
using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Features.GetLevelRevision;

internal sealed class GetLevelRevisionQueryHandler(ILevelsRepository repository)
    : IQueryHandler<GetLevelRevisionQuery, LevelRevisionDto?>
{
    public ValueTask<LevelRevisionDto?> HandleAsync(
        GetLevelRevisionQuery query,
        CancellationToken cancellationToken) =>
        repository.GetLevelRevisionAsync(query.ProfileId, query.LevelId, query.Revision, cancellationToken);
}
