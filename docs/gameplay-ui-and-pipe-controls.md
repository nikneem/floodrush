# FloodRush — Gameplay UI and Pipe Controls

This document describes the visual and interactive design of the active gameplay screen: the pipe tile controls, the placement stack, the playfield grid, and the flow animation contract.

---

## Screen layout

The gameplay screen is divided into three horizontal zones:

```
┌──────────────────────────────────────────────────────────────┐
│  HUD: level name │ score │ speed indicator │ countdown timer │
├────────────┬─────────────────────────────────────────────────┤
│            │                                                  │
│  PIPE      │            PLAYFIELD VIEWPORT                   │
│  STACK     │                                                  │
│  (10 items)│  zoomable, scrollable playfield grid            │
│            │                                                  │
│  ↑ queued  │                                                  │
│            │                                                  │
│ [→ place]  │                                                  │
│            │                                                  │
└────────────┴─────────────────────────────────────────────────┘
```

- **HUD strip** — fixed height (~48 dp): level name, current score, fast-forward button, pause button, and the pre-flow countdown.
- The pre-flow countdown shifts from yellow to orange to red as it approaches zero, and it blinks during the last 10 seconds.
- **Pipe stack sidebar** — fixed width (~180–220 dp): 10 upcoming pipe sections, stacked vertically with enough room for a readable preview label.
- When a level loads, the current 10-item stack drops into the sidebar from above with a gravity-style settling animation.
- **Playfield viewport** — fills remaining space: a clipped viewport that hosts the level board and can pan around oversized layouts.

---

## Pipe placement stack

The stack is the player's queue of upcoming pipe sections.

### Rules

| Rule | Detail |
|------|--------|
| Fixed size | Always exactly 10 items |
| Active item | The **bottom** item is placed on the next tap |
| Consumption | Tap a cell → bottom item placed → items shift down → new item appended at top |
| Generation | New items are drawn randomly from the level's `PipeInventoryRule` pool |
| Pre-population | All 10 items are generated when the level loads, before the start countdown begins |

### Visual

- Items are small-scale pipe section previews.
- The **bottom item** has an amber highlight border — the "next to place" indicator.
- Items above it are shown at reduced opacity (queued, not yet active).
- A brief slide-down animation plays when an item is consumed.
- The initial queue also animates in from the top so the stack visibly settles from top to bottom when gameplay opens.

---

## Pipe tile controls

Each of the seven placeable pipe types is a MAUI `ContentView`. All share a base class (`PipeTileBase`) that owns the common contract.

### Geometry reference

| PipeSectionType | Open sides |
|-----------------|-----------|
| Horizontal | Left ↔ Right |
| Vertical | Top ↔ Bottom |
| CornerLeftToTop | Left ↔ Top |
| CornerRightToTop | Right ↔ Top |
| CornerLeftToBottom | Left ↔ Bottom |
| CornerRightToBottom | Right ↔ Bottom |
| Cross | Left ↔ Right **and** Top ↔ Bottom |

### Visual design

- Pipe PNG image rendered on top; fluid animation layer is **below** the pipe image so the pipe appears to fill from inside.
- Fluid fill is a **46 px thick path stroke** (blue-teal) that sweeps from entry edge to exit edge using `StrokeDashOffset` animation.
- On lock (flow started), a subtle border highlight communicates the tile is now immovable.

### Flow timing

Flow animation duration scales linearly with the level's `FlowSpeedIndicator` (1–100):

```
durationMs = Math.Max(300, (101 - flowSpeedIndicator) × 100)
```

Speed 1 ≈ 10 000 ms per tile. Speed 100 = minimum 300 ms per tile.

When **fast-forward** is active, each tile always animates at a fixed **500 ms** regardless of the speed indicator. Enabling fast-forward mid-tile immediately cancels the current in-flight animation (snapping it to fully drawn) and starts the next tile at 500 ms.

**Fluid basins** take three times the normal tile duration.

### Fast-forward and pause

The HUD contains two control buttons:

| Button | Position | Behaviour |
|--------|----------|-----------|
| Pause | Right side of HUD | Opens the `PauseResultOverlay` in pause state. |
| Fast-forward | Between the score and the pause button | Toggles fast-forward. When active, tile flow time drops to 500 ms. Mid-tile transitions cancel immediately. |

Both buttons use the default app `Button` style (amber gradient `PrimaryButtonStyle`).

### Fluid animation visual

The flowing fluid is rendered as a **46 px thick `Path` stroke** that sweeps from the entry edge to the exit edge of the tile. Key properties:

- The fluid layer sits **below** the pipe PNG image in the Z-order, so the pipe appears to be filling up from inside.
- The reveal animation uses `StrokeDashOffset` (MAUI's `AnimateTo`) to sweep the path progressively from 0 to fully drawn over `durationMs`.
- For straight pipes the path is a single line segment from entry midpoint to exit midpoint.
- For corner pipes the path has three segments: a short straight entry segment, a quarter-circle arc, then a short straight exit segment — producing a smooth filled-pipe appearance through the bend.
- For cross pipes the path follows the primary axis (entry to exit) only; the perpendicular axis is drawn separately if a second traversal occurs.

### Tile rendering

Each tile is a `PlayfieldTileView` (a MAUI `ContentView`) rendered in a `PlayfieldBoardView` grid. Tile visual layers from bottom to top:

1. Base layer (tile background colour)
2. Background image (randomised empty-tile texture)
3. **Fluid path** (`Path` with 46 px stroke — fluid animation)
4. Pipe overlay image (the pipe PNG)
5. Pipe flood-fill (persistent water tint after traversal)
6. Overlay (colour tint for tile type)
7. Tile content labels
8. Points label ("+N pts" popup)
9. Illegal-move flash (red, topmost)

### Lock behaviour

| State | Can be replaced? | Visual |
|-------|-----------------|--------|
| Empty | — (no tile here) | Dark cell |
| Placed (unlocked) | Yes | Pipe rendered normally |
| Placed (locked) | **No** | Pipe with lock highlight |

---

## Playfield grid

The grid renders the full level board. Each cell maps to a `GridPosition`.

Each board cell is its own MAUI tile control, so the level-defined row and column count directly determines how many tile views are created inside the zoomable playfield viewport.

### Viewport interaction

- The board may be larger than the visible gameplay area.
- The gameplay board renders each playfield tile at a base size of **64 × 64** pixels before zoom is applied.
- Players can zoom the playfield between **50% and 200%**.
- Players can drag to pan across the board whenever the current zoom level or board size causes overflow in either direction.
- On Windows, the mouse wheel scrolls the playfield viewport vertically.
- Zoom and pan only change what part of the board is visible; they do not affect scoring, timing, or placement rules.
- The first playable slice renders the downloaded released-level board immediately so the player can inspect the fixed tiles before starting.
- Every playfield cell gets a randomized `empty_tile_background_{x}.png` texture when the board is built, and those assignments stay stable for that loaded level so redraws do not reshuffle the art.

### Cell states

| State | Tappable | Content |
|-------|----------|---------|
| Empty | Yes | Faint border, dark fill |
| Occupied, unlocked | Yes (replace) | Pipe control |
| Occupied, locked | No | Pipe control + lock indicator |
| Fixed tile | No | Fixed tile control |
| Start point | No | Start tile with exit-direction arrow |
| Finish point | No | Finish tile with required-entry indicator |
| Wall | No | Wall image (`wall_section.png`), full-cell fill |

### Tap-to-place flow

1. Player taps an empty or unlocked cell.
2. The playfield pops the bottom item from the pipe stack.
3. A pipe control of that type is instantiated with `EntryDirection` / `ExitDirection` calculated from the pipe's geometry and the cell position.
4. The pipe control is placed into the grid cell.
5. The stack shifts; a new item is generated and appended to the top.

### Gesture rules

- **Single tap** — place or replace the next pipe, subject to the normal lock and fixed-tile rules.
- **Two-finger pinch** — zoom the playfield viewport in or out.
- **Drag** — scroll the viewport horizontally or vertically across the playfield when the content exceeds the visible bounds.

## Pre-start modal

When gameplay loads a released level from the server, the board and the left-side pipe stack are rendered first so the player can see the full empty-tile playfield before a centered summary card appears above it. The card shows:

- Level number
- Difficulty
- Flow timeout
- Flow speed indicator

The card's bottom-aligned **Start** button dismisses the preview and begins the countdown.

---

## Fixed tile controls

### Start point

- Rendered with a source icon and a **directional arrow** pointing in `OutputDirection`.
- Shows the countdown timer during the pre-flow placement window.
- When start delay expires, triggers the first `StartFlow` chain.

### Finish point

- Rendered with a target icon and an **opening indicator** on the `EntryDirection` side.
- Players must complete a pipe connection to this side.
- Transitions to a "satisfied" visual state when the flow arrives.

### Fluid basin fixed tile

- Rendered with a basin/reservoir icon (`pipe_section_bassin.png` or `pipe_section_bassin_mandatory.png`).
- Shows a fill animation during the extra delay (3× normal tile time).
- Grants **50 bonus points** when traversed.
- If `IsMandatory = true`, the player **must** route flow through this basin for the level to count as successful. A mandatory basin that is never visited causes the session to end in `Failed` even if all finish points are reached.

### Split section

- Rendered with a fork icon.
- Creates two downstream flow branches when reached.
- Applies its `SpeedModifierPercent` to all downstream segments.

### Wall section

- Rendered with a square wall icon (`wall_section.png`), filling the entire cell.
- **Not tappable** — the player cannot place pipes on wall cells.
- **Flow cannot enter a wall cell.** Any branch that attempts to enter a wall cell is immediately marked `Failed`.
- The level validation (BFS reachability) will reject any level where wall placement makes a finish point unreachable from all start points.
- Wall tiles have no bonus points and no flow-time modifier.

---

## Flow event chain

When the start delay expires:

```
GameSession → FlowActive
  ↓
Playfield.StartFlow(startTile.OutputDirection, speedMultiplier)
  ↓
StartTile raises FlowCompleted(exitDirection, position)
  ↓
Playfield resolves adjacent cell in exitDirection
  ↓
AdjacentPipe.StartFlow(reciprocalEntry, speedMultiplier)
  ↓
[repeat until finish, failure, or board edge]
```

- **Finish reached** → mark finish point satisfied; when all satisfied → `GameSession.Succeeded`
- **Invalid connection / board edge** → `GameSession.Failed`
- **Basin tile** → delay next `StartFlow` by `FillDelayMilliseconds`
- **Split tile** → call `StartFlow` on both exit directions simultaneously (two branches)
- **Wall tile** → branch immediately marked `Failed`

---

## Component locations

| Component | Path |
|-----------|------|
| Pipe tile base | `src/Game/HexMaster.FloodRush.Game/Controls/Pipes/PipeTileBase.cs` |
| 7 pipe controls | `src/Game/HexMaster.FloodRush.Game/Controls/Pipes/` |
| Pipe stack | `src/Game/HexMaster.FloodRush.Game/Controls/PipeStackControl.xaml` |
| Playfield grid | `src/Game/HexMaster.FloodRush.Game/Controls/PlayfieldGridControl.xaml` |
| Fixed tile controls | `src/Game/HexMaster.FloodRush.Game/Controls/Tiles/` |

All `BoardDirection` and `PipeSectionType` types are imported from `HexMaster.FloodRush.Game.Core`.
