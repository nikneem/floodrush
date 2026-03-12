using Azure;
using Azure.Data.Tables;
using HexMaster.FloodRush.Shared.Contracts.Scores;

namespace HexMaster.FloodRush.Server.Scores.Data;

internal sealed class LevelScoreEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public string LevelRevision { get; set; } = string.Empty;

    public string ProfileId { get; set; } = string.Empty;

    public int Points { get; set; }

    public DateTimeOffset AchievedAtUtc { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public LevelScoreDto ToDto() =>
        new(PartitionKey, LevelRevision, ProfileId, Points, AchievedAtUtc);
}
