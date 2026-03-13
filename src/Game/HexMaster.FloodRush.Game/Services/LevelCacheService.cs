using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using HexMaster.FloodRush.Game.Diagnostics;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.Extensions.Logging;

namespace HexMaster.FloodRush.Game.Services;

public sealed class LevelCacheService : ILevelCacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly ILogger<LevelCacheService> logger;
    private readonly string cacheRootPath = Path.Combine(FileSystem.AppDataDirectory, "levels");
    private readonly string revisionsRootPath;
    private readonly string releasedLevelsPath;

    public LevelCacheService(ILogger<LevelCacheService> logger)
    {
        this.logger = logger;
        revisionsRootPath = Path.Combine(cacheRootPath, "revisions");
        releasedLevelsPath = Path.Combine(cacheRootPath, "released-levels.json");
    }

    public async Task<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (!File.Exists(releasedLevelsPath))
        {
            FloodRushTelemetry.CacheOperations.Add(1, new TagList
            {
                { "operation", "released-levels-read" },
                { "result", "miss" }
            });
            logger.LogInformation("No cached released levels were found.");
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(releasedLevelsPath);
            var releasedLevels = await JsonSerializer.DeserializeAsync<ReleasedLevelSummaryDto[]>(
                stream,
                SerializerOptions,
                cancellationToken)
                ?? [];

            FloodRushTelemetry.CacheOperations.Add(1, new TagList
            {
                { "operation", "released-levels-read" },
                { "result", "hit" }
            });
            logger.LogInformation("Loaded {Count} released levels from the local cache.", releasedLevels.Length);
            return releasedLevels;
        }
        catch (JsonException exception)
        {
            FloodRushTelemetry.CacheOperations.Add(1, new TagList
            {
                { "operation", "released-levels-read" },
                { "result", "invalid" }
            });
            logger.LogWarning(exception, "Cached released levels are invalid.");
            throw new InvalidOperationException("The cached released levels data is invalid.", exception);
        }
        finally
        {
            FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
            {
                { "operation", "released-levels-cache-read" }
            });
        }
    }

    public async Task SaveReleasedLevelsAsync(
        IReadOnlyCollection<ReleasedLevelSummaryDto> releasedLevels,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(releasedLevels);

        var stopwatch = Stopwatch.StartNew();
        Directory.CreateDirectory(cacheRootPath);

        await using var stream = File.Create(releasedLevelsPath);
        await JsonSerializer.SerializeAsync(stream, releasedLevels, SerializerOptions, cancellationToken);

        FloodRushTelemetry.CacheOperations.Add(1, new TagList
        {
            { "operation", "released-levels-write" },
            { "result", "success" }
        });
        FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
        {
            { "operation", "released-levels-cache-write" }
        });
        logger.LogInformation("Saved {Count} released levels to the local cache.", releasedLevels.Count);
    }

    public async Task<LevelRevisionDto?> GetLevelRevisionAsync(
        string levelId,
        string revision,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);

        var revisionPath = GetRevisionPath(levelId, revision);
        var stopwatch = Stopwatch.StartNew();
        if (!File.Exists(revisionPath))
        {
            FloodRushTelemetry.CacheOperations.Add(1, new TagList
            {
                { "operation", "level-revision-read" },
                { "result", "miss" }
            });
            logger.LogInformation("No cached level revision was found for {LevelId}/{Revision}.", levelId, revision);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(revisionPath);
            var levelRevision = await JsonSerializer.DeserializeAsync<LevelRevisionDto>(
                stream,
                SerializerOptions,
                cancellationToken);

            FloodRushTelemetry.CacheOperations.Add(1, new TagList
            {
                { "operation", "level-revision-read" },
                { "result", levelRevision is null ? "empty" : "hit" }
            });
            logger.LogInformation("Loaded cached level revision {LevelId}/{Revision}.", levelId, revision);
            return levelRevision;
        }
        catch (JsonException exception)
        {
            FloodRushTelemetry.CacheOperations.Add(1, new TagList
            {
                { "operation", "level-revision-read" },
                { "result", "invalid" }
            });
            logger.LogWarning(exception, "Cached level revision {LevelId}/{Revision} is invalid.", levelId, revision);
            throw new InvalidOperationException(
                $"The cached level revision '{levelId}/{revision}' is invalid.",
                exception);
        }
        finally
        {
            FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
            {
                { "operation", "level-revision-cache-read" }
            });
        }
    }

    public async Task SaveLevelRevisionAsync(
        LevelRevisionDto levelRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(levelRevision);

        var stopwatch = Stopwatch.StartNew();
        Directory.CreateDirectory(revisionsRootPath);

        var revisionPath = GetRevisionPath(levelRevision.LevelId, levelRevision.Revision);
        await using var stream = File.Create(revisionPath);
        await JsonSerializer.SerializeAsync(stream, levelRevision, SerializerOptions, cancellationToken);

        FloodRushTelemetry.CacheOperations.Add(1, new TagList
        {
            { "operation", "level-revision-write" },
            { "result", "success" }
        });
        FloodRushTelemetry.OperationDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, new TagList
        {
            { "operation", "level-revision-cache-write" }
        });
        logger.LogInformation("Saved cached level revision {LevelId}/{Revision}.", levelRevision.LevelId, levelRevision.Revision);
    }

    private string GetRevisionPath(string levelId, string revision) =>
        Path.Combine(
            revisionsRootPath,
            $"{Uri.EscapeDataString(levelId)}--{Uri.EscapeDataString(revision)}.json");
}
