# 11. Gameplay UI and Pipe Controls

## Objective

Define the visual and interactive components that make up the active gameplay screen: the pipe tile controls, the pipe placement stack, the playfield grid, and fixed tile representations. This spec covers the MAUI control design, the tap-to-place mechanic, flow animation contracts, and how the pipe controls communicate with the playfield.

---

## Pipe section controls

Each of the seven placeable pipe section types is a self-contained MAUI `ContentView`. All seven share a common base but render different path geometry.

### Pipe section types

| Type | Entry directions | Exit directions |
|------|-----------------|-----------------|
| Horizontal | Left | Right (and vice versa) |
| Vertical | Top | Bottom (and vice versa) |
| CornerLeftToTop | Left | Top (and vice versa) |
| CornerRightToTop | Right | Top (and vice versa) |
| CornerLeftToBottom | Left | Bottom (and vice versa) |
| CornerRightToBottom | Right | Bottom (and vice versa) |
| Cross | Left/Right | Right/Left **and** Top/Bottom |

These map directly to `PipeSectionType` in `HexMaster.FloodRush.Game.Core.Domain.Pipes`.

### Visual design

- Each control renders a pipe path as a **semi-transparent graphic** (target opacity 0.80 for the pipe body).
- The pipe path colour uses the secondary navy palette with a subtle metallic edge highlight.
- Transparency allows a **water fill animation to play behind the pipe shape**, creating the illusion of fluid flowing through it.
- The water fill is a separate animated layer (opacity 0.70, coloured blue-teal gradient) that sweeps from entry to exit following the pipe path geometry.
- Fixed tiles (start, end, basin, split) use a distinct visual treatment (heavier border, icon or label) to communicate they are non-removable.

### Bindable properties

| Property | Type | Description |
|----------|------|-------------|
| `PipeType` | `PipeSectionType` | Determines which path geometry is rendered |
| `EntryDirection` | `BoardDirection` | The side from which flow enters this tile |
| `ExitDirection` | `BoardDirection` | The side from which flow exits this tile |
| `IsLocked` | `bool` | `true` once flow has started; prevents replacement |
| `SpeedMultiplier` | `double` | Affects water animation duration (see below) |

For the `Cross` type, `EntryDirection` and `ExitDirection` refer to the **active traversal axis** for a given flow invocation. The cross control supports being traversed twice.

### Flow timing

- Base flow duration through any single tile: **10 seconds** at `SpeedMultiplier = 1`.
- At `SpeedMultiplier = 100`, flow duration is **1 second**.
- Formula: `durationMs = 10000 - ((speedMultiplier - 1) / 99.0 * 9000)` — linear scale from 10 000 ms to 1 000 ms.
- The water animation runs for exactly this duration, from entry edge to exit edge.
- Speed multiplier is provided externally by the playfield; the control does not determine the game speed.

### Public API

```csharp
// Start water flow animation through this tile.
// entryDirection: the side water enters from.
// speedMultiplier: 1–100 from level/split definition.
void StartFlow(BoardDirection entryDirection, double speedMultiplier);

// Raised immediately when StartFlow is called; tile locks itself.
event EventHandler<FlowStartedEventArgs> FlowStarted;

// Raised when the water animation reaches the exit edge.
event EventHandler<FlowCompletedEventArgs> FlowCompleted;
```

### Event arguments

```csharp
public class FlowStartedEventArgs : EventArgs
{
    public BoardDirection EntryDirection { get; }
    public GridPosition Position { get; }
}

public class FlowCompletedEventArgs : EventArgs
{
    public BoardDirection ExitDirection { get; }
    public GridPosition Position { get; }
}
```

The playfield uses `FlowCompleted` to determine which adjacent tile to start flowing through next, using `ExitDirection` to calculate the neighbour and the reciprocal entry direction.

### Locked state

- `IsLocked` is set to `true` when `StartFlow` is called.
- A locked tile cannot be tapped or replaced by the player.
- Locked tiles render a subtle border highlight or overlay to communicate they are fixed.
- Tiles that have never had flow started on them remain replaceable until flow reaches their position.

---

## Pipe placement stack

The placement stack is the player's inventory of upcoming pipe sections. It is displayed as a **vertical list on the left side of the gameplay screen**.

### Stack rules

- The stack always contains exactly **10 pipe section previews**.
- The **bottommost item** is the pipe that will be placed on the next tap.
- When a tile is placed, the remaining 9 items shift down by one position, and a new item is **appended to the top**.
- New items are generated randomly from the available pipe types, subject to the level's `PipeInventoryRule` definitions. If a type has reached its `MaxCount`, it is excluded from the random pool until the level resets.
- The first 10 items are pre-populated when the level loads, before the start delay countdown begins.
- When a level finishes loading, the initial stack items animate in from above and settle into their slots with a gravity-like drop.

