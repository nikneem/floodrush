# FloodRush Level Data Format Analysis

## Summary
This analysis covers the complete level data flow in FloodRush, from server-side seeding through client-side deserialization.

---

## 1. ASPIRE CUSTOM RESOURCE SEED COMMAND

**File**: src\Aspire\HexMaster.FloodRush.Aspire\HexMaster.FloodRush.Aspire.AppHost\ProjectResourceBuilderExtensions.cs

### Seed Command Configuration
The Aspire app host registers a custom HTTP command that invokes the dev seed endpoint:

`csharp
public static IResourceBuilder<ProjectResource> WithSeedBasicLevelsCommand(
    this IResourceBuilder<ProjectResource> builder)
{
    builder.WithHttpCommand(
        path: "/api/levels/dev/seed-basic-levels",
        displayName: "Add Basic Levels",
        endpointName: "https",
        commandName: "seed-basic-levels",
        commandOptions: new HttpCommandOptions
        {
            ConfirmationMessage = "Add three basic easy levels to local development storage?"
        });

    return builder;
}
`

### Levels Created by Seed
**File**: src\Server\HexMaster.FloodRush.Server.Levels\Data\BasicLevelsSeedCatalog.cs

The seed catalog creates **3 basic levels**:

`csharp
public IReadOnlyCollection<LevelRevisionDto> GetLevels() =>
[
    CreateLevel("level-002", "basic-release-002", "Level 2 - Basic Flow", 2),
    CreateLevel("level-003", "basic-release-003", "Level 3 - Basic Flow", 5),
    CreateLevel("level-004", "basic-release-004", "Level 4 - Basic Flow", 7)
];
`

### Seeded Level Details

Each level is created with:
- **DisplayName**: e.g., "Level 2 - Basic Flow"
- **Difficulty**: "Easy"
- **BoardWidth**: 10
- **BoardHeight**: 10
- **StartDelayMilliseconds**: 30000 (30 seconds)
- **FlowSpeedIndicator**: 1
- **ReleasedAtUtc**: 2026-03-13T00:00:00Z

### Fixed Tiles in Seeded Levels

Each level has exactly 2 fixed tiles (a start and finish point):

`csharp
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
                row,  // This is the only difference between levels (row 2, 5, 7)
                OutputDirection: BoardDirectionDto.Right),
            new LevelFixedTileDto(
                LevelFixedTileTypeDto.FinishPoint,
                9,
                row,
                EntryDirection: BoardDirectionDto.Left)
        ]);
`

**Key observation**: The only difference between the three seeded levels is the **row position** of the start/finish points.

---

## 2. SERVER-SIDE LEVEL STORAGE FORMAT (AZURE TABLE STORAGE)

**Table Name**: levels

### Entity 1: ReleasedLevelEntity
**File**: src\Server\HexMaster.FloodRush.Server.Levels\Data\ReleasedLevelEntity.cs

Stores the public metadata about released levels:

`csharp
internal sealed class ReleasedLevelEntity : ITableEntity
{
    public const string PartitionValue = "released";
    
    public string PartitionKey { get; set; } = PartitionValue;
    public string RowKey { get; set; } = string.Empty;  // Set to LevelId
    public string Revision { get; set; } = "1";
    public string DisplayName { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium";
    public int FlowSpeedIndicator { get; set; }
    public DateTimeOffset ReleasedAtUtc { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
`

**Storage Pattern**:
- PartitionKey: "released"
- RowKey: {LevelId} (e.g., "level-002")

### Entity 2: LevelRevisionEntity
**File**: src\Server\HexMaster.FloodRush.Server.Levels\Data\LevelRevisionEntity.cs

Stores the full level definition with fixed tiles:

`csharp
internal sealed class LevelRevisionEntity : ITableEntity
{
    public const string PartitionValue = "revision";
    
    public string PartitionKey { get; set; } = PartitionValue;
    public string RowKey { get; set; } = string.Empty;  // Composite key
    public string LevelId { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Medium";
    public int BoardWidth { get; set; }
    public int BoardHeight { get; set; }
    public int StartDelayMilliseconds { get; set; }
    public int FlowSpeedIndicator { get; set; }
    public string FixedTilesJson { get; set; } = "[]";  // JSON-serialized array
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
`

