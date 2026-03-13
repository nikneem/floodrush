using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.Services;

public sealed class LevelsApiService : ILevelsApiService
{
    private readonly IApiBaseUrlProvider apiBaseUrlProvider;
    private readonly IDeviceAuthenticationService deviceAuthenticationService;
    private readonly ILogger<LevelsApiService> logger;

    public LevelsApiService(
        IApiBaseUrlProvider apiBaseUrlProvider,
        IDeviceAuthenticationService deviceAuthenticationService,
        ILogger<LevelsApiService> logger)
    {
        this.apiBaseUrlProvider = apiBaseUrlProvider;
        this.deviceAuthenticationService = deviceAuthenticationService;
        this.logger = logger;
    }

    public async Task<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
        CancellationToken cancellationToken = default)
    {
        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("levels-api.get-released-levels", ActivityKind.Client);
        activity?.SetTag("http.route", "/api/levels/released");

        FloodRushTelemetry.ApiRequests.Add(1, new TagList
        {
            { "endpoint", "released-levels" }
        });

        logger.LogInformation("Fetching released levels from the FloodRush API.");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = await CreateAuthenticatedClientAsync(cancellationToken);
            using var response = await client.GetAsync("api/levels/released", cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ReleasedLevelsResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The released levels endpoint returned an empty response.");

            var releasedLevels = payload.Levels?.ToArray()
                ?? throw new InvalidOperationException("The released levels endpoint did not include a levels collection.");

            activity?.SetTag("levels.count", releasedLevels.Length);
            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Fetched {Count} released levels from the FloodRush API.", releasedLevels.Length);
            return releasedLevels;
        }
        catch (JsonException exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogWarning(exception, "Released levels payload could not be deserialized.");
            throw new InvalidOperationException("The released levels response from the FloodRush API was invalid.", exception);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogWarning(exception, "Fetching released levels failed.");
            throw;
        }
        finally
        {
            FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
            {
                { "operation", "released-levels-api" }
            });
        }
    }

    public async Task<LevelRevisionDto> GetLevelRevisionAsync(
        string levelId,
        string revision,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);

        using var activity = FloodRushTelemetry.ActivitySource.StartActivity("levels-api.get-level-revision", ActivityKind.Client);
        activity?.SetTag("level.id", levelId);
        activity?.SetTag("level.revision", revision);

        FloodRushTelemetry.ApiRequests.Add(1, new TagList
        {
            { "endpoint", "level-revision" }
        });

        logger.LogInformation("Fetching level revision {LevelId}/{Revision} from the FloodRush API.", levelId, revision);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var client = await CreateAuthenticatedClientAsync(cancellationToken);
            using var response = await client.GetAsync(
                $"api/levels/{Uri.EscapeDataString(levelId)}/revisions/{Uri.EscapeDataString(revision)}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var levelRevision = await response.Content.ReadFromJsonAsync<LevelRevisionDto>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException($"The level revision '{levelId}/{revision}' returned an empty response.");

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Fetched level revision {LevelId}/{Revision}.", levelId, revision);
            return levelRevision;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            logger.LogWarning(exception, "Fetching level revision {LevelId}/{Revision} failed.", levelId, revision);
            throw;
        }
        finally
        {
            FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
            {
                { "operation", "level-revision-api" }
            });
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(CancellationToken cancellationToken)
    {
        var client = new HttpClient
        {
            BaseAddress = apiBaseUrlProvider.GetBaseUri()
        };

        var accessToken = await deviceAuthenticationService.GetAccessTokenAsync(cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
