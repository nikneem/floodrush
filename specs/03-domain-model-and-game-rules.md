# 03. Domain Model and Game Rules

## Objective
Define the core game concepts and immutable rules used by both gameplay and persistence.

## Board concepts
- The level board is a rectangular grid.
- Each cell may contain empty space, a fixed tile, or a player-placeable pipe tile.
- Coordinates must be stable and zero-based in persisted data.

## Required tile types
### Placeable pipe sections
- Horizontal
- Vertical
- Corner left-to-top
- Corner right-to-top
- Corner left-to-bottom
- Corner right-to-bottom
- Cross section

### Fixed tiles
- Start point
- Finish point
- Fluid basin
- Split section

## Flow rules
- Each level defines one or more start points and one or more required finish points.
- Flow begins after a level-defined start delay.
- Flow advances according to the level flow speed indicator.
- Flow can only continue through valid connections between adjacent tiles.
- A level is successful only when all required finish points are reached.
- Invalid, open, or missing downstream connections end that branch in failure.

## Split section behavior
- A split section generates two active downstream branches.
- Branches inherit the current flow state and continue independently.
- The split section applies a speed modifier only to segments downstream from the split.
- Levels using split sections may require multiple finish points.

## Fluid basin behavior
- A fluid basin fills before downstream flow continues.
- Basin fill time extends the effective planning window for downstream placement.
- A basin grants bonus score when used by a successful flow path.

## Cross section behavior
- The cross section supports one horizontal traversal and one vertical traversal.
- The first successful traversal awards its base cross score.
- The second traversal on the other axis awards additional bonus points.
- Repeating the same axis does not score twice.

## Validation rules
- Levels must not place incompatible fixed tiles in the same cell.
- All finish points must be theoretically reachable.
- Split sections in released levels must define downstream branches unambiguously.
- Flow speed indicator must be an integer from 1 to 100.

## Acceptance criteria
- The domain model can express every tile and rule described in the product brief.
- Validation catches impossible or malformed level definitions before play begins.
