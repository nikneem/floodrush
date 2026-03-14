using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Data;

public sealed class BasicLevelsSeedCatalog
{
    private static readonly DateTimeOffset ReleasedAtUtc = new(2026, 3, 13, 0, 0, 0, TimeSpan.Zero);

    public IReadOnlyCollection<LevelRevisionDto> GetLevels() =>
    [
        CreateLevel("level-002", "basic-release-002", "Level 2 - Basic Flow", 2),
        CreateWalledLevel003(),
        CreateWalledLevel004(),
        CreateBasinLevel(),
        CreateMandatoryBasinLevel()
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

    /// <summary>
    /// Level 003 – "Around the Wall": 10×10 board. Walls at (3,5) and (6,5) block the direct
    /// horizontal path forcing the player to route around them.
    /// </summary>
    private static LevelRevisionDto CreateWalledLevel003() =>
        new(
            "level-003",
            "wall-release-003",
            "Level 3 - Around the Wall",
            "Easy",
            10,
            10,
            30000,
            1,
            [
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.StartPoint,
                    0,
                    5,
                    OutputDirection: BoardDirectionDto.Right),
                new LevelFixedTileDto(LevelFixedTileTypeDto.Wall, 3, 5),
                new LevelFixedTileDto(LevelFixedTileTypeDto.Wall, 6, 5),
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.FinishPoint,
                    9,
                    5,
                    EntryDirection: BoardDirectionDto.Left)
            ]);

    /// <summary>
    /// Level 004 – "Dual Barrier": 10×10 board. Walls at (2,7) and (7,7) break the direct
    /// path at row 7, requiring the player to route through adjacent rows.
    /// </summary>
    private static LevelRevisionDto CreateWalledLevel004() =>
        new(
            "level-004",
            "wall-release-004",
            "Level 4 - Dual Barrier",
            "Easy",
            10,
            10,
            30000,
            1,
            [
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.StartPoint,
                    0,
                    7,
                    OutputDirection: BoardDirectionDto.Right),
                new LevelFixedTileDto(LevelFixedTileTypeDto.Wall, 2, 7),
                new LevelFixedTileDto(LevelFixedTileTypeDto.Wall, 7, 7),
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.FinishPoint,
                    9,
                    7,
                    EntryDirection: BoardDirectionDto.Left)
            ]);

    /// <summary>
    /// Level 005 – "The Basin Trail": 12×8 board with an optional fluid basin.
    /// The basin sits off the direct horizontal path at row 3. Routing through it
    /// awards a 50-point bonus and triples the flow time for that tile.
    /// </summary>
    private static LevelRevisionDto CreateBasinLevel() =>
        new(
            "level-005",
            "wall-release-005",
            "Level 5 - The Basin Trail",
            "Easy",
            12,
            8,
            45000,
            3,
            [
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.StartPoint,
                    0,
                    4,
                    OutputDirection: BoardDirectionDto.Right),
                new LevelFixedTileDto(LevelFixedTileTypeDto.Wall, 3, 4),
                new LevelFixedTileDto(LevelFixedTileTypeDto.Wall, 8, 4),
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.FluidBasin,
                    5,
                    2,
                    EntryDirection: BoardDirectionDto.Left,
                    OutputDirection: BoardDirectionDto.Right,
                    BonusPoints: 50,
                    IsMandatory: false),
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.FinishPoint,
                    11,
                    4,
                    EntryDirection: BoardDirectionDto.Left)
            ]);

    /// <summary>
    /// Level 006 – "Fill Before Proceed": 12×8 board with a mandatory fluid basin.
    /// The engine will reject level completion unless the basin at (6, 2) is traversed.
    /// </summary>
    private static LevelRevisionDto CreateMandatoryBasinLevel() =>
        new(
            "level-006",
            "wall-release-006",
            "Level 6 - Fill Before Proceed",
            "Easy",
            12,
            8,
            45000,
            3,
            [
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.StartPoint,
                    0,
                    5,
                    OutputDirection: BoardDirectionDto.Right),
                new LevelFixedTileDto(LevelFixedTileTypeDto.Wall, 4, 5),
                new LevelFixedTileDto(LevelFixedTileTypeDto.Wall, 9, 5),
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.FluidBasin,
                    6,
                    2,
                    EntryDirection: BoardDirectionDto.Left,
                    OutputDirection: BoardDirectionDto.Right,
                    BonusPoints: 50,
                    IsMandatory: true),
                new LevelFixedTileDto(
                    LevelFixedTileTypeDto.FinishPoint,
                    11,
                    5,
                    EntryDirection: BoardDirectionDto.Left)
            ]);
}
