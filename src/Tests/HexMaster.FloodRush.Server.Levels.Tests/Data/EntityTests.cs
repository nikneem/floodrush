using Azure;
using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Tests.Data;

public sealed class LevelRevisionEntityTests
{
    [Fact]
    public void CreateRowKey_EncodesLevelIdAndRevision()
    {
        var rowKey = LevelRevisionEntity.CreateRowKey("level-001", "first-release");

        Assert.Equal("level-001--first-release", rowKey);
    }

    [Fact]
    public void CreateRowKey_EscapesSpecialCharacters()
    {
        var rowKey = LevelRevisionEntity.CreateRowKey("level/1", "rev 2");

        Assert.Contains("%2F", rowKey);
        Assert.Contains("rev%202", rowKey);
    }

    [Fact]
    public void FromDto_MapsAllProperties()
    {
        var tiles = new[]
        {
            new LevelFixedTileDto(LevelFixedTileTypeDto.StartPoint, 0, 2, OutputDirection: BoardDirectionDto.Right),
            new LevelFixedTileDto(LevelFixedTileTypeDto.FinishPoint, 9, 2, EntryDirection: BoardDirectionDto.Left)
        };
        var dto = new LevelRevisionDto("level-001", "rev-1", "Test Level", "Easy", 10, 6, 3000, 5, tiles);

        var entity = LevelRevisionEntity.FromDto(dto);

        Assert.Equal("level-001", entity.LevelId);
        Assert.Equal("rev-1", entity.Revision);
        Assert.Equal("Test Level", entity.DisplayName);
        Assert.Equal("Easy", entity.Difficulty);
        Assert.Equal(10, entity.BoardWidth);
        Assert.Equal(6, entity.BoardHeight);
        Assert.Equal(3000, entity.StartDelayMilliseconds);
        Assert.Equal(5, entity.FlowSpeedIndicator);
        Assert.Equal(LevelRevisionEntity.PartitionValue, entity.PartitionKey);
        Assert.Equal(LevelRevisionEntity.CreateRowKey("level-001", "rev-1"), entity.RowKey);
    }

    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var tiles = new[]
        {
            new LevelFixedTileDto(LevelFixedTileTypeDto.StartPoint, 0, 2, OutputDirection: BoardDirectionDto.Right)
        };
        var dto = new LevelRevisionDto("level-001", "rev-1", "Test Level", "Hard", 10, 6, 3000, 5, tiles);
        var entity = LevelRevisionEntity.FromDto(dto);

        var result = entity.ToDto();

        Assert.Equal("level-001", result.LevelId);
        Assert.Equal("rev-1", result.Revision);
        Assert.Equal("Test Level", result.DisplayName);
        Assert.Equal("Hard", result.Difficulty);
        Assert.Single(result.FixedTiles);
    }

    [Fact]
    public void ToDto_EmptyDifficulty_DefaultsToMedium()
    {
        var entity = new LevelRevisionEntity
        {
            LevelId = "level-001",
            Revision = "rev-1",
            Difficulty = "",
            FixedTilesJson = "[]"
        };

        var result = entity.ToDto();

        Assert.Equal("Medium", result.Difficulty);
    }

    [Fact]
    public void RoundTrip_PreservesFixedTiles()
    {
        var tiles = new[]
        {
            new LevelFixedTileDto(LevelFixedTileTypeDto.StartPoint, 1, 2, OutputDirection: BoardDirectionDto.Right, BonusPoints: 100),
            new LevelFixedTileDto(LevelFixedTileTypeDto.FinishPoint, 8, 2, EntryDirection: BoardDirectionDto.Left)
        };
        var dto = new LevelRevisionDto("level-001", "rev-1", "Level 1", "Easy", 10, 6, 5000, 3, tiles);

        var result = LevelRevisionEntity.FromDto(dto).ToDto();

        Assert.Equal(2, result.FixedTiles.Count);
    }

    [Fact]
    public void ETagAndTimestamp_Setters_AreCovered()
    {
        var entity = new LevelRevisionEntity
        {
            ETag = Azure.ETag.All,
            Timestamp = DateTimeOffset.UtcNow
        };

        Assert.Equal(Azure.ETag.All, entity.ETag);
        Assert.NotNull(entity.Timestamp);
    }
}

public sealed class ReleasedLevelEntityTests
{
    [Fact]
    public void ToDto_MapsAllProperties()
    {
        var releasedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var entity = new ReleasedLevelEntity
        {
            RowKey = "level-001",
            Revision = "rev-1",
            DisplayName = "Level 1",
            Difficulty = "Easy",
            FlowSpeedIndicator = 3,
            ReleasedAtUtc = releasedAt
        };

        var dto = entity.ToDto();

        Assert.Equal("level-001", dto.LevelId);
        Assert.Equal("rev-1", dto.Revision);
        Assert.Equal("Level 1", dto.DisplayName);
        Assert.Equal("Easy", dto.Difficulty);
        Assert.Equal(3, dto.FlowSpeedIndicator);
        Assert.Equal(releasedAt, dto.ReleasedAtUtc);
    }

    [Fact]
    public void ToDto_EmptyDifficulty_DefaultsToMedium()
    {
        var entity = new ReleasedLevelEntity
        {
            RowKey = "level-001",
            Difficulty = "",
            ReleasedAtUtc = DateTimeOffset.UtcNow
        };

        var dto = entity.ToDto();

        Assert.Equal("Medium", dto.Difficulty);
    }

    [Fact]
    public void ToDto_NullDifficulty_DefaultsToMedium()
    {
        var entity = new ReleasedLevelEntity
        {
            RowKey = "level-001",
            Difficulty = null!,
            ReleasedAtUtc = DateTimeOffset.UtcNow
        };

        var dto = entity.ToDto();

        Assert.Equal("Medium", dto.Difficulty);
    }

    [Fact]
    public void ETagAndTimestamp_Setters_AreCovered()
    {
        var entity = new ReleasedLevelEntity
        {
            ETag = Azure.ETag.All,
            Timestamp = DateTimeOffset.UtcNow
        };

        Assert.Equal(Azure.ETag.All, entity.ETag);
        Assert.NotNull(entity.Timestamp);
    }
}
