using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Game.Services;

public interface ILevelsApiService
{
    Task<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(CancellationToken cancellationToken = default);
    Task<LevelRevisionDto> GetLevelRevisionAsync(string levelId, string revision, CancellationToken cancellationToken = default);
}
