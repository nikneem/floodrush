using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Data;

public sealed class BasicLevelsSeedCatalog
{
    private static readonly DateTimeOffset ReleasedAtUtc = new(2026, 3, 13, 0, 0, 0, TimeSpan.Zero);

    public IReadOnlyCollection<LevelRevisionDto> GetLevels() =>
    [
        CreateLevel("level-002", "basic-release-002", "Level 2 - Basic Flow", 2),
        CreateLevel("level-003", "basic-release-003", "Level 3 - Basic Flow", 5),
        CreateLevel("level-004", "basic-release-004", "Level 4 - Basic Flow", 7)
    ];

    public IReadOnlyCollection<ReleasedLevelSummaryDto> GetReleasedLevels() =>
        GetLevels()
            .Select(level => new ReleasedLevelSummaryDto(
                level.LevelId,
                level.Revision,
                level.DisplayName,
                level.Difficulty,
                level.FlowSpeedIndicator,
                ReleasedAtUtc))
            .ToArray();

    private static LevelRevisionDto CreateLevel(
        string levelId,
        string revision,
        string displayName,
        int row) =>
        new(
            levelId,
            revision,
            displayName,
            "Easy",
            10,
            10,
            30000,
            1,
            [
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.StartPoint,
                    0,
                    row,
                    OutputDirection: BoardDirectionDto.Right),
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.FinishPoint,
                    9,
                    row,
                    EntryDirection: BoardDirectionDto.Left)
            ]);
}