**Storage Pattern**:
- PartitionKey: "revision"
- RowKey: {Uri.EscapeDataString(levelId)}--{Uri.EscapeDataString(revision)}
  - Example: "level-002--basic-release-002"
- FixedTilesJson: JSON array of LevelFixedTileDto objects (deserialized on read)

**Note**: FixedTilesJson stores the tiles as a JSON serialized string using JsonSerializerDefaults.Web.

---

## 3. SERVER API RESPONSE DTOs

### Endpoint 1: GET /api/levels/released
**File**: src\Server\HexMaster.FloodRush.Server.Levels\LevelsModuleEndpointRouteBuilderExtensions.cs

`csharp
group.MapGet("/released", async (
    ClaimsPrincipal principal,
    IQueryHandler<GetReleasedLevelsQuery, ReleasedLevelsResponse> handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(
        new GetReleasedLevelsQuery(principal.GetRequiredProfileId()),
        cancellationToken);

    return Results.Ok(response);
})
`

**Response DTO**: ReleasedLevelsResponse
**File**: src\Shared\HexMaster.FloodRush.Shared.Contracts\Levels\ReleasedLevelsResponse.cs

`csharp
public sealed record ReleasedLevelsResponse(
    IReadOnlyCollection<ReleasedLevelSummaryDto> Levels);
`

### Endpoint 2: GET /api/levels/{levelId}/revisions/{revision}
**File**: src\Server\HexMaster.FloodRush.Server.Levels\LevelsModuleEndpointRouteBuilderExtensions.cs

`csharp
group.MapGet("/{levelId}/revisions/{revision}", async (
    string levelId,
    string revision,
    ClaimsPrincipal principal,
    IQueryHandler<GetLevelRevisionQuery, LevelRevisionDto?> handler,
    CancellationToken cancellationToken) =>
{
    var response = await handler.HandleAsync(
        new GetLevelRevisionQuery(principal.GetRequiredProfileId(), levelId, revision),
        cancellationToken);

    return response is null ? Results.NotFound() : Results.Ok(response);
})
`

**Response DTO**: LevelRevisionDto (or null)

---

## 4. SHARED CONTRACT DTOs

### ReleasedLevelSummaryDto
**File**: src\Shared\HexMaster.FloodRush.Shared.Contracts\Levels\ReleasedLevelSummaryDto.cs

`csharp
public sealed record ReleasedLevelSummaryDto(
    string LevelId,
    string Revision,
    string DisplayName,
    string Difficulty,
    int FlowSpeedIndicator,
    DateTimeOffset ReleasedAtUtc);
`

### LevelRevisionDto
**File**: src\Shared\HexMaster.FloodRush.Shared.Contracts\Levels\LevelRevisionDto.cs

`csharp
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
`

### LevelFixedTileDto
**File**: src\Shared\HexMaster.FloodRush.Shared.Contracts\Levels\LevelFixedTileDto.cs

`csharp
public sealed record LevelFixedTileDto(
    LevelFixedTileTypeDto TileType,
    int X,
    int Y,
    BoardDirectionDto? OutputDirection = null,
    BoardDirectionDto? EntryDirection = null,
    BoardDirectionDto? SecondaryOutputDirection = null,
    int? FillDelayMilliseconds = null,
    int? SpeedModifierPercent = null,
    int BonusPoints = 0);
`

### LevelFixedTileTypeDto
**File**: src\Shared\HexMaster.FloodRush.Shared.Contracts\Levels\LevelFixedTileTypeDto.cs

`csharp
public enum LevelFixedTileTypeDto
{
    StartPoint = 0,
    FinishPoint = 1,
    FluidBasin = 2,
    SplitSection = 3
}
`

