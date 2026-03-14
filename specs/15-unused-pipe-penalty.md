# 15. Unused Pipe Penalty

## Objective

Raise the skill ceiling by penalising over-placement. When a level is completed successfully, any pipe the player placed but that was never traversed by fluid is removed from the board and subtracts **2 points** from the final score per pipe. The removal is animated so the player can see exactly which pipes were discarded, but the animation runs fast and in parallel so the end-of-level flow is not disrupted.

---

## Concept

Players often hedge by filling in extra pipes along alternative routes. The unused-pipe penalty discourages wasteful placement and rewards precise, efficient routing. The mechanic is revealed only on success — a failed level does not evaluate unused pipes.

---

## Definition of "unused pipe"

A placed pipe section is **unused** if fluid has never passed through it during the completed run. The engine tracks traversed positions in `ScoreBreakdown` (via `TraversalRecord`). Any `PlacedPipe` on the board whose `GridPosition` has no matching `TraversalRecord` at the moment the session transitions to `Succeeded` is considered unused.

Pipes locked by the engine before flow starts are still subject to the penalty if fluid never reaches them.

---

## Score model changes

### New penalty field in `ScoreBreakdown`

| Field | Type | Description |
|-------|------|-------------|
| `UnusedPipePenalty` | `int` | Total penalty points deducted (always zero or negative). Equals `–2 × count of unused pipes`. |

The existing `Total` property must include `UnusedPipePenalty` in its calculation:

```
Total = PipeScore + BasinBonus + SplitBonus + CompletionBonus + UnusedPipePenalty
```

`UnusedPipePenalty` will always be ≤ 0. The total is clamped to a minimum of 0 — a player can never score below zero.

### Penalty constant

The penalty per unused pipe is **2 points** and is a domain constant. Individual levels may not override this value in the initial implementation (reserved for future per-level scoring overrides).

---

## Engine behavior

### Trigger

The engine calculates and applies the unused-pipe penalty as part of `GameSession` success resolution — after all finish points are satisfied and the session transitions from `FlowActive` to `Succeeded`.

### Calculation steps

1. Collect all `PlacedPipe` positions from `GameBoard`.
2. Collect all traversed `GridPosition` values from `ScoreBreakdown.Traversals`.
3. The unused set = placed positions minus traversed positions.
4. Set `ScoreBreakdown.UnusedPipePenalty = –2 × |unused set|`.
5. Emit an `UnusedPipesIdentified` event (or expose via `GameSession.UnusedPipePositions`) so the ViewModel can trigger the removal animation.

### No side effects on failure

If the session ends in `Failed`, the penalty calculation is skipped entirely. Unused pipes remain on the board unchanged.

---

## MAUI client behavior

### Removal animation

When the ViewModel receives the unused-pipe positions (via the `Succeeded` phase):

1. The level-complete dialog is **not** shown immediately.
2. For each unused-pipe cell, start a **simultaneous** removal animation:
   - Fade the pipe tile out (`Opacity` 1 → 0) over **300 ms**.
   - The animation uses `Easing.CubicIn` so the fade accelerates toward the end.
3. All fade animations run in **parallel** (`Task.WhenAll` or equivalent).
4. After all animations complete, clear the pipe tile from the cell (set back to empty visual state).
5. Show the level-complete dialog once all removals have finished.

The total removal phase must complete in at most **300 ms** regardless of how many unused pipes exist, because all animations run concurrently.

### Penalty point popup

As each pipe fades out, display a transient **"–2"** label in red at the cell position. The label fades out with the pipe over the same 300 ms. This gives the player immediate per-tile feedback.

### Level-complete dialog changes

The existing level-complete dialog must be expanded to show a score breakdown:

| Row | Label | Value |
|-----|-------|-------|
| 1 | Pipe score | `+{PipeScore}` pts |
| 2 | Basin bonus | `+{BasinBonus}` pts _(hidden if 0)_ |
| 3 | Split bonus | `+{SplitBonus}` pts _(hidden if 0)_ |
| 4 | Completion bonus | `+{CompletionBonus}` pts |
| 5 | Unused pipe penalty | `–{abs(UnusedPipePenalty)}` pts _(in red; hidden if 0)_ |
| 6 | **Total** | **`{Total}`** pts (bold, larger font) |

Rows that would show `0` for their category (basin, split, penalty) are hidden to avoid visual clutter.

The **Total** row is separated from the itemised rows by a thin divider line and uses a slightly larger font size and bold weight.

---

## Acceptance criteria

- `ScoreBreakdown` has an `UnusedPipePenalty` property (int, ≤ 0) and `Total` includes it.
- `GameSession` calculates `UnusedPipePenalty` on success: `–2` per placed pipe not present in any `TraversalRecord`.
- `GameSession` exposes the set of unused pipe positions so the ViewModel can animate them.
- A failed level does not calculate or expose unused pipe positions.
- The MAUI ViewModel triggers parallel fade-out animations (300 ms) for all unused pipe cells before showing the level-complete dialog.
- Each fading pipe cell displays a transient red `–2` label during the animation.
- The level-complete dialog shows an itemised score breakdown: pipe score, optional basin/split bonus, completion bonus, optional penalty row (red), and a bold total.
- Rows with a zero value for their category (basin, split, penalty) are hidden in the dialog.
- Total is clamped to a minimum of 0.
- Unit tests cover: zero unused pipes → no penalty; N unused pipes → penalty = –2N; total clamped to 0 when penalty exceeds positive score.
