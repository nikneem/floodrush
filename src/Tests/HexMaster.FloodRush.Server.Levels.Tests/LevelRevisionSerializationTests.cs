using System.Text.Json;
using HexMaster.FloodRush.Server.Levels.Data;
using HexMaster.FloodRush.Shared.Contracts.Levels;

namespace HexMaster.FloodRush.Server.Levels.Tests;

/// <summary>
/// Tests that verify the full serialization round-trip for seeded level revisions:
/// BasicLevelsSeedCatalog → HTTP JSON (ASP.NET Core default) → Client deserialization (JsonSerializerDefaults.Web)
/// </summary>
public sealed class LevelRevisionSerializationTests
{
    // Simulates what ASP.NET Core minimal APIs use by default (camelCase, no string enums)
    private static readonly JsonSerializerOptions AspNetCoreDefaults = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Simulates what the client uses: System.Net.Http.Json.ReadFromJsonAsync defaults
    private static readonly JsonSerializerOptions ClientDefaults = new(JsonSerializerDefaults.Web);

    private readonly BasicLevelsSeedCatalog catalog = new();

    [Theory]
    [InlineData("level-002", "basic-release-002", 2)]
    [InlineData("level-003", "basic-release-003", 5)]
    [InlineData("level-004", "basic-release-004", 7)]
    public void LevelRevision_HttpRoundTrip_PreservesFixedTiles(
        string levelId, string revision, int expectedRow)
    {
        // Arrange: get the LevelRevisionDto as it would be served after storage round-trip
        var serverDto = catalog.GetLevels().Single(l => l.LevelId == levelId);

        // Act: serialize (ASP.NET Core → HTTP response body) then deserialize (client)
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
        Assert.Null(startTile.EntryDirection);

        var finishTile = clientDto.FixedTiles.Single(t => t.TileType == LevelFixedTileTypeDto.FinishPoint);
        Assert.Equal(9, finishTile.X);
        Assert.Equal(expectedRow, finishTile.Y);
        Assert.Equal(BoardDirectionDto.Left, finishTile.EntryDirection);
        Assert.Null(finishTile.OutputDirection);
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

    [Fact]
    public void LevelRevision_SerializedJson_ContainsExpectedProperties()
    {
        var serverDto = catalog.GetLevels().First();
        var json = JsonSerializer.Serialize(serverDto, AspNetCoreDefaults);

        // Ensure property names are camelCase (as ASP.NET Core serializes them)
        Assert.Contains("\"levelId\"", json);
        Assert.Contains("\"fixedTiles\"", json);
        Assert.Contains("\"tileType\"", json);
        Assert.Contains("\"outputDirection\"", json);
        Assert.Contains("\"boardWidth\"", json);
        Assert.Contains("\"boardHeight\"", json);

        // Ensure enums are serialized as integers (not strings)
        Assert.DoesNotContain("\"StartPoint\"", json);
        Assert.DoesNotContain("\"Right\"", json);
        Assert.DoesNotContain("\"FinishPoint\"", json);
        Assert.DoesNotContain("\"Left\"", json);
    }
}