### BoardDirectionDto
**File**: src\Shared\HexMaster.FloodRush.Shared.Contracts\Levels\BoardDirectionDto.cs

`csharp
public enum BoardDirectionDto
{
    Left = 0,
    Top = 1,
    Right = 2,
    Bottom = 3
}
`

---

## 5. CLIENT-SIDE LEVEL LOADING AND DESERIALIZATION

**Main File**: src\Game\HexMaster.FloodRush.Game\ViewModels\GameplayViewModel.cs

### Level Loading Flow

1. **InitiateLevelLoad** → Calls LoadLevelAsync()

2. **LoadLevelAsync()** → ResolveLevelAsync()
   - Checks network availability
   - Tries to fetch from API if online
   - Falls back to cache if offline or API fails

3. **ResolveLevelAsync()** → Returns (ReleasedLevelSummaryDto, LevelRevisionDto, source)
   
   `csharp
   private async Task<(ReleasedLevelSummaryDto ReleasedLevel, LevelRevisionDto Revision, string Source)> 
       ResolveLevelAsync(CancellationToken cancellationToken)
   {
       if (networkStatus.HasInternetAccess)
       {
           try
           {
               // Fetch from API
               var releasedLevels = await levelsApiService.GetReleasedLevelsAsync(cancellationToken);
               var releasedLevel = releasedLevels.FirstOrDefault(level =>
                   string.Equals(level.LevelId, LevelId, StringComparison.Ordinal))
                   ?? throw new InvalidOperationException($"The server did not return released level '{LevelId}'.");

               var levelRevision = await levelsApiService.GetLevelRevisionAsync(
                   releasedLevel.LevelId,
                   releasedLevel.Revision,
                   cancellationToken);

               // Cache both
               await levelCacheService.SaveLevelRevisionAsync(levelRevision, cancellationToken);
               await levelCacheService.SaveReleasedLevelsAsync(releasedLevels, cancellationToken);

               return (releasedLevel, levelRevision, "server");
           }
           catch (HttpRequestException exception)
           {
               logger.LogWarning(exception, "Falling back to the cached level revision for {LevelId} after a server request failed.", LevelId);
           }
       }

       // Fall back to cache
       var cachedReleasedLevels = await levelCacheService.GetReleasedLevelsAsync(cancellationToken);
       var cachedReleasedLevel = cachedReleasedLevels.FirstOrDefault(level =>
           string.Equals(level.LevelId, LevelId, StringComparison.Ordinal))
           ?? throw new InvalidOperationException($"Level '{LevelId}' is not available in the local cache.");

       var cachedRevision = await levelCacheService.GetLevelRevisionAsync(
           cachedReleasedLevel.LevelId,
           cachedReleasedLevel.Revision,
           cancellationToken)
           ?? throw new InvalidOperationException(
               $"Level '{cachedReleasedLevel.LevelId}' is listed locally but its cached revision is missing.");

       return (cachedReleasedLevel, cachedRevision, "cache");
   }
   `

4. **ApplyLevel()** → Applies DTOs to ViewModels
   
   `csharp
   private void ApplyLevel(ReleasedLevelSummaryDto releasedLevel, LevelRevisionDto levelRevision)
   {
       loadedReleasedLevel = releasedLevel;
       loadedRevision = levelRevision;
       DisplayName = levelRevision.DisplayName;
       Difficulty = string.IsNullOrWhiteSpace(levelRevision.Difficulty)
           ? releasedLevel.Difficulty
           : levelRevision.Difficulty;
       FlowSpeedIndicator = levelRevision.FlowSpeedIndicator;
       FlowTimeoutSeconds = (int)Math.Ceiling(TimeSpan.FromMilliseconds(levelRevision.StartDelayMilliseconds).TotalSeconds);
       RemainingPrepSeconds = FlowTimeoutSeconds;
       Score = 0;
       IsGameOver = false;
       IsSuccess = false;
       PlayerHighScore = null;
       GlobalHighScore = null;
       BoardWidth = levelRevision.BoardWidth;
       BoardHeight = levelRevision.BoardHeight;

       BoardTiles.Clear();
       foreach (var tile in BuildTiles(levelRevision))
       {
           BoardTiles.Add(tile);
       }

       UpcomingPipes.Clear();
       foreach (var pipe in BuildPipeStack())
       {
           UpcomingPipes.Add(pipe);
       }

       PipeStackAnimationVersion++;
   }
   `

