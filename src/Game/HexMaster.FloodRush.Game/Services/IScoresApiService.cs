using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Game.Services;

public interface IScoresApiService
{
    /// <summary>
    /// Submits a completed-level score to the server.
    /// Returns the persisted <see cref="LevelScoreDto"/> on success, or
    /// <c>null</c> when the server is unreachable or the request fails.
    /// </summary>
    Task<LevelScoreDto?> SubmitScoreAsync(
        SubmitScoreRequest request,
        CancellationToken cancellationToken = default);
}
