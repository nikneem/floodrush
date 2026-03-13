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

## Initial released level

The API currently includes a first built-in released level intended to bootstrap end-to-end development:

- `LevelId`: `level-001`
- Display name: `Level 1 - First Flow`
- Difficulty: `Easy`
- Board size: **10 x 6**
- Start point: first column, flowing right
- Finish point: last column, accepting from the left
- Pre-flow timeout: **60 seconds**
- Flow speed indicator: **1**

Both the released-level summary payload and the downloadable level revision carry this difficulty label so the MAUI client can show matching details in the level list and the gameplay start modal.

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
