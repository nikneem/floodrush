using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Shared.Contracts.Scores;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.Services;

public sealed class ScoresApiService : IScoresApiService
{
    private readonly IApiBaseUrlProvider apiBaseUrlProvider;
    private readonly IDeviceAuthenticationService deviceAuthenticationService;
    private readonly ILogger<ScoresApiService> logger;

    public ScoresApiService(
        IApiBaseUrlProvider apiBaseUrlProvider,
        IDeviceAuthenticationService deviceAuthenticationService,
        ILogger<ScoresApiService> logger)
    {
        this.apiBaseUrlProvider = apiBaseUrlProvider;
        this.deviceAuthenticationService = deviceAuthenticationService;
        this.logger = logger;
    }

    public async Task<LevelScoreDto?> SubmitScoreAsync(
        SubmitScoreRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity(
            "scores-api.submit-score", ActivityKind.Client);
        activity?.SetTag("level.id", request.LevelId);
        activity?.SetTag("level.revision", request.LevelRevision);
        activity?.SetTag("score.points", request.Points);

        FloodRushTelemetry.ApiRequests.Add(1, new TagList
        {
            { "endpoint", "submit-score" }
        });

        logger.LogInformation(
            "Submitting score {Points} for level {LevelId} revision {Revision}.",
            request.Points, request.LevelId, request.LevelRevision);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = await CreateAuthenticatedClientAsync(cancellationToken);
            using var response = await client.PostAsJsonAsync("api/scores", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LevelScoreDto>(
                cancellationToken: cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation(
                "Score {Points} for level {LevelId} submitted successfully.", request.Points, request.LevelId);

            return result;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // Offline or network error – the score was not persisted server-side.
            // The UI shows points locally regardless; don't crash the happy path.
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogWarning(exception,
                "Score submission for level {LevelId} failed (network unavailable or server error).",
                request.LevelId);
            return null;
        }
        finally
        {
            FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
            {
                { "operation", "submit-score-api" }
            });
        }
    }

    public async Task<LevelScoreDto?> GetPlayerBestScoreAsync(
        string levelId,
        CancellationToken cancellationToken = default)
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity(
            "scores-api.get-player-best", ActivityKind.Client);
        activity?.SetTag("level.id", levelId);

        logger.LogDebug("Fetching player best score for level {LevelId}.", levelId);

        try
        {
            using var client = await CreateAuthenticatedClientAsync(cancellationToken);
            using var response = await client.GetAsync($"api/scores/my/{Uri.EscapeDataString(levelId)}", cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<LevelScoreDto>(cancellationToken: cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogDebug("Player best score for level {LevelId}: {Points} pts.", levelId, result?.Points);
            return result;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogDebug(exception, "Could not fetch player best score for level {LevelId} (network unavailable).", levelId);
            return null;
        }
    }

    public async Task<TopScoresResponse?> GetTopScoresAsync(
        string levelId,
        int take = 1,
        CancellationToken cancellationToken = default)
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity(
            "scores-api.get-top-scores", ActivityKind.Client);
        activity?.SetTag("level.id", levelId);
        activity?.SetTag("scores.take", take);

        logger.LogDebug("Fetching top {Take} scores for level {LevelId}.", take, levelId);

        try
        {
            using var client = new HttpClient(apiBaseUrlProvider.CreateHandler())
            {
                BaseAddress = apiBaseUrlProvider.GetBaseUri()
            };
            using var response = await client.GetAsync(
                $"api/scores/top/{Uri.EscapeDataString(levelId)}?take={take}",
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TopScoresResponse>(cancellationToken: cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogDebug("Global top score for level {LevelId}: {Points} pts.", levelId, result?.Scores.FirstOrDefault()?.Points);
            return result;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogDebug(exception, "Could not fetch top scores for level {LevelId} (network unavailable).", levelId);
            return null;
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken cancellationToken)
    {
        var client = new HttpClient(apiBaseUrlProvider.CreateHandler())
        {
            BaseAddress = apiBaseUrlProvider.GetBaseUri()
        };

        var accessToken = await deviceAuthenticationService.GetAccessTokenAsync(cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