### BuildTiles() - FixedTile Processing

The client deserializes fixed tiles from the LevelRevisionDto and processes them:

`csharp
private static IReadOnlyCollection<PlayfieldTileItem> BuildTiles(LevelRevisionDto levelRevision)
{
    var fixedTileLookup = (levelRevision.FixedTiles ?? []).ToDictionary(tile => (tile.X, tile.Y));
    var tiles = new List<PlayfieldTileItem>(levelRevision.BoardWidth * levelRevision.BoardHeight);
    var random = new Random(HashCode.Combine(levelRevision.LevelId, levelRevision.Revision));

    for (var y = 0; y < levelRevision.BoardHeight; y++)
    {
        for (var x = 0; x < levelRevision.BoardWidth; x++)
        {
            var backgroundImage = TileBackgroundImages[random.Next(TileBackgroundImages.Length)];

            if (fixedTileLookup.TryGetValue((x, y), out var fixedTile))
            {
                tiles.Add(new PlayfieldTileItem(
                    x,
                    y,
                    MapTileKind(fixedTile.TileType),
                    backgroundImage,
                    GetTileTitle(fixedTile.TileType),
                    GetTileSubtitle(fixedTile),
                    GetTileOverlayImage(fixedTile.TileType),
                    GetTileImageRotation(fixedTile)));
            }
            else
            {
                tiles.Add(new PlayfieldTileItem(x, y, PlayfieldTileKind.Empty, backgroundImage, string.Empty, string.Empty));
            }
        }
    }

    return tiles;
}
`

### Client-Side Caching

**Service**: src\Game\HexMaster.FloodRush.Game\Services\LevelCacheService.cs

Deserializes DTOs from JSON:

`csharp
public async Task<IReadOnlyCollection<ReleasedLevelSummaryDto>> GetReleasedLevelsAsync(
    CancellationToken cancellationToken = default)
{
    if (!File.Exists(releasedLevelsPath))
    {
        return [];
    }

    try
    {
        await using var stream = File.OpenRead(releasedLevelsPath);
        var releasedLevels = await JsonSerializer.DeserializeAsync<ReleasedLevelSummaryDto[]>(
            stream,
            SerializerOptions,
            cancellationToken)
            ?? [];

        return releasedLevels;
    }
    catch (JsonException exception)
    {
        throw new InvalidOperationException("The cached released levels data is invalid.", exception);
    }
}

public async Task<LevelRevisionDto?> GetLevelRevisionAsync(
    string levelId,
    string revision,
    CancellationToken cancellationToken = default)
{
    var revisionPath = GetRevisionPath(levelId, revision);
    if (!File.Exists(revisionPath))
    {
        return null;
    }

    try
    {
        await using var stream = File.OpenRead(revisionPath);
        var levelRevision = await JsonSerializer.DeserializeAsync<LevelRevisionDto>(
            stream,
            SerializerOptions,
            cancellationToken);

        return levelRevision;
    }
    catch (JsonException exception)
    {
        throw new InvalidOperationException(
            $"The cached level revision '{levelId}/{revision}' is invalid.",
            exception);
    }
}
`

**Storage Paths**:
- Released levels: {AppDataDirectory}/levels/released-levels.json
- Level revisions: {AppDataDirectory}/levels/revisions/{Uri.EscapeDataString(levelId)}--{Uri.EscapeDataString(revision)}.json

**JSON Serialization**: Uses JsonSerializerDefaults.Web with WriteIndented = false

---

## 6. DATA FLOW VERIFICATION

### Expected JSON Format for Seeded Levels

**Example LevelRevisionDto JSON** (as it would appear in cache or API response):

