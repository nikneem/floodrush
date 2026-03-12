using Azure;
using Azure.Data.Tables;
using HexMaster.FloodRush.Shared.Contracts.Profiles;

namespace HexMaster.FloodRush.Server.Profiles.Data;

internal sealed class PlayerProfileEntity : ITableEntity
{
    public const string PartitionValue = "profile";

    public string PartitionKey { get; set; } = PartitionValue;

    public string RowKey { get; set; } = string.Empty;

    public string ProfileId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public DateTimeOffset RegisteredAtUtc { get; set; }

    public DateTimeOffset LastSeenAtUtc { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public PlayerProfileDto ToDto() =>
        new(ProfileId, RowKey, DisplayName, RegisteredAtUtc, LastSeenAtUtc);
}
