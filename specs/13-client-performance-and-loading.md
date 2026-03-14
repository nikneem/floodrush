# 13. Client Performance and Loading

## Objective

Define the performance requirements and loading patterns for the MAUI client, focusing on level loading, board construction, and visual feedback obligations during potentially slow operations.

---

## Level loading

### Background tile construction

Building a level board requires constructing one `PlayfieldTileItem` per grid cell, assigning background images, and resolving fixed tile types. For large boards this work must not block the UI thread.

**Required pattern:**

1. Level data is resolved (from server or cache) on the calling task.
2. `BuildTiles(levelRevision)` runs on a background thread via `Task.Run`.
3. Once the pre-built tile collection is ready, `ApplyLevel` is called back on the main thread.
4. `ApplyLevel` applies the tiles to the ViewModel using a single `Reset` notification (see below).

This ensures the "level is loading" overlay remains animated and responsive during the construction step.

### Single-reset collection update

Replacing all tiles in an `ObservableCollection<T>` one at a time causes N `Add` notifications, each of which triggers a full grid rebuild. On a 20 × 20 board this produces 400 rebuilds and roughly 80 000 tile view objects — enough to create a multi-minute hang.

**Required pattern:** Use `BatchObservableCollection<T>.ResetTo(IEnumerable<T>)`, which:
1. Clears and repopulates the backing `Items` list directly.
2. Fires exactly **one** `NotifyCollectionChangedAction.Reset` notification.
3. Triggers a single board rebuild regardless of board size.

`BatchObservableCollection<T>` extends `ObservableCollection<T>` and lives in the MAUI client project. Both `BoardTiles` and `UpcomingPipes` in `GameplayViewModel` must use this type.

---

## Retry loading feedback

When the player triggers retry after a level failure, the failure overlay closes immediately and a loading overlay appears. There must be no gap where the screen appears frozen.

### Required sequence

1. **Failure overlay dismissed** — `IsGameOver = false` is set first so the overlay hides at once.
2. **Loading overlay shown** — `IsRetrying = true` displays the "Resetting level…" spinner immediately.
3. **Background tile build** — `BuildTiles` runs on a background thread while the spinner is visible.
4. **Main-thread apply** — `ApplyLevel` (with the pre-built tiles) is called on the main thread.
5. **Loading overlay hidden** — `IsRetrying = false` is set inside `ApplyLevel` after collection update.
6. **Fast-forward reset** — `IsFastForward` is reset to `false` during the retry so players always begin the fresh attempt at normal speed.

### Loading overlay visual

The loading overlay is a full-screen translucent layer (88% opacity) that covers the gameplay board. It contains:
- An amber `ActivityIndicator` (running while `IsRetrying = true`).
- A centred label: "Resetting level…"

The overlay must not be dismissible by the player; it disappears automatically when the level is ready.

---

## Pre-built tiles

`BuildTiles(LevelRevision)` is a pure function that takes level data and returns a ready-to-use `IReadOnlyCollection<PlayfieldTileItem>`. It has no MAUI UI dependencies and can run safely on any thread.

The overload `ApplyLevel(ReleasedLevel, LevelRevision, IReadOnlyCollection<PlayfieldTileItem>)` accepts pre-built tiles instead of building them on the main thread.

---

## Performance targets

| Operation | Target |
|-----------|--------|
| Level load (board construction) | ≤ 1 second for any board up to 30 × 30 |
| Retry reset (tile rebuild) | ≤ 1 second; spinner visible immediately |
| Collection `Reset` notification handling | Single rebuild regardless of tile count |
| Tile view creation total (load or retry) | N views, not N² |

---

## Acceptance criteria

- Loading and retry tile builds run on a background thread and never block the UI thread.
- `BoardTiles` and `UpcomingPipes` are `BatchObservableCollection<T>` instances; `ResetTo` is used for all bulk updates.
- Tapping **Try Again** shows the "Resetting level…" spinner within one UI frame (before any background work begins).
- `IsFastForward` is `false` after a retry.
- A 20 × 20 board loads without visible jank; the loading overlay is animated throughout.
- Unit tests for `BuildTiles` run without a MAUI host or UI thread.

---

## Implementation notes

- `BatchObservableCollection<T>`: `src/Game/HexMaster.FloodRush.Game/ViewModels/BatchObservableCollection.cs`
- `GameplayViewModel.BuildTiles`: pure method in `GameplayViewModel.cs`; accepts a `LevelRevision` and returns `IReadOnlyCollection<PlayfieldTileItem>`
- `GameplayViewModel.ApplyLevel` overload: accepts pre-built tiles; calls `BoardTiles.ResetTo(...)` and `UpcomingPipes.ResetTo(...)`
- `IsRetrying` binding drives both the loading overlay visibility and the `ActivityIndicator.IsRunning` property