`json
{
  "levelId": "level-002",
  "revision": "basic-release-002",
  "displayName": "Level 2 - Basic Flow",
  "difficulty": "Easy",
  "boardWidth": 10,
  "boardHeight": 10,
  "startDelayMilliseconds": 30000,
  "flowSpeedIndicator": 1,
  "fixedTiles": [
    {
      "tileType": 0,
      "x": 0,
      "y": 2,
      "outputDirection": 2,
      "entryDirection": null,
      "secondaryOutputDirection": null,
      "fillDelayMilliseconds": null,
      "speedModifierPercent": null,
      "bonusPoints": 0
    },
    {
      "tileType": 1,
      "x": 9,
      "y": 2,
      "outputDirection": null,
      "entryDirection": 0,
      "secondaryOutputDirection": null,
      "fillDelayMilliseconds": null,
      "speedModifierPercent": null,
      "bonusPoints": 0
    }
  ]
}
`

**Example ReleasedLevelSummaryDto JSON**:

`json
{
  "levelId": "level-002",
  "revision": "basic-release-002",
  "displayName": "Level 2 - Basic Flow",
  "difficulty": "Easy",
  "flowSpeedIndicator": 1,
  "releasedAtUtc": "2026-03-13T00:00:00Z"
}
`

---

## 7. POTENTIAL MISMATCHES ANALYSIS

### ✓ No Mismatches Found

The level data format is consistent throughout the entire pipeline:

1. **Seed Data Creation**: BasicLevelsSeedCatalog creates LevelRevisionDto and ReleasedLevelSummaryDto
2. **Server Storage**: Entity classes preserve all DTO properties (FixedTilesJson stores tiles as serialized JSON)
3. **API Response**: Server returns the same DTO types
4. **Client Deserialization**: Client deserializes into the same DTO types using JsonSerializerDefaults.Web
5. **Client Processing**: BuildTiles() correctly processes the FixedTiles collection

### Verified Field Mappings:

| DTO Field | Seed Value | Server Entity | API Response | Client Usage |
|-----------|-----------|---------------|--------------|--------------|
| LevelId | "level-00X" | LevelRevisionEntity.LevelId | ✓ | LevelId |
| Revision | "basic-release-00X" | LevelRevisionEntity.Revision | ✓ | Revision |
| DisplayName | "Level X - Basic Flow" | LevelRevisionEntity.DisplayName | ✓ | DisplayName |
| Difficulty | "Easy" | LevelRevisionEntity.Difficulty | ✓ | Difficulty |
| BoardWidth | 10 | LevelRevisionEntity.BoardWidth | ✓ | BoardWidth |
| BoardHeight | 10 | LevelRevisionEntity.BoardHeight | ✓ | BoardHeight |
| StartDelayMilliseconds | 30000 | LevelRevisionEntity.StartDelayMilliseconds | ✓ | FlowTimeoutSeconds |
| FlowSpeedIndicator | 1 | LevelRevisionEntity.FlowSpeedIndicator | ✓ | FlowSpeedIndicator |
| FixedTiles | [StartPoint, FinishPoint] | LevelRevisionEntity.FixedTilesJson | ✓ | BuildTiles() |

---

## 8. KEY IMPLEMENTATION DETAILS

### Property Access Patterns
- Server uses Uri.EscapeDataString() for composite RowKeys to safely handle special characters
- Client uses the same pattern for file naming

### JSON Serialization Configuration
- Server (LevelRevisionEntity): 
ew(JsonSerializerDefaults.Web)
- Client (LevelCacheService): 
ew(JsonSerializerDefaults.Web) { WriteIndented = false }
- Both use JsonSerializerDefaults.Web for camelCase property names in JSON

### Fallback Strategy
- If online: Try API → Cache result
- If offline or API fails: Load from local cache
- If cache missing: Throw InvalidOperationException

### Fixed Tile Types Used in Seeded Levels
- StartPoint (0): OutputDirection = Right
- FinishPoint (1): EntryDirection = Left

