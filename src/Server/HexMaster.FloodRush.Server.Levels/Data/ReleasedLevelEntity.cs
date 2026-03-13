using Azure;
using Azure.Data.Tables;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Data;

internal sealed class ReleasedLevelEntity : ITableEntity
{
    public const string PartitionValue = "released";

    public string PartitionKey { get; set; } = PartitionValue;

    public string RowKey { get; set; } = string.Empty;

    public string Revision { get; set; } = "1";

    public string DisplayName { get; set; } = string.Empty;

    public string Difficulty { get; set; } = "Medium";

    public int FlowSpeedIndicator { get; set; }

    public DateTimeOffset ReleasedAtUtc { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public ReleasedLevelSummaryDto ToDto() =>
        new(
            RowKey,
            Revision,
            DisplayName,
            string.IsNullOrWhiteSpace(Difficulty) ? "Medium" : Difficulty,
            FlowSpeedIndicator,
            ReleasedAtUtc);
}
