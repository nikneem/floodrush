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

    /// <summary>
    /// Fetches the authenticated player's personal best score for a level.
    /// Returns <c>null</c> when the player has no score yet, or on network failure.
    /// </summary>
    Task<LevelScoreDto?> GetPlayerBestScoreAsync(
        string levelId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the global top scores for a level (top 1 by default).
    /// Returns <c>null</c> on network failure.
    /// </summary>
    Task<TopScoresResponse?> GetTopScoresAsync(
        string levelId,
        int take = 1,
        CancellationToken cancellationToken = default);
}
