using System.Text.Json;
using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Tests;

/// <summary>
/// Tests that verify the full serialization round-trip for seeded level revisions:
/// BasicLevelsSeedCatalog → LevelRevisionEntity (FixedTilesJson) → LevelRevisionDto
/// → HTTP JSON (ASP.NET Core default) → Client deserialization (JsonSerializerDefaults.Web)
/// </summary>
public sealed class LevelRevisionSerializationTests
{
    // Simulates what ASP.NET Core minimal APIs use by default
    private static readonly JsonSerializerOptions AspNetCoreDefaults = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Simulates what the client uses (System.Net.Http.Json.ReadFromJsonAsync defaults)
    private static readonly JsonSerializerOptions ClientDefaults = new(JsonSerializerDefaults.Web);

    private readonly BasicLevelsSeedCatalog catalog = new();

    [Theory]
    [InlineData("level-002", "basic-release-002", 2)]
    [InlineData("level-003", "basic-release-003", 5)]
    [InlineData("level-004", "basic-release-004", 7)]
    public void LevelRevisionEntity_RoundTrip_PreservesFixedTiles(
        string levelId, string revision, int expectedRow)
    {
        // Arrange: get the original dto from the seed catalog
        var original = catalog.GetLevels().Single(l => l.LevelId == levelId);

        // Act: simulate storing to Azure Table Storage and retrieving back
        var entity = LevelRevisionEntity.FromDto(original);
        var retrieved = entity.ToDto();

        // Assert
        Assert.Equal(levelId, retrieved.LevelId);
        Assert.Equal(revision, retrieved.Revision);
        Assert.Equal(10, retrieved.BoardWidth);
        Assert.Equal(10, retrieved.BoardHeight);
        Assert.Equal(2, retrieved.FixedTiles.Count);

        var startTile = retrieved.FixedTiles.Single(t => t.TileType == LevelFixedTileTypeDto.StartPoint);
        Assert.Equal(0, startTile.X);
        Assert.Equal(expectedRow, startTile.Y);
        Assert.Equal(BoardDirectionDto.Right, startTile.OutputDirection);
        Assert.Null(startTile.EntryDirection);

        var finishTile = retrieved.FixedTiles.Single(t => t.TileType == LevelFixedTileTypeDto.FinishPoint);
        Assert.Equal(9, finishTile.X);
        Assert.Equal(expectedRow, finishTile.Y);
        Assert.Equal(BoardDirectionDto.Left, finishTile.EntryDirection);
        Assert.Null(finishTile.OutputDirection);
    }

    [Theory]
    [InlineData("level-002", "basic-release-002", 2)]
    [InlineData("level-003", "basic-release-003", 5)]
    [InlineData("level-004", "basic-release-004", 7)]
    public void LevelRevision_HttpRoundTrip_PreservesFixedTiles(
        string levelId, string revision, int expectedRow)
    {
        // Arrange: get the dto as the server would return it (after Table Storage round-trip)
        var original = catalog.GetLevels().Single(l => l.LevelId == levelId);
        var serverDto = LevelRevisionEntity.FromDto(original).ToDto();

        // Act: simulate the HTTP response serialization (ASP.NET Core) and
        //      client deserialization (System.Net.Http.Json)
        var json = JsonSerializer.Serialize(serverDto, AspNetCoreDefaults);
        var clientDto = JsonSerializer.Deserialize<LevelRevisionDto>(json, ClientDefaults);

        // Assert
        Assert.NotNull(clientDto);
        Assert.Equal(levelId, clientDto.LevelId);
        Assert.Equal(revision, clientDto.Revision);
        Assert.NotNull(clientDto.FixedTiles);
        Assert.Equal(2, clientDto.FixedTiles.Count);

        var startTile = clientDto.FixedTiles.Single(t => t.TileType == LevelFixedTileTypeDto.StartPoint);
        Assert.Equal(0, startTile.X);
        Assert.Equal(expectedRow, startTile.Y);
        Assert.Equal(BoardDirectionDto.Right, startTile.OutputDirection);

        var finishTile = clientDto.FixedTiles.Single(t => t.TileType == LevelFixedTileTypeDto.FinishPoint);
        Assert.Equal(9, finishTile.X);
        Assert.Equal(expectedRow, finishTile.Y);
        Assert.Equal(BoardDirectionDto.Left, finishTile.EntryDirection);
    }

    [Fact]
    public void ReleasedLevelsResponse_HttpRoundTrip_PreservesAllLevels()
    {
        // Arrange: build a ReleasedLevelsResponse as the server would return it
        var seededLevels = catalog.GetReleasedLevels();
        var response = new ReleasedLevelsResponse(seededLevels);

        // Act: serialize (server) then deserialize (client)
        var json = JsonSerializer.Serialize(response, AspNetCoreDefaults);
        var clientResponse = JsonSerializer.Deserialize<ReleasedLevelsResponse>(json, ClientDefaults);

        // Assert
        Assert.NotNull(clientResponse);
        Assert.NotNull(clientResponse.Levels);
        Assert.Equal(3, clientResponse.Levels.Count);

        Assert.Contains(clientResponse.Levels, l => l.LevelId == "level-002");
        Assert.Contains(clientResponse.Levels, l => l.LevelId == "level-003");
        Assert.Contains(clientResponse.Levels, l => l.LevelId == "level-004");
    }
}
