# Level Format and Release Progression

This document describes how FloodRush levels are represented, versioned, cached, and released to players.

## Level definition

A `LevelDefinition` is the core game-playable description of a level. It encapsulates:

| Field | Type | Description |
|---|---|---|
| `LevelId` | `string` | Stable identifier that never changes across revisions. |
| `DisplayName` | `string` | Human-readable title shown in menus and during play. |
| `BoardDimensions` | `BoardDimensions` | Width × height of the tile grid. |
| `StartDelayMilliseconds` | `int` | Milliseconds before fluid begins flowing after the level starts. |
| `FlowSpeedIndicator` | `FlowSpeedIndicator` | A 1–100 value controlling fluid flow speed. |
| `FixedTiles` | `IReadOnlyCollection<FixedTile>` | Pre-placed tiles (start, finish, basin, split). |
| `InventoryRules` | `IReadOnlyCollection<PipeInventoryRule>` | Per-type placement limits; empty means all types are unlimited. |
| `ScoringOverrides` | `IReadOnlyCollection<PipeScoringOverride>` | Per-level point adjustments; empty means default scoring applies. |

Invariants enforced by the domain model:
- At least one `StartPointTile` and one `FinishPointTile` must be present.
- No two fixed tiles may share the same grid position.
- All finish points must be theoretically reachable from a start point via the BFS reachability check.
- Inventory rules must not define the same pipe type twice.
- Scoring overrides must not define the same pipe type twice.

## Pipe inventory rules

`PipeInventoryRule` allows level designers to restrict how many of a given pipe type a player may place:

- `MaxCount = null` → that pipe type is unlimited (default when no rule is defined).
- `MaxCount = N` → player may place at most N tiles of that type.
- `MaxCount` must be at least 1 if set; defining a rule with 0 would make the pipe type blocked, which is modelled by simply omitting it from the inventory instead.

## Scoring overrides

`PipeScoringOverride` replaces the default point values for a specific pipe type in a level. This allows level designers to reward or de-incentivise particular pipe shapes:

- `BasePoints` replaces the default base points for the pipe type.
- `SecondaryTraversalBonusPoints` is only valid for the `Cross` pipe type; it awards bonus points when the cross is traversed a second time.

## Level revision

A `LevelRevision` wraps a `LevelDefinition` with:

- `RevisionToken` – a `LevelRevisionToken` (unique string, typically a GUID) that identifies this exact snapshot.
- `Metadata` – a `LevelMetadata` object carrying presentational and release information (see below).

**Revision immutability rule:** once a revision has been released, it must never change. If level behaviour changes, a new `LevelRevision` with a fresh `RevisionToken` must be created. Score submissions always reference the exact revision played.

## Level metadata

`LevelMetadata` carries optional presentational information that does not affect gameplay:

| Field | Description |
|---|---|
| `DisplayName` | Required. Shown in level selection and completion screens. |
| `Difficulty` | `DifficultyLabel` enum: `Easy`, `Medium`, `Hard`, `Expert`. |
| `ParScore` | Optional target score for a "par" completion. |
| `ReleasedFrom` | Optional earliest date/time at which this level is available. |
| `ReleasedUntil` | Optional expiry date/time. Requires `ReleasedFrom` to be set. |
| `TutorialHints` | Ordered list of hint strings shown during tutorial overlay. |
| `Tags` | Free-form strings used for filtering and discovery (e.g. `tutorial`, `featured`). |

## Release gating

The server is the authority for which levels are released to a profile:

- The `Load Level` screen only lists levels that have been released to the current profile.
- Levels are available locally (cached) or fetched from the server on demand.
- The sync system fetches newly released levels without requiring a full data reset.
- A successful released-level refresh should also persist the corresponding downloadable revisions locally so the player can continue into gameplay while offline.

## Seeded levels

The API ships with six built-in released levels for local development and testing. All are seeded through the `seed-basic-levels` Aspire dashboard command on the API resource.

| Level ID | Name | Board | Difficulty | Speed |
|---|---|---|---|---|
| `level-001` | Level 1 - First Flow | 10 × 6 | Easy | 1 |
| `level-002` | Level 2 - Simple Corner | 10 × 6 | Easy | 1 |
| `level-003` | Level 3 - Crossroads | 12 × 7 | Medium | 3 |
| `level-004` | Level 4 - The Long Way | 12 × 7 | Medium | 3 |
| `level-005` | Level 5 - Basin Challenge | 14 × 8 | Hard | 3 |
| `level-006` | Level 6 - Basin Mandatory | 14 × 8 | Hard | 3 |

Levels 005 and 006 introduce fluid basins. Level 006 includes a mandatory basin that the player must route flow through to successfully complete the level.

The `ReleasedFrom` date of all seeded levels is set to a past date so they appear immediately in the released-level catalog.

## Local cache tracking

`ReleasedLevel` tracks a server-released level and the device's local cache state:

| Cache status | Meaning |
|---|---|
| `NotDownloaded` | The level has never been downloaded to this device. |
| `Cached` | The local revision matches the latest server revision. |
| `Obsolete` | A newer revision has been released; the cached copy is stale. |

Cache status is derived automatically:
- If `CachedRevision == null` → `NotDownloaded`.
- If `CachedRevision.RevisionToken == LatestRevisionToken` → `Cached`.
- Otherwise → `Obsolete`.

An obsolete cached revision remains playable offline; the device will download the newer revision when connectivity is restored.

## Domain model class diagram (summary)

```
LevelRevision
├── RevisionToken : LevelRevisionToken
├── Definition    : LevelDefinition
│   ├── LevelId, DisplayName
│   ├── BoardDimensions
│   ├── FlowSpeedIndicator
│   ├── FixedTiles[]
│   ├── InventoryRules[]  (PipeInventoryRule)
│   └── ScoringOverrides[] (PipeScoringOverride)
└── Metadata      : LevelMetadata
    ├── DisplayName, Difficulty, ParScore
    ├── ReleaseWindow (ReleasedFrom / ReleasedUntil)
    ├── TutorialHints[]
    └── Tags[]

ReleasedLevel
├── LevelId
├── LatestRevisionToken : LevelRevisionToken
├── CachedRevision?     : LevelRevision
└── CacheStatus         : LevelCacheStatus (computed)
```
