using Azure.Data.Tables;
using HexMaster.FloodRush.Server.Abstractions.Storage;
using HexMaster.FloodRush.Shared.Contracts.Levels;
using Microsoft.Extensions.Configuration;

namespace HexMaster.FloodRush.Server.Levels.Data;

internal sealed class TableLevelsRepository : ILevelsRepository
{
    private const string TableName = "levels";
    private readonly TableClient tableClient;

    public TableLevelsRepository(IConfiguration configuration)
    {
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

        var levels = new List<ReleasedLevelSummaryDto>();
        var query = tableClient.QueryAsync<ReleasedLevelEntity>(
            entity => entity.PartitionKey == ReleasedLevelEntity.PartitionValue,
            cancellationToken: cancellationToken);

        await foreach (var entity in query)
        {
            levels.Add(entity.ToDto());
        }

        return levels
            .OrderByDescending(level => level.ReleasedAtUtc)
            .ThenBy(level => level.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }
}
