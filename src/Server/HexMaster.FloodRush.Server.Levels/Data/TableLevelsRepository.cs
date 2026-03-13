using Azure.Data.Tables;
using HexMaster.FloodRush.Server.Abstractions.Storage;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.Extensions.Configuration;

namespace HexMaster.FloodRush.Server.Levels.Data;

internal sealed class TableLevelsRepository : ILevelsRepository
{
    private const string TableName = "levels";
    private readonly TableClient tableClient;
    private readonly BuiltInLevelsCatalog builtInLevelsCatalog;
    private readonly BasicLevelsSeedCatalog basicLevelsSeedCatalog;
    private readonly Task _tableReadyTask;

    public TableLevelsRepository(
        IConfiguration configuration,
        BuiltInLevelsCatalog builtInLevelsCatalog,
        BasicLevelsSeedCatalog basicLevelsSeedCatalog)
    {
        this.builtInLevelsCatalog = builtInLevelsCatalog;
        this.basicLevelsSeedCatalog = basicLevelsSeedCatalog;
        var connectionString = configuration.GetConnectionString(StorageResourceNames.Tables)
            ?? throw new InvalidOperationException(
                $"Connection string '{StorageResourceNames.Tables}' is required for the levels module.");

        tableClient = new TableClient(connectionString, TableName);
        _tableReadyTask = tableClient.CreateIfNotExistsAsync();
    }

    public async ValueTask<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        await _tableReadyTask;

        var levels = new List<ReleasedLevelSummaryDto>(builtInLevelsCatalog.GetReleasedLevels());
        var query = tableClient.QueryAsync<ReleasedLevelEntity>(
            entity => entity.PartitionKey == ReleasedLevelEntity.PartitionValue,
            cancellationToken: cancellationToken);

        await foreach (var entity in query)
        {
            levels.Add(entity.ToDto());
        }

        return levels
            .DistinctBy(level => (level.LevelId, level.Revision))
            .OrderByDescending(level => level.ReleasedAtUtc)
            .ThenBy(level => level.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public ValueTask<LevelRevisionDto?> GetLevelRevisionAsync(
        string profileId,
        string levelId,
        string revision,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);

        return GetLevelRevisionCoreAsync(levelId, revision, cancellationToken);
    }

    public async ValueTask<int> SeedBasicLevelsAsync(CancellationToken cancellationToken)
    {
        await _tableReadyTask;

        var revisions = basicLevelsSeedCatalog.GetLevels();
        var releasedLevels = basicLevelsSeedCatalog.GetReleasedLevels()
            .ToDictionary(level => (level.LevelId, level.Revision));

        foreach (var revision in revisions)
        {
            var releasedLevel = releasedLevels[(revision.LevelId, revision.Revision)];

            await tableClient.UpsertEntityAsync(
                new ReleasedLevelEntity
                {
                    RowKey = releasedLevel.LevelId,
                    Revision = releasedLevel.Revision,
                    DisplayName = releasedLevel.DisplayName,
                    Difficulty = releasedLevel.Difficulty,
                    FlowSpeedIndicator = releasedLevel.FlowSpeedIndicator,
                    ReleasedAtUtc = releasedLevel.ReleasedAtUtc
                },
                TableUpdateMode.Replace,
                cancellationToken);

            await tableClient.UpsertEntityAsync(
                LevelRevisionEntity.FromDto(revision),
                TableUpdateMode.Replace,
                cancellationToken);
        }

        return revisions.Count;
    }

    private async ValueTask<LevelRevisionDto?> GetLevelRevisionCoreAsync(
        string levelId,
        string revision,
        CancellationToken cancellationToken)
    {
        await _tableReadyTask;

        var response = await tableClient.GetEntityIfExistsAsync<LevelRevisionEntity>(
            LevelRevisionEntity.PartitionValue,
            LevelRevisionEntity.CreateRowKey(levelId, revision),
            cancellationToken: cancellationToken);

        if (response.HasValue && response.Value is { } entity)
        {
            return entity.ToDto();
        }

        return builtInLevelsCatalog.GetLevelRevision(levelId, revision);
    }
}
