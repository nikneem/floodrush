namespace HexMaster.FloodRush.Shared.Contracts.Levels;

public sealed record LevelFixedTileDto(
    LevelFixedTileTypeDto TileType,
    int X,
    int Y,
    BoardDirectionDto? OutputDirection = null,
    BoardDirectionDto? EntryDirection = null,
    BoardDirectionDto? SecondaryOutputDirection = null,
    int? FillDelayMilliseconds = null,
    int? SpeedModifierPercent = null,
    int BonusPoints = 0,
    bool IsMandatory = false);
