# 05. Game Engine and Scoring

## Objective
Define the deterministic runtime behavior for fluid flow, victory evaluation, and score calculation.

## Runtime phases
1. Level loaded
2. Pre-flow placement window
3. Flow started
4. Active simulation
5. Success or failure resolution
6. Score finalization and persistence

## Simulation requirements
- The engine must update from a time source that can be controlled in tests.
- Flow progression must not depend on rendering frame rate.
- Multiple active branches must be simulated concurrently.
- The engine must track which tiles and axes were traversed for scoring.

## Score model
Each traversed pipe section awards points based on tile type.

### Base pipe scores

| Pipe type | Points per traversal | Notes |
|-----------|---------------------|-------|
| Horizontal | 10 | |
| Vertical | 10 | |
| Corner left-to-top | 12 | |
| Corner right-to-top | 12 | |
| Corner left-to-bottom | 12 | |
| Corner right-to-bottom | 12 | |
| Cross (first traversal) | 10 | Either horizontal or vertical axis |
| Cross (second traversal) | 50 bonus | Opposite axis only; same axis does not score twice |

These values are the defaults for all levels. Individual levels may define `PipeScoringOverride` entries to raise or lower any of these values.

### Additional scoring categories

| Category | Default value | Condition |
|----------|--------------|-----------|
| Fluid basin bonus | Defined per tile | Basin tile successfully traversed |
| Split section bonus | Defined per level | Level enables it |
| Completion bonus | 1 000 points | All required finish points reached |
| Time / efficiency bonus | Not yet introduced | Reserved for future level metadata |

Score is tracked in `ScoreBreakdown` (pipe traversal score, basin bonus, split bonus, completion bonus).

## Flow duration

The ViewModel calculates the animation duration for each tile traversal using:

```
durationMs = max(300, (101 − flowSpeedIndicator) × 100)
```

| Flow speed indicator | Duration per tile |
|---------------------|------------------|
| 1 (slowest) | 10 000 ms |
| 50 | 5 100 ms |
| 98 | 300 ms (floor) |
| 100 (fastest) | 300 ms (floor) |

The minimum floor of 300 ms ensures the water fill animation is always visible to the player.

### Fast-forward mode

The player can activate fast-forward at any time during an active level from the gameplay HUD. When enabled:

- Every subsequent tile animation runs for a fixed **500 ms** regardless of the level's flow speed indicator.
- Fast-forward is a UI-level override; it does not affect `GameSession` engine timing or score calculation.
- Fast-forward toggles off when the player clicks the button again.
- Fast-forward resets to off when the player retries the level.

## Failure model
- A branch fails if fluid exits the board, reaches an invalid connection, or reaches a dead end before fulfilling the level goals.
- A level fails when any required route becomes impossible to complete.

## Determinism requirements
- Given the same level, placements, and timing inputs, the engine must produce the same traversal order and score.
- Floating-point drift must not affect simulation outcome.

## Acceptance criteria
- Unit tests can simulate complete levels without MAUI.
- The engine supports start delays, basin delays, split speed modifiers, and cross double-scoring rules.
- All six `GamePhase` states are reachable and tested.
- `Tick(elapsedMs)` is deterministic: same inputs always produce the same traversal order and score.
- Integer-only transit time formula eliminates floating-point drift.
- Code coverage for `Game.Core` remains at or above 80% line and branch.

## Implementation notes
- Engine classes live in `Domain/Engine/` within `HexMaster.FloodRush.Game.Core`.
- `FlowBranch` is `internal`; `GameSession`, `GameBoard`, `PlacedPipe`, `ScoreBreakdown`, and `TraversalRecord` are public.
- `[assembly: InternalsVisibleTo("HexMaster.FloodRush.Game.Core.Tests")]` is declared in `Properties/AssemblyInfo.cs`.
- See `docs/game-engine-and-scoring.md` for the full design reference.
