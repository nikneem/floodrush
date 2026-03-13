using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Data;

internal sealed class LevelRevisionEntity : ITableEntity
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public const string PartitionValue = "revision";

    public string PartitionKey { get; set; } = PartitionValue;

    public string RowKey { get; set; } = string.Empty;

    public string LevelId { get; set; } = string.Empty;

    public string Revision { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Difficulty { get; set; } = "Medium";

    public int BoardWidth { get; set; }

    public int BoardHeight { get; set; }

    public int StartDelayMilliseconds { get; set; }

    public int FlowSpeedIndicator { get; set; }

    public string FixedTilesJson { get; set; } = "[]";

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public LevelRevisionDto ToDto() =>
        new(
            LevelId,
            Revision,
            DisplayName,
            string.IsNullOrWhiteSpace(Difficulty) ? "Medium" : Difficulty,
            BoardWidth,
            BoardHeight,
            StartDelayMilliseconds,
            FlowSpeedIndicator,
            JsonSerializer.Deserialize<LevelFixedTileDto[]>(FixedTilesJson, SerializerOptions)
                ?? []);

    public static LevelRevisionEntity FromDto(LevelRevisionDto dto) =>
        new()
        {
            RowKey = CreateRowKey(dto.LevelId, dto.Revision),
            LevelId = dto.LevelId,
            Revision = dto.Revision,
            DisplayName = dto.DisplayName,
            Difficulty = dto.Difficulty,
            BoardWidth = dto.BoardWidth,
            BoardHeight = dto.BoardHeight,
            StartDelayMilliseconds = dto.StartDelayMilliseconds,
            FlowSpeedIndicator = dto.FlowSpeedIndicator,
            FixedTilesJson = JsonSerializer.Serialize(dto.FixedTiles, SerializerOptions)
        };

    public static string CreateRowKey(string levelId, string revision) =>
        $"{Uri.EscapeDataString(levelId)}--{Uri.EscapeDataString(revision)}";
}