### Visual design

- Stack items are rendered as small pipe section previews (scaled-down versions of the full controls).
- The bottommost item (next to place) is highlighted with an amber glow or border.
- The remaining 9 items are shown at reduced opacity to indicate they are queued but not yet active.
- The stack slides down with a brief animation when an item is consumed.
- The initial 10 generated items drop from the top of the sidebar toward their final positions with a bounce-eased settling motion.

### Placement interaction

- Tapping any **empty or replaceable** cell on the playfield places the current bottom-of-stack pipe there.
- If the tapped cell already contains a (non-locked) pipe, it is replaced.
- Locked cells (flow already started) cannot be tapped for placement.
- Fixed tiles (start, finish, basin, split) cannot be tapped for placement.

---

## Playfield grid

The playfield grid occupies the **right portion of the gameplay screen** (approximately 80% width in landscape). The remaining 20% on the left holds the pipe stack. The rendered board lives inside a viewport so the full playfield can extend beyond the immediately visible gameplay area.

### Grid composition

- The grid is a collection of tappable cells matching the `BoardDimensions` of the loaded level.
- Cells are rendered uniformly; fixed tiles replace empty cells at their defined `GridPosition`.
- Each board cell is rendered as its own MAUI tile control inside the zoomable playfield viewport.
- The grid renders cell borders to help players orient the board.
- Every rendered playfield cell uses one of the shipped `empty_tile_background_{x}.png` images as a randomized background, chosen once per loaded board so the board texture feels varied without flickering during redraws.
- The viewport clips the visible region while allowing the rendered playfield to exceed the viewport bounds.
- On initial load, the gameplay page renders the released level's fixed tiles from the downloaded `LevelRevisionDto` before any pipe placement begins.

### Viewport behaviour

- The gameplay board renders each playfield tile at a base size of **64 × 64** pixels before zoom is applied.
- The playfield viewport supports zoom from **50% to 200%**.
- The default zoom is **100%**.
- If the rendered board is wider or taller than the viewport, dragging pans horizontally and vertically across the board.
- On Windows, the mouse wheel scrolls the playfield viewport vertically.
- Pinch and drag gestures only change the viewport transform. They do not modify the board state, timer, or score.
- Single-cell placement remains tap-driven and continues to work inside the current zoomed or panned viewport position.

### Cell state

| State | Interaction | Visual |
|-------|------------|--------|
| Empty | Tap to place next pipe | Faint grid border, dark fill |
| Occupied (unlocked) | Tap to replace | Pipe control rendered; replaceable |
| Occupied (locked) | No interaction | Pipe control rendered; locked overlay |
| Fixed tile | No interaction | Fixed tile rendered with icon |
| Start point | No interaction | Start tile with directional arrow |
| Finish point | No interaction | Finish tile with entry indicator |

### Touch handling

- A single tap on a cell triggers placement of the next stack item.
- There is no drag-and-drop; tap only.
- Placement is immediate with no confirmation.
- A two-finger pinch changes the playfield viewport zoom level.
- A drag gesture scrolls the playfield viewport whenever content extends beyond the visible bounds.

---

## Start and finish point tiles

Start and finish point tiles are fixed and rendered as part of the playfield grid. They are not part of the player's pipe stack.

### Start point

- Rendered with a distinctive icon (e.g. a source/pipe origin icon) and an **arrow indicating the exit direction** (`OutputDirection` on `StartPointTile`).
- The arrow communicates which neighbouring cell the water will flow into first.
- The start countdown timer is displayed on or adjacent to the start tile during the placement window.

### Finish point

- Rendered with a target/goal icon and an **arrow or opening indicating the required entry direction** (`EntryDirection` on `FinishPointTile`).
- Players must connect a pipe to the finish tile from the correct direction to complete the route.
- When a finish point is reached by the flow, it changes to a "completed" visual state.
- Levels with multiple finish points require all finish points to be reached for success.

---

## Gameplay screen layout

```
┌──────────────────────────────────────────────────────────────┐
│  HUD: level name │ score │ speed indicator │ timer           │
├────────────┬─────────────────────────────────────────────────┤
│            │                                                  │
│  PIPE      │            PLAYFIELD VIEWPORT                   │
│  STACK     │                                                  │
│  (10 items)│    [zoomable / scrollable playfield grid]       │
│            │                                                  │
│  ↑ next 9  │                                                  │
│            │                                                  │
│ [→ place]  │                                                  │
│            │                                                  │
└────────────┴─────────────────────────────────────────────────┘
```

- HUD height: fixed, minimal (~48 dp).
- Stack width: fixed (~180–220 dp) so each upcoming pipe preview can show both its symbol and label.
- Grid viewport: fills remaining space; the visible region is clipped while the rendered board may be larger than the viewport.
- The preparation countdown timer changes from yellow to orange to red as it approaches zero and blinks during the final 10 seconds.

