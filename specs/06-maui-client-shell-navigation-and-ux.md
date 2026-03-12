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

## Button behavior
- Show `Play` when no local play history exists.
- Show `Continue` when the user has an in-progress or resumable state.
- `Load Level` opens a released-level selection experience.
- `Settings` opens configuration and game settings.

## Core screens
- Welcome page
- Level selection page
- Gameplay page
- Pause or result overlay
- Settings page
- Optional sync status indicator

## Gameplay screen layout

The active gameplay screen uses a fixed three-zone landscape layout:

- **HUD strip** — top bar showing level name, current score, flow speed indicator, and the pre-flow countdown timer.
- **Pipe stack sidebar** — left column (~80–100 dp) showing the 10 upcoming pipe sections. The bottom item is highlighted as "next to place".
- **Playfield grid** — remaining width; the level board with fixed tiles and player-placed pipes.

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

- The gameplay board should maximize usable horizontal space.
- The player must be able to distinguish fixed tiles from placeable pipes clearly.
- Flow speed, remaining preparation time, and required finish points should be visible during play.
- Offline availability and sync state should be understandable without blocking gameplay.
- The pipe stack must always show exactly 10 upcoming items; the bottom item must be visually distinct.
- Start and finish point tiles must display directional indicators to guide pipe placement.

## Related specification
- See **spec 11** (`11-gameplay-ui-and-pipe-controls.md`) for the full pipe control API, flow animation contract, event chain, and component file locations.
- See `docs/gameplay-ui-and-pipe-controls.md` for the human-readable design reference.


## Accessibility
- Use clear color and shape differences for tile types.
- Avoid relying on color alone to communicate state.
- Ensure touch targets are practical for mobile devices in landscape mode.

## Acceptance criteria
- A new player can navigate from the welcome page into a playable level without confusion.
- Orientation behavior is enforced on supported mobile targets.
- All button styles (primary, secondary, danger) are defined in `Resources/Styles/Styles.xaml` and applied via style keys — no inline color or gradient declarations on individual pages.
- All colours are defined in `Resources/Styles/Colors.xaml` and referenced by key.
- The `Play` button text changes to `Continue` when local progress exists.
- All pages use the dark game theme (deep navy background, amber accents).
- DI wires ViewModels to pages via constructor injection with no service-locator patterns.

## Implementation notes
- MAUI project: `src/Game/HexMaster.FloodRush.Game/`
- Navigation routes: `Services/AppRoutes.cs` — all Shell routing centralised here
- Colour palette anchor: Primary `#f5b442`, Secondary `#134573`
- Button gradient brushes: `PrimaryButtonGradientBrush` and `SecondaryButtonGradientBrush` in `Colors.xaml`
- Implicit `Button` style defaults to `PrimaryButtonStyle`; use `Style="{StaticResource SecondaryButtonStyle}"` to override
- Landscape lock: Android `ScreenOrientation.Landscape` in `MainActivity`; iOS `Info.plist` landscape-only
- See `docs/maui-client-shell-and-navigation.md` for full design reference
