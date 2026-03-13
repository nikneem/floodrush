namespace HexMaster.FloodRush.Shared.Contracts.Levels;

public sealed record LevelRevisionDto(
    string LevelId,
    string Revision,
    string DisplayName,
    string Difficulty,
    int BoardWidth,
    int BoardHeight,
    int StartDelayMilliseconds,
    int FlowSpeedIndicator,
    IReadOnlyCollection<LevelFixedTileDto> FixedTiles);
