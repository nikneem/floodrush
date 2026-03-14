# 06. MAUI Client Shell, Navigation, and UX

## Objective
Describe the .NET MAUI application shell and the minimum user experience for the first playable version.

## Orientation
- The application must run in landscape only.
- Phone and tablet orientation must be locked accordingly on supported platforms.

## Welcome page
The welcome page must contain:
- `Play` or `Continue`
- `Load Level`
- `Settings`
- `Quit`

## Button behavior
- Show `Play` when no local play history exists.
- Show `Continue` when the user has an in-progress or resumable state.
- `Load Level` opens a released-level selection experience backed by the server's released-level catalog after device login.
- Leaving the level selection page cancels any in-flight released-level refresh.
- `Settings` opens configuration and game settings.
- `Quit` opens a confirmation dialog before exiting the app, unless the player has previously opted out of that confirmation.

## Core screens
- Welcome page
- Level selection page
- Gameplay page
- Pause or result overlay
- Settings page
- Optional sync status indicator

## Gameplay screen layout

The active gameplay screen uses a fixed three-zone landscape layout:

- **HUD strip** — top bar showing: level name, pre-flow countdown timer, flow speed indicator, current score, a **Fast Fwd** toggle button, and a **Pause** button.
- **Pipe stack sidebar** — left column (~80–100 dp) showing the 10 upcoming pipe sections. The bottom item is highlighted as "next to place".
- **Playfield viewport** — remaining width; a clipped viewport that hosts the playfield grid with fixed tiles and player-placed pipes.
- Before countdown begins, the gameplay page shows a centered modal summarising the selected level number, difficulty, flow timeout, and flow speed indicator, with a `Start` button pinned to the bottom of the card.

## HUD action buttons

The HUD strip contains two interactive buttons aligned to the right:

### Fast Fwd / Normal

- Toggles fast-forward mode on and off.
- Label reads **"Fast Fwd"** when off (click to enable) and **"Normal"** when on (click to return to normal speed).
- Uses the default primary button style (amber gradient).
- Enabled whenever a level is loaded; clicking during the prep countdown has no visible effect until flow begins.
- Resets to off on level retry.
- See spec 05 for the timing definition when fast-forward is active.

### Pause

- Pauses active gameplay and shows the pause overlay.
- Uses the default primary button style.
- Disabled until a level has loaded and the pre-start modal is dismissed.

## Pause and result overlays

### Pause overlay

- Shown when the player taps **Pause** from the HUD.
- Semi-transparent full-screen overlay with a centred card.
- Card contains the title "Paused" and two buttons: **Resume** (returns to active gameplay) and **Quit Level** (returns to level selection).

### Level failed overlay

- Shown automatically when the flow fails (open pipe end or invalid connection).
- Full-bleed background visual with semi-transparent dark veil.
- Card shows: failure title ("Flow Leaked!"), message, the score accumulated during the run, a **Try Again** button (primary), and a **Quit to Menu** button (secondary).
- Score shown in the failure overlay is for display only; it is not submitted to the server.
- Tapping **Try Again** dismisses the overlay immediately and shows a loading indicator while the level resets. See the Retry loading feedback section below.

### Level complete overlay

- Shown automatically when all required finish points are reached.
- Full-bleed background visual with semi-transparent dark veil.
- Card shows: completion title ("Level Complete!"), the final score, a score submission indicator, the player's personal best, the global best score, a **Next Level** button (primary), and a **Quit to Menu** button (secondary).
- Score submission is attempted in the background; the indicator is visible while the request is in flight.

## Retry loading feedback

When the player triggers **Try Again** from the level failed overlay:

1. The failure overlay is dismissed immediately.
2. A full-screen loading overlay appears over the gameplay page showing an amber spinner and the text "Resetting level…".
3. Level tiles are rebuilt on a background thread to avoid blocking the UI.
4. Once tiles are ready, the level is applied on the main thread in a single `Reset` notification (no per-tile rebuild).
5. The loading overlay is dismissed and the pre-start modal appears for the fresh board.

This pattern ensures the player receives immediate visual confirmation that retry is in progress.

## Pipe placement stack

- The stack always contains exactly 10 items.
- The **bottom item** is placed when the player taps a valid cell.
- After placement, remaining items shift down and a new item is appended at the top.
- Items are drawn randomly from the level's `PipeInventoryRule` pool, respecting any `MaxCount` limits.
- All 10 items are pre-populated at level load, before the start countdown begins.

## Tap-to-place mechanic

- Tapping an **empty or unlocked** cell places the current bottom stack item.
- Tapping a cell with an existing (unlocked) pipe **replaces** it.
- Locked cells (flow has reached them) and fixed tiles cannot be tapped.
- The bottom stack item is consumed immediately; there is no drag-and-drop.
- The playfield viewport supports pinch-to-zoom, but individual tiles must not exceed the native 128 × 128 pixel playfield background art size.
- When the rendered playfield is larger than the visible viewport, dragging pans the viewport across the board in both directions.
- Zooming and panning must preserve the three-zone landscape layout; the player does not leave the gameplay screen to inspect the rest of the board.

