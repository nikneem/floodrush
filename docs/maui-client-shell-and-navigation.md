# MAUI Client Shell and Navigation

## Overview

The FloodRush MAUI client is a landscape-only .NET 10 MAUI application that uses Shell-based navigation, MVVM with constructor-injected ViewModels, and a centralized style system. All screens share a dark game theme (deep navy background, amber/gold primary accents, navy-blue secondary accents) with styles defined in one place so the visual language stays consistent as the app evolves.

## Orientation lock

The device is locked to landscape on all supported platforms:

| Platform | Mechanism |
|----------|-----------|
| Android | `ScreenOrientation = ScreenOrientation.Landscape` on `MainActivity` |
| iOS | `UISupportedInterfaceOrientations` set to `LandscapeLeft` + `LandscapeRight` only |
| macOS / Windows | Landscape-first layout; minimum width enforces horizontal aspect ratio |

## Navigation architecture

Navigation uses `Shell.Current.GoToAsync` with named routes. All routes are centralised in `Services/AppRoutes.cs`:

| Constant | Route | Page |
|----------|-------|------|
| `Welcome` | `//welcome` | `WelcomePage` (root) |
| `LevelSelection` | `levelselection` | `LevelSelectionPage` |
| `Gameplay` | `gameplay` | `GameplayPage` |
| `Settings` | `settings` | `SettingsPage` |

The `INavigationService` interface provides typed navigation methods so ViewModels never reference `Shell` directly. Routes are registered in `AppShell.xaml.cs`. Shell chrome (nav bar, tab bar, flyout) is hidden on all pages.

## Colour system

Defined in `Resources/Styles/Colors.xaml`. Two brand anchor colours drive the entire palette:

| Token | Value | Usage |
|-------|-------|-------|
| `BrandAmber` / `Primary` | `#f5b442` | Default/primary buttons, active scores, titles |
| `BrandNavy` / `Secondary` | `#134573` | Secondary buttons, HUD background |
| `AppBackground` | `#0d1b2a` | Page backgrounds |
| `SurfaceBackground` | `#1a2f45` | Cards, panels, HUD strip |
| `CardBackground` | `#1e3650` | Elevated card surfaces |
| `TextPrimary` | `#f0f0f0` | Body text |
| `TextSecondary` | `#a0b4c8` | Subtitles, secondary info |
| `TextMuted` | `#607080` | Labels, placeholders |
| `Success` | `#2ecc71` | Win state, confirmed actions |
| `Danger` | `#e74c3c` | Fail state, destructive actions |

Pre-defined gradient brushes live here too (`PrimaryButtonGradientBrush`, `SecondaryButtonGradientBrush`, etc.) so XAML styles can reference them by key.

## Button styles

All button styles are defined in `Resources/Styles/Styles.xaml`. The implicit `Button` style applies `PrimaryButtonStyle` by default so every button gets the amber gradient unless overridden.

| Style key | Gradient | Text colour | Use for |
|-----------|----------|-------------|---------|
| `PrimaryButtonStyle` | `#ffc55a` → `#e8a030` | `#1a1000` dark | Main call-to-action |
| `SecondaryButtonStyle` | `#1a5a9e` → `#134573` | White | Back, secondary actions |
| `DangerButtonStyle` | `#e74c3c` → `#c0392b` | White | Destructive actions |

All buttons: `CornerRadius=10`, `Padding="32,16"`, `FontSize=16`, `FontAttributes=Bold`, `MinimumWidthRequest=200`.

## Other shared styles

| Style key | Target | Purpose |
|-----------|--------|---------|
| `GameCardStyle` | `Border` | Elevated cards with gradient, rounded corners, shadow |
| `GamePageStyle` | `ContentPage` | Dark app background (applied implicitly) |
| `PageTitleStyle` | `Label` | 42 pt bold heading |
| `PageSubtitleStyle` | `Label` | 18 pt muted subtitle |
| `SectionHeadingStyle` | `Label` | 22 pt bold section header |
| `BodyTextStyle` | `Label` | 14 pt body text (implicit default) |
| `MutedTextStyle` | `Label` | 12 pt muted helper text |
| `StatusBadgeStyle` | `Border` | Small pill for status indicators |

## Typography system

Fonts are registered in `MauiProgram.ConfigureFonts` and referenced by alias across all styles.

| Alias | File | Usage |
|-------|------|-------|
| `Peralta` | `Resources/Fonts/Peralta-Regular.ttf` | Game title, page headings, section headings, HUD values |
| `PatrickHand` | `Resources/Fonts/PatrickHand-Regular.ttf` | All other text — subtitles, body, muted labels, button labels, inputs |

Style keys using Peralta: `GameTitleStyle`, `PageTitleStyle`, `SectionHeadingStyle`, `HudValueStyle`.  
Style keys using PatrickHand: `PageSubtitleStyle`, `BodyTextStyle`, `MutedTextStyle`, implicit `Label`, all `Button` styles, implicit `Entry`.

## Component organisation

Pages are thin orchestration shells — they provide the background, layout skeleton, and ViewModel binding. All visual sections are extracted into `ContentView` components.

