using HexMaster.FloodRush.Game.Core.Domain.Board;
using HexMaster.FloodRush.Game.Core.Domain.Levels;
using HexMaster.FloodRush.Game.Core.Domain.Rules;
using HexMaster.FloodRush.Game.Core.Domain.Tiles;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Data;

public sealed class BuiltInLevelsCatalog
{
    public const string FirstLevelId = "level-001";
    public const string FirstLevelRevision = "first-release-001";
    public const string FirstLevelDifficulty = nameof(DifficultyLabel.Easy);

    private static readonly DateTimeOffset FirstLevelReleasedAtUtc =
        new(2026, 3, 13, 0, 0, 0, TimeSpan.Zero);

    public IReadOnlyCollection<ReleasedLevelSummaryDto> GetReleasedLevels() =>
    [
        new ReleasedLevelSummaryDto(
            FirstLevelId,
            FirstLevelRevision,
            "Level 1 - First Flow",
            FirstLevelDifficulty,
            1,
            FirstLevelReleasedAtUtc)
    ];

    public LevelRevisionDto? GetLevelRevision(string levelId, string revision)
    {
        if (!string.Equals(levelId, FirstLevelId, StringComparison.Ordinal) ||
            !string.Equals(revision, FirstLevelRevision, StringComparison.Ordinal))
        {
            return null;
        }

        var definition = new LevelDefinition(
            FirstLevelId,
            "Level 1 - First Flow",
            new BoardDimensions(10, 6),
            60000,
            new FlowSpeedIndicator(1),
            [
                new StartPointTile(new GridPosition(0, 2), BoardDirection.Right),
                new FinishPointTile(new GridPosition(9, 2), BoardDirection.Left)
            ]);

        return new LevelRevisionDto(
            definition.LevelId,
            FirstLevelRevision,
            definition.DisplayName,
            FirstLevelDifficulty,
            definition.BoardDimensions.Width,
            definition.BoardDimensions.Height,
            definition.StartDelayMilliseconds,
            definition.FlowSpeedIndicator.Value,
            definition.FixedTiles.Select(MapFixedTile).ToArray());
    }

    private static LevelFixedTileDto MapFixedTile(FixedTile tile) =>
        tile switch
        {
            StartPointTile startPoint => new LevelFixedTileDto(
                LevelFixedTileTypeDto.StartPoint,
                startPoint.Position.X,
                startPoint.Position.Y,
                OutputDirection: MapDirection(startPoint.OutputDirection),
                BonusPoints: startPoint.BonusPoints),
            FinishPointTile finishPoint => new LevelFixedTileDto(
                LevelFixedTileTypeDto.FinishPoint,
                finishPoint.Position.X,
                finishPoint.Position.Y,
                EntryDirection: MapDirection(finishPoint.EntryDirection),
                BonusPoints: finishPoint.BonusPoints),
            FluidBasinTile fluidBasin => new LevelFixedTileDto(
                LevelFixedTileTypeDto.FluidBasin,
                fluidBasin.Position.X,
                fluidBasin.Position.Y,
                OutputDirection: MapDirection(fluidBasin.ExitDirection),
                EntryDirection: MapDirection(fluidBasin.EntryDirection),
                FillDelayMilliseconds: fluidBasin.FillDelayMilliseconds,
                BonusPoints: fluidBasin.BonusPoints,
                IsMandatory: fluidBasin.IsMandatory),
            SplitSectionTile splitSection => new LevelFixedTileDto(
                LevelFixedTileTypeDto.SplitSection,
                splitSection.Position.X,
                splitSection.Position.Y,
                OutputDirection: MapDirection(splitSection.PrimaryExitDirection),
                EntryDirection: MapDirection(splitSection.EntryDirection),
                SecondaryOutputDirection: MapDirection(splitSection.SecondaryExitDirection),
                SpeedModifierPercent: splitSection.SpeedModifierPercent,
                BonusPoints: splitSection.BonusPoints),
            _ => throw new InvalidOperationException($"Unsupported fixed tile type '{tile.GetType().Name}'.")
        };

    private static BoardDirectionDto MapDirection(BoardDirection direction) =>
        direction switch
        {
            BoardDirection.Left => BoardDirectionDto.Left,
            BoardDirection.Top => BoardDirectionDto.Top,
            BoardDirection.Right => BoardDirectionDto.Right,
            BoardDirection.Bottom => BoardDirectionDto.Bottom,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
}
