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

## UX expectations
- The gameplay board should maximize usable horizontal space.
- The player must be able to distinguish fixed tiles from placeable pipes clearly.
- Flow speed, remaining preparation time, and required finish points should be visible during play.
- Offline availability and sync state should be understandable without blocking gameplay.

## Accessibility
- Use clear color and shape differences for tile types.
- Avoid relying on color alone to communicate state.
- Ensure touch targets are practical for mobile devices in landscape mode.

## Acceptance criteria
- A new player can navigate from the welcome page into a playable level without confusion.
- Orientation behavior is enforced on supported mobile targets.
