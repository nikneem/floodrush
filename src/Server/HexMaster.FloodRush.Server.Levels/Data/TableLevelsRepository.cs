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

    public TableLevelsRepository(
        IConfiguration configuration,
        BuiltInLevelsCatalog builtInLevelsCatalog)
    {
        this.builtInLevelsCatalog = builtInLevelsCatalog;
        var connectionString = configuration.GetConnectionString(StorageResourceNames.Tables)
            ?? throw new InvalidOperationException(
                $"Connection string '{StorageResourceNames.Tables}' is required for the levels module.");

        tableClient = new TableClient(connectionString, TableName);
    }

    public async ValueTask<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
        string profileId,
        CancellationToken cancellationToken)
    {
        await tableClient.CreateIfNotExistsAsync(cancellationToken);

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
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(builtInLevelsCatalog.GetLevelRevision(levelId, revision));
}
