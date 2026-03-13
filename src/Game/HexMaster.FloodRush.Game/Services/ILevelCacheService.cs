using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Game.Services;

public interface ILevelCacheService
{
    Task<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(CancellationToken cancellationToken = default);
    Task SaveReleasedLevelsAsync(IReadOnlyCollection<ReleasedLevelSummaryDto> releasedLevels, CancellationToken cancellationToken = default);
    Task<LevelRevisionDto?> GetLevelRevisionAsync(string levelId, string revision, CancellationToken cancellationToken = default);
    Task SaveLevelRevisionAsync(LevelRevisionDto levelRevision, CancellationToken cancellationToken = default);
}
