# Game Engine and Scoring

## Overview

The game engine is a deterministic, frame-rate-independent simulation that lives entirely in `Game.Core` and requires no MAUI dependencies. It is driven by a controllable time source so tests can exercise complete level runs without a real clock.

## Runtime phases

The `GamePhase` enum defines the six lifecycle states of a `GameSession`:

| Phase | Description |
|-------|-------------|
| `LevelLoaded` | Level definition accepted; placement not yet open |
| `PlacementOpen` | Player can add and remove pipe sections |
| `FlowActive` | Fluid is flowing; pipes are locked |
| `Succeeded` | All required finish points reached |
| `Failed` | An irrecoverable branch failure occurred |
| `ScoreFinalized` | Score persisted; session complete |

## Domain model

### `GameSession`

The public entry point. Created with a `LevelDefinition` and optional `completionBonusPoints`.

```csharp
var session = new GameSession(level, completionBonus: 1000);
session.StartPlacementPhase();
session.PlacePipe(new GridPosition(1, 0), PipeSectionType.Horizontal);
session.StartFlow();
session.Tick(elapsedMs);         // drive the simulation
```

`PlacePipe` and `RemovePipe` are blocked once `StartFlow` is called.

### `GameBoard`

Combines the level's fixed tile map (cached at construction) with the player's placed pipes. All lookups are O(1) via internal dictionaries. Fixed tiles take priority over placed pipes at any given position.

### `PlacedPipe`

Represents one player-placed pipe section. Encapsulates the geometric open-directions table used by the engine to determine valid entry/exit pairs.

### `FlowBranch` (internal)

Tracks the live flow front: current `GridPosition`, `PendingExitDirection`, `AccumulatedMilliseconds`, `RequiredMilliseconds`, and `SpeedModifierPercent`. Not exposed outside the engine assembly.

### `ScoreBreakdown`

Accumulates score events as the simulation runs:
- `PipeSectionPoints` — sum of base points for each traversed pipe
- `CrossBonusPoints` — bonus for second traversal of a cross section on the perpendicular axis
- `BasinBonusPoints` — sum of basin fill bonuses
- `SplitBonusPoints` — sum of split entry bonuses
- `CompletionBonusPoints` — awarded when all finish points are reached

### `TraversalRecord`

Immutable record of one pipe traversal: position, axis, points awarded.

## Simulation mechanics

### Transit time

Each tile takes a fixed number of milliseconds to traverse, computed once per branch:

```
transitMs = (101 - speedValue) * 10 * 100 / speedModifierPercent
```

At speed 100 and modifier 100 this equals **10 ms per tile** (used in unit tests).  
At speed 50 and modifier 100 this equals **510 ms per tile**.

Integer arithmetic is used throughout to eliminate floating-point drift.

### Basin delay

When flow enters a `FluidBasinTile`, the `RequiredMilliseconds` for that tile step becomes:

```
basinTransitMs = transitMs + basin.FillDelayMilliseconds
```

The basin bonus is credited immediately on entry.

### Split section

When flow enters a `SplitSectionTile`:
1. The original branch is marked `Completed`.
2. Two new `FlowBranch` instances are created at the split position — one for each exit direction.
3. The split speed modifier applies to **all tiles reached via those new branches**.
4. The split bonus is credited once on entry.

New branches are not processed in the current tick; they are picked up on the next `Tick` call.

### Cross section double-scoring

The engine tracks traversal history per position and axis via an internal dictionary:
- First traversal of any axis: awards `BasePoints`.
- Second traversal on the **perpendicular** axis: awards `SecondaryTraversalBonusPoints`.
- Second traversal on the **same** axis: the branch is immediately marked `Failed`.

### Failure conditions

A branch is marked `Failed` when:
- The next position is outside the board.
- The target tile cannot accept flow from the incoming direction.
- A cross section is re-entered on the same axis.

The session transitions to `Phase = Failed` when any branch fails and not all required finish points have been reached.

## Scoring overview

| Category | Source |
|----------|--------|
| Pipe sections | `BasePoints` per `PlaceablePipeSectionDefinition` by type |
| Cross bonus | `SecondaryTraversalBonusPoints` on second perpendicular traversal |
| Basin bonus | `FluidBasinTile.BonusPoints` |
| Split bonus | `SplitSectionTile.BonusPoints` |
| Completion | Configurable per `GameSession` constructor |

Default base points by pipe type:

| Type | Points |
|------|--------|
| Horizontal | 10 |
| Vertical | 12 |
| CornerLeftToTop | 14 |
| CornerRightToTop | 15 |
| CornerLeftToBottom | 16 |
| CornerRightToBottom | 17 |
| Cross | 20 |

## Unit tests

All engine tests live in `HexMaster.FloodRush.Game.Core.Tests/Domain/Engine/`. The `InternalsVisibleTo` attribute exposes internal members to the test project.

Coverage target: **≥ 80% line and branch** for `Game.Core`.  
Actual (spec 5): **91.8% line / 87.8% branch**.

Key test scenarios covered:
- Straight-line success and scoring
- Wrong-direction placement → failure
- Out-of-bounds flow → failure  
- Cross section double traversal (horizontal then vertical) → bonus awarded
- Fluid basin pause duration
- Split into two branches → both must reach their finish points
- Negative elapsed ms → `ArgumentOutOfRangeException`