| Directory | Contents |
|-----------|---------|
| `Components/Welcome/` | `GameTitleView` — animated Peralta title with amber glow; `MainMenuView` — stagger-entrance nav buttons |
| `Components/Gameplay/` | HUD strip, result banner, and other gameplay screen sections |
| `Controls/Pipes/` | Interactive pipe tile controls (one per `PipeSectionType`) |
| `Controls/Tiles/` | Fixed tile controls (start, finish, basin, split) |
| `Controls/` | `PipeStackControl`, `PlayfieldGridControl` |
| `Views/` | Full-screen overlay views (`PauseResultOverlay`) |

### Animation conventions

- All entrance animations use **`CubicOut`** easing — fast start, soft landing.
- All looping ambient animations use **`SinInOut`** easing — smooth, fatigue-free.
- Parallel animations always use `Task.WhenAll` rather than sequential `await`.
- Animation code lives in component code-behind only — pages never animate sub-elements directly.
- Use `*Async` MAUI extension methods (`FadeToAsync`, `TranslateToAsync`, etc.).

### Welcome page — `GameTitleView`

The `GameTitleView` component produces the animated game title:

- **Three stacked `Label` layers** in a `Grid` create a layered glow:
  - Outer halo: `Opacity=0.25`, `Shadow.Radius=48` — wide, soft glow
  - Mid layer: `Opacity=0.45`, `Shadow.Radius=24` — intermediate halo
  - Foreground: full `Opacity`, `Shadow.Radius=8` — crisp text
- **Breathing animation**: outer and mid layers pulse between opacity 0.20/0.35 and 0.55/0.70 on a 2.2 s `SinInOut` cycle.
- **Accent rule**: slides in from the left and fades in over 700 ms on first load.
## Screens

### Welcome page (`Pages/WelcomePage.xaml`)

Two-column landscape layout backed by the `main_screen_background.png` illustration with a directional dark overlay.

- **Left column** — `GameTitleView` component: animated "FloodRush" title in Peralta font with layered amber glow + breathing animation, tagline in PatrickHand, sliding accent rule.
- **Right column** — `MainMenuView` component: three action buttons (**Play / Continue**, **Load Level**, **Settings**) with stagger-entrance animation on load.

The `WelcomeViewModel.PlayButtonText` property returns `"Continue"` when `ILocalStateService.HasActiveProgress` is true, otherwise `"Play"`. `OnAppearing` calls `RefreshState()` so the button text updates each time the player returns to this screen.



### Level selection page (`Pages/LevelSelectionPage.xaml`)

Three-column grid of `GameCard` bordered tiles. Each card shows the level display name, difficulty label, and a **Play** button. Data is currently stubbed in `LevelSelectionViewModel.LoadLevels()`; real level loading from local cache is wired in spec 7.

### Gameplay page (`Pages/GameplayPage.xaml`)

- **HUD strip** (top row) — Level ID, prep countdown, live score, pause button.
- **Board area** — Placeholder `Border` with "Game Board" label; full tile rendering connects to the `GameSession` engine in spec 7.
- **PauseResultOverlay** — shown when `IsPaused` or `IsGameOver` is true (see below).

### Settings page (`Pages/SettingsPage.xaml`)

Two-column layout:
- **Left** — Player display name entry + save button.
- **Right** — Sound on/off, music on/off switches (amber `OnColor`), plus a Danger Zone card with **Reset Progress** button.

All settings persist via `Preferences.Default`.

### Pause/Result overlay (`Views/PauseResultOverlay.xaml`)

A `ContentView` placed over the game board. Shows either:
- **Paused** state — Resume and Quit Level buttons.
- **Game Over / Success** state — "Level Complete!" (green) or "Game Over" (red) heading, final score, Try Again and Quit to Menu buttons.

The overlay uses a semi-transparent black `BoxView` under a centred `GameCard`.

## Dependency injection

All services, ViewModels, and pages are registered in `MauiProgram.cs`:

- `INavigationService` / `NavigationService` — **singleton**
- `ILocalStateService` / `LocalStateService` — **singleton**
- All ViewModels — **transient**
- All pages — **transient**

Pages receive their ViewModel via constructor injection, which DI resolves automatically when `Shell` navigates to a registered route.

## Project layout

```
src/Game/HexMaster.FloodRush.Game/
├── Converters/        InverseBoolConverter
├── Pages/             WelcomePage, LevelSelectionPage, GameplayPage, SettingsPage
├── Services/          AppRoutes, INavigationService, NavigationService,
│                      ILocalStateService, LocalStateService
├── ViewModels/        BaseViewModel, WelcomeViewModel, LevelSelectionViewModel,
│                      GameplayViewModel, SettingsViewModel
├── Views/             PauseResultOverlay
├── Resources/
│   └── Styles/        Colors.xaml, Styles.xaml
├── Platforms/
│   ├── Android/       MainActivity (landscape lock), colors.xml
│   └── iOS/           Info.plist (landscape orientations)
├── AppShell.xaml/.cs  Routes registered here
└── MauiProgram.cs     DI registration
```