## Pre-start modal

- When a released level finishes loading, gameplay pauses behind a modal card.
- The loaded playfield, including empty tiles and the left-side pipe stack, must be visible before the modal appears so the player sees the full board presentation first.
- The modal shows the level number, difficulty, flow timeout, and flow speed indicator from the downloaded level data.
- A `Start` button at the bottom of the card dismisses the modal and begins the level's pre-flow countdown.
- The board remains visible behind the modal so the player can preview the layout before starting.

---

## Flow event chain

When the start delay expires:

1. `GameSession` transitions to `FlowActive`.
2. The playfield calls `StartFlow(outputDirection, speedMultiplier)` on the start point tile.
3. The start tile raises `FlowCompleted(exitDirection, position)`.
4. The playfield identifies the adjacent cell in `exitDirection`.
5. The playfield calls `StartFlow(reciprocalEntryDirection, speedMultiplier)` on that cell's pipe control.
6. Steps 3–5 repeat until the flow reaches a finish point, exits the board, or hits an invalid connection.
7. On `FlowCompleted` from a finish point tile, the playfield marks that finish point as satisfied.
8. When all required finish points are satisfied, `GameSession` transitions to `Succeeded`.
9. When a branch hits an invalid connection or exits the board, `GameSession` transitions to `Failed`.

---

## Acceptance criteria

- All seven pipe section types are implemented as MAUI `ContentView` controls in `src/Game/HexMaster.FloodRush.Game/Controls/Pipes/`.
- Each control exposes `StartFlow(BoardDirection, double)`, raises `FlowStarted`, and raises `FlowCompleted`.
- Flow duration follows the formula: `10000ms` at speed 1, `1000ms` at speed 100, linear interpolation.
- Calling `StartFlow` sets `IsLocked = true`; locked tiles cannot be replaced.
- `FlowStarted` event arguments include `EntryDirection` and `GridPosition`.
- `FlowCompleted` event arguments include `ExitDirection` and `GridPosition`.
- The pipe placement stack shows exactly 10 items at all times.
- The gameplay page animates the initial 10 stack items from top to bottom when a level loads.
- Tapping an empty or unlocked cell on the playfield places the bottom stack item.
- After placement, the stack shifts and a new item is appended at the top.
- The start tile renders its `OutputDirection` as a visible directional indicator.
- The finish tile renders its required `EntryDirection` as a visible directional indicator.
- The layout allocates left sidebar for the stack and the remaining width for the playfield viewport.
- Randomized playfield tile backgrounds remain stable for a loaded board and the tile zoom cap prevents tiles from exceeding 128 × 128 pixels.
- When the rendered board exceeds the visible area, dragging pans across the playfield in both directions.
- Code coverage for pipe control logic remains at or above 80%.

---

## Implementation notes

- Pipe controls: `src/Game/HexMaster.FloodRush.Game/Controls/Pipes/`
  - `PipeTileBase.cs` — shared bindable properties, lock state, event definitions
  - `HorizontalPipeControl.xaml/.cs`
  - `VerticalPipeControl.xaml/.cs`
  - `CornerLeftTopPipeControl.xaml/.cs`
  - `CornerRightTopPipeControl.xaml/.cs`
  - `CornerLeftBottomPipeControl.xaml/.cs`
  - `CornerRightBottomPipeControl.xaml/.cs`
  - `CrossPipeControl.xaml/.cs`
- Pipe stack: `src/Game/HexMaster.FloodRush.Game/Controls/PipeStackControl.xaml/.cs`
- Playfield grid: `src/Game/HexMaster.FloodRush.Game/Controls/PlayfieldGridControl.xaml/.cs`
- Playfield viewport: `src/Game/HexMaster.FloodRush.Game/Controls/ZoomablePlayfieldViewport.xaml/.cs`
- Fixed tile controls: `src/Game/HexMaster.FloodRush.Game/Controls/Tiles/`
  - `StartPointTileControl.xaml/.cs`
  - `FinishPointTileControl.xaml/.cs`
  - `FluidBasinTileControl.xaml/.cs`
  - `SplitSectionTileControl.xaml/.cs`
- `GameplayViewModel` drives the overall game session; it subscribes to pipe events and forwards them to `GameSession.Tick(elapsedMs)`.
- The water animation layer is a MAUI `GraphicsView` or `SKCanvasView` (if SkiaSharp is added) overlaid on the pipe path shape.
- `BoardDirection` and `PipeSectionType` come from `HexMaster.FloodRush.Game.Core`; do not redefine them in the MAUI project.
- Keep viewport zoom and scroll math separate from flow rules so changing the visible region never changes deterministic gameplay behaviour.
