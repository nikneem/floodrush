# 14. Wall Sections

## Objective

Introduce wall sections as a new category of fixed tile that level designers can use to create physical obstructions on the board. Walls add maze-like complexity by restricting where players may place pipes and how flow can be routed across the grid.

---

## Concept

A **wall section** is a fixed tile that permanently occupies a board cell and is entirely impassable. Walls are placed by the level designer in the level definition and cannot be placed, moved, or removed by the player. No fluid can enter or exit a wall cell. No pipe may be placed on a wall cell.

Walls turn open grids into directed puzzles. A well-placed wall forces the player to route flow around an obstacle, choose a specific pipe combination, or use a corner/cross where the direct path is blocked.

---

## Domain model

### `WallTile`

`WallTile` is a new concrete subtype of `FixedTile` defined in `HexMaster.FloodRush.Game.Core`.

```
WallTile
├── Position : GridPosition
└── (no direction, no bonus, no flow state)
```

- `WallTile` has no entry direction, exit direction, bonus points, or speed modifier.
- It carries only its `GridPosition`.
- It is immutable after construction.

### Registration in `LevelDefinition`

`WallTile` instances are included in the existing `FixedTiles` collection alongside start points, finish points, basins, and split sections. No new collection is needed.

---

## Rules

### Placement rules

| Rule | Detail |
|------|--------|
| Designer-only | Walls appear in the level definition. Players cannot add, move, or remove them. |
| No overlap | A wall cell may not share its `GridPosition` with any other fixed tile. |
| No pipe placement | Tapping a wall cell is a no-op; the pipe stack is not consumed. |
| No pipe replacement | An existing pipe cannot be placed on a wall cell. |

### Flow rules

| Rule | Detail |
|------|--------|
| Flow cannot enter | Any flow branch that tries to advance into a wall cell is immediately marked `Failed`. |
| Flow cannot exit | A wall tile never emits flow in any direction. |
| Unreachable finish | If walls block every possible path to a required finish point, the level must be rejected at validation time. |

### Board reachability

The existing BFS reachability check in `LevelDefinition` must treat wall cells as impassable nodes. A path through a wall cell is never valid. A level that cannot satisfy all finish points without passing through a wall fails the `FinishPointsAreReachable` invariant and must not be released.

---

## Level validation invariants

Add to the existing invariant set:

- No wall tile may share a position with any other fixed tile.
- No wall tile may share a position with a start point or finish point.
- At least one valid path from every start point to every required finish point must exist when wall tiles are excluded.

---

## Visual representation

### Asset

`wall_section.png` — a square image that fills the tile cell. The image is included in `Resources/Images/` and follows the same naming convention as other fixed-tile images.

### Rendering

- The wall tile is drawn as a full-cell filled block using the `wall_section.png` image.
- The wall tile renders **above** the empty-cell background layer but uses no fluid, pipe, or overlay layers.
- Walls do not animate at any point.
- Walls are visually distinct from empty cells and from all pipe types so the player can immediately identify them as impassable.
- The wall cell is **not tappable** — no tap gesture is registered on a wall cell.

### Z-order within a wall cell (bottom to top)

1. Base layer (tile background colour)
2. Background image (randomised empty-tile texture, same as other cells)
3. `wall_section.png` (fills the cell, no transparency)

No fluid path, pipe overlay, or points label layer is present on wall cells.

---

## Impact on gameplay mechanics

### Pipe placement stack

Tapping a wall cell must not consume a pipe from the placement stack. The tap is silently ignored and no animation or error feedback is shown.

### Illegal-move flash

The red illegal-move flash is **not** shown when tapping a wall. The tap is simply a no-op so the player receives no confusing feedback.

### Flow failure on wall contact

When the engine advances a flow branch and the next cell is a `WallTile`, the branch is immediately marked `Failed`. This triggers the same failure resolution path as flowing off the board edge or into an incompatible pipe.

---

## Server and serialization

### DTO / contract

`WallTile` must round-trip through the existing fixed-tile serialization. The recommended approach is a discriminated union using a `type` discriminator field already established for `FixedTile`.

```json
{
  "type": "wall",
  "position": { "column": 3, "row": 2 }
}
```

No additional fields are required. The contract lives in `HexMaster.FloodRush.Shared.Contracts`.

### Level seed

New seeded levels that include wall sections should be added to the API seed command (`seed-basic-levels`) to demonstrate the feature and support local development.

---

## Acceptance criteria

- `WallTile` exists in `HexMaster.FloodRush.Game.Core` as a `FixedTile` subtype with only a `Position` property.
- `LevelDefinition` accepts `WallTile` instances in its `FixedTiles` collection.
- Validation rejects a level where a wall position duplicates another fixed tile's position.
- Validation rejects a level where walls block every path from a start point to a required finish point (BFS check).
- `GameSession` marks any flow branch `Failed` the moment it would enter a wall cell.
- Tapping a wall cell in the MAUI client is a no-op: no pipe is placed, the stack is not consumed, no flash plays.
- `wall_section.png` is rendered as a full-cell image on wall tiles, drawn above the background but below any overlays.
- `WallTile` serializes and deserializes correctly through the shared contracts layer with `"type": "wall"`.
- At least one seeded level includes wall sections to enable end-to-end verification.
- Unit tests cover: flow-into-wall → `Failed`; placement-on-wall → rejected; wall in BFS → treated as impassable.
