using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Server.Scores.Data;

internal interface IScoresRepository
{
    ValueTask<LevelScoreDto> SubmitScoreAsync(
        string profileId,
        SubmitScoreRequest request,
        CancellationToken cancellationToken);

    ValueTask<IReadOnlyCollection<LevelScoreDto>> GetTopScoresAsync(
        string levelId,
        int take,
        CancellationToken cancellationToken);

    ValueTask<LevelScoreDto?> GetPlayerBestScoreAsync(
        string profileId,
        string levelId,
        CancellationToken cancellationToken);
}
