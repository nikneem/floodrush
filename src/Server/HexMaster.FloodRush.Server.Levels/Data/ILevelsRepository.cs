using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Data;

internal interface ILevelsRepository
{
    ValueTask<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
        string profileId,
        CancellationToken cancellationToken);
}