- The gameplay board should maximize usable horizontal space.
- Entering gameplay for a released level downloads the current level revision from the API and renders the fixed-tile board before play begins.
- The playfield viewport should prioritize keeping the active area visible while still allowing inspection of off-screen board regions through pinch and drag gestures.
- The player must be able to distinguish fixed tiles from placeable pipes clearly.
- Flow speed, remaining preparation time, and required finish points should be visible during play.
- Offline availability and sync state should be understandable without blocking gameplay.
- The pipe stack must always show exactly 10 upcoming items; the bottom item must be visually distinct.
- Start and finish point tiles must display directional indicators to guide pipe placement.

## Related specification
- See **spec 11** (`11-gameplay-ui-and-pipe-controls.md`) for the full pipe control API, flow animation contract, event chain, and component file locations.
- See `docs/gameplay-ui-and-pipe-controls.md` for the human-readable design reference.


## Component organisation

Pages are thin orchestration shells. Visual sections must be extracted into reusable `ContentView` components.

### Directory structure

| Path | Purpose |
|------|---------|
| `Components/Welcome/` | Welcome page components (`GameTitleView`, `MainMenuView`) |
| `Components/Gameplay/` | Gameplay screen components (HUD strip, pipe stack, result banner) |
| `Controls/Pipes/` | Interactive pipe tile controls (one per `PipeSectionType`) |
| `Controls/Tiles/` | Fixed tile controls (start, finish, basin, split) |
| `Controls/` | Shared interactive controls (`PipeStackControl`, `PlayfieldGridControl`) |
| `Views/` | Full-screen overlay views (`PauseResultOverlay`) |

### Component rules

- A page must not embed more than one logical section inline; each section belongs in a `ContentView` component.
- Components own their animation logic in code-behind; pages do not animate sub-elements directly.
- Bindable properties are used when a component needs data from the outside; events are used to communicate back.
- No inline colour or gradient declarations anywhere — all via style or colour keys.
- Animation must use the `*Async` MAUI extension methods (e.g. `FadeToAsync`, `TranslateToAsync`).

## Typography

- **Heading / title elements** (`GameTitleStyle`, `PageTitleStyle`, `SectionHeadingStyle`, `HudValueStyle`): `FontFamily="Peralta"`
- **All other text** (body, subtitle, muted, button labels, inputs): `FontFamily="PatrickHand"`
- Both fonts registered in `MauiProgram.ConfigureFonts` as `"Peralta"` and `"PatrickHand"`.
- Font files live in `Resources/Fonts/Peralta-Regular.ttf` and `Resources/Fonts/PatrickHand-Regular.ttf`.

## Animation guidelines

- Entrance animations (fade + translate) use `CubicOut` easing for snappiness.
- Looping ambient animations (glow pulse, breathing) use `SinInOut` for smoothness.
- Always combine parallel animations with `Task.WhenAll` — never sequential `await` chains for simultaneous motion.
- Keep ambient loop durations between 1.5 s and 3 s per phase to avoid fatigue.
- Animation code lives in component code-behind, never in page code-behind.


- Use clear color and shape differences for tile types.
- Avoid relying on color alone to communicate state.
- Ensure touch targets are practical for mobile devices in landscape mode.

## Acceptance criteria
- A new player can navigate from the welcome page into a playable level without confusion.
- The welcome page exposes a fourth `Quit` button and confirms app exit unless the locally stored opt-out preference suppresses the dialog.
- The level selection page authenticates the device, loads released levels from the API, and lists the server-provided display name, difficulty, and speed indicator.
- Level selection loading must not block the UI thread; failures surface a visible error message and cached released levels remain available offline.
- The MAUI client emits structured logs, traces, and metrics for navigation, authentication, released-level refresh, cache fallback, gameplay level loading, quit confirmation, and settings changes.
- Orientation behavior is enforced on supported mobile targets.
- All button styles (primary, secondary, danger) are defined in `Resources/Styles/Styles.xaml` and applied via style keys — no inline color or gradient declarations on individual pages or components.
- All colours are defined in `Resources/Styles/Colors.xaml` and referenced by key.
- The `Play` button text changes to `Continue` when local progress exists.
- All pages use the dark game theme (deep navy background, amber accents).
- DI wires ViewModels to pages via constructor injection with no service-locator patterns.
- Heading/title elements use the Peralta font; all other text uses the Patrick Hand font.
- Pages delegate visual sections to `ContentView` components; no page embeds more than one logical section inline.
- Animations use `*Async` MAUI extension methods and run in component code-behind.

## Implementation notes
- MAUI project: `src/Game/HexMaster.FloodRush.Game/`
- Navigation routes: `Services/AppRoutes.cs` — all Shell routing centralised here
- Colour palette anchor: Primary `#f5b442`, Secondary `#134573`
- Button gradient brushes: `PrimaryButtonGradientBrush` and `SecondaryButtonGradientBrush` in `Colors.xaml`
- Implicit `Button` style defaults to `PrimaryButtonStyle`; use `Style="{StaticResource SecondaryButtonStyle}"` to override
- Landscape lock: Android `ScreenOrientation.SensorLandscape` in `MainActivity`; iOS `Info.plist` landscape-only
- Fullscreen: Android `WindowCompat` immersive mode; Windows `AppWindowPresenterKind.FullScreen`
- Welcome page: `GameTitleView` (animated Peralta title with amber glow) + `MainMenuView` (stagger-entrance buttons)
- See `docs/maui-client-shell-and-navigation.md` for full design reference
