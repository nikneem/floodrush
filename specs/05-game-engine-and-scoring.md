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

Required scoring categories:
- Horizontal score
- Vertical score
- Corner scores
- Cross first traversal score
- Cross second traversal bonus
- Fluid basin bonus
- Split section bonus, if the level enables it
- Completion bonus for reaching all required finish points
- Optional time or efficiency bonus, if later introduced by level metadata

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
