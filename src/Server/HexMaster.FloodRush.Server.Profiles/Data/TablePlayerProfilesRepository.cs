using Azure;
using Azure.Data.Tables;
using HexMaster.FloodRush.Server.Abstractions.Storage;
using HexMaster.FloodRush.Shared.Contracts.Profiles;
using Microsoft.Extensions.Configuration;

namespace HexMaster.FloodRush.Server.Profiles.Data;

internal sealed class TablePlayerProfilesRepository : IPlayerProfilesRepository
{
    private const string TableName = "profiles";
    private readonly TableClient tableClient;
    private readonly Task _tableReadyTask;

    public TablePlayerProfilesRepository(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(StorageResourceNames.Tables)
            ?? throw new InvalidOperationException(
                $"Connection string '{StorageResourceNames.Tables}' is required for the profiles module.");

        tableClient = new TableClient(connectionString, TableName);
        _tableReadyTask = tableClient.CreateIfNotExistsAsync();
    }

    public async ValueTask<PlayerProfileDto> GetOrCreateByDeviceIdAsync(
        string deviceId,
        CancellationToken cancellationToken)
    {
        await EnsureTableExistsAsync(cancellationToken);

        try
        {
            var existing = await tableClient.GetEntityAsync<PlayerProfileEntity>(
                PlayerProfileEntity.PartitionValue,
                deviceId,
                cancellationToken: cancellationToken);

            var entity = existing.Value;
            entity.LastSeenAtUtc = DateTimeOffset.UtcNow;

            await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);

            return entity.ToDto();
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            var now = DateTimeOffset.UtcNow;
            var entity = new PlayerProfileEntity
            {
                RowKey = deviceId,
                ProfileId = Guid.NewGuid().ToString("N"),
                RegisteredAtUtc = now,
                LastSeenAtUtc = now
            };

            try
            {
                await tableClient.AddEntityAsync(entity, cancellationToken);
                return entity.ToDto();
            }
            catch (RequestFailedException conflictException) when (conflictException.Status == 409)
            {
                var existing = await tableClient.GetEntityAsync<PlayerProfileEntity>(
                    PlayerProfileEntity.PartitionValue,
                    deviceId,
                    cancellationToken: cancellationToken);

                return existing.Value.ToDto();
            }
        }
    }

    public async ValueTask<PlayerProfileDto?> GetByDeviceIdAsync(
        string deviceId,
        CancellationToken cancellationToken)
    {
        await EnsureTableExistsAsync(cancellationToken);

        try
        {
            var existing = await tableClient.GetEntityAsync<PlayerProfileEntity>(
                PlayerProfileEntity.PartitionValue,
                deviceId,
                cancellationToken: cancellationToken);

            return existing.Value.ToDto();
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }

    public async ValueTask<PlayerProfileDto> UpdateDisplayNameAsync(
        string deviceId,
        string displayName,
        CancellationToken cancellationToken)
    {
        await EnsureTableExistsAsync(cancellationToken);

        try
        {
            var existing = await tableClient.GetEntityAsync<PlayerProfileEntity>(
                PlayerProfileEntity.PartitionValue,
                deviceId,
                cancellationToken: cancellationToken);

            var entity = existing.Value;
            entity.DisplayName = displayName;
            entity.LastSeenAtUtc = DateTimeOffset.UtcNow;

            await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, cancellationToken);

            return entity.ToDto();
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            throw new InvalidOperationException($"Profile for device '{deviceId}' does not exist.");
        }
    }

    private async ValueTask EnsureTableExistsAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        await _tableReadyTask;
    }
}
