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
│  PIPE      │               PLAYFIELD GRID                    │
│  STACK     │                                                  │
│  (10 items)│   fixed tiles  +  player-placed pipes           │
│            │                                                  │
│  ↑ queued  │                                                  │
│            │                                                  │
│ [→ place]  │                                                  │
│            │                                                  │
└────────────┴─────────────────────────────────────────────────┘
```

- **HUD strip** — fixed height (~48 dp): level name, current score, flow speed indicator, and the pre-flow countdown.
- **Pipe stack sidebar** — fixed width (~80–100 dp): 10 upcoming pipe sections, stacked vertically.
- **Playfield grid** — fills remaining space: the level board with all tiles.

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

- Pipe body rendered **semi-transparent** (opacity ~0.80) so the water animation layer below is visible through it.
- Water fill is a separate animated layer (opacity ~0.70, blue-teal gradient) that sweeps from the entry edge to the exit edge along the pipe path.
- On lock (flow started), a subtle border highlight communicates the tile is now immovable.

### Flow timing

Flow duration scales linearly between speed 1 (10 000 ms) and speed 100 (1 000 ms):

```
durationMs = 10000 - ((speedMultiplier - 1) / 99.0 × 9000)
```

Speed 50 ≈ 5 455 ms. Speed 100 = 1 000 ms.

### Control API

```csharp
// Bindable properties
PipeSectionType PipeType      // determines geometry rendered
BoardDirection  EntryDirection // which side flow enters
BoardDirection  ExitDirection  // which side flow exits
bool            IsLocked       // true once StartFlow has been called
double          SpeedMultiplier // 1–100

// Method
void StartFlow(BoardDirection entryDirection, double speedMultiplier);

// Events
event EventHandler<FlowStartedEventArgs>   FlowStarted;
event EventHandler<FlowCompletedEventArgs> FlowCompleted;
```

### Event arguments

```csharp
public class FlowStartedEventArgs : EventArgs
{
    public BoardDirection EntryDirection { get; }
    public GridPosition   Position       { get; }
}

public class FlowCompletedEventArgs : EventArgs
{
    public BoardDirection ExitDirection { get; }
    public GridPosition   Position      { get; }
}
```

### Lock behaviour

| State | Can be replaced? | Visual |
|-------|-----------------|--------|
| Empty | — (no tile here) | Dark cell |
| Placed (unlocked) | Yes | Pipe rendered normally |
| Placed (locked) | **No** | Pipe with lock highlight |

---

## Playfield grid

The grid renders the full level board. Each cell maps to a `GridPosition`.

### Cell states

| State | Tappable | Content |
|-------|----------|---------|
| Empty | Yes | Faint border, dark fill |
| Occupied, unlocked | Yes (replace) | Pipe control |
| Occupied, locked | No | Pipe control + lock indicator |
| Fixed tile | No | Fixed tile control |
| Start point | No | Start tile with exit-direction arrow |
| Finish point | No | Finish tile with required-entry indicator |

### Tap-to-place flow

1. Player taps an empty or unlocked cell.
2. The playfield pops the bottom item from the pipe stack.
3. A pipe control of that type is instantiated with `EntryDirection` / `ExitDirection` calculated from the pipe's geometry and the cell position.
4. The pipe control is placed into the grid cell.
5. The stack shifts; a new item is generated and appended to the top.

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

### Fluid basin

- Rendered with a container/reservoir icon.
- Shows a fill animation during the basin delay.
- Grants bonus score on success.

### Split section

- Rendered with a fork icon.
- Creates two downstream flow branches when reached.
- Applies its `SpeedModifierPercent` to all downstream segments.

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
