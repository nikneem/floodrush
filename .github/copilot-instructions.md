# FloodRush Copilot Instructions

## Product summary
FloodRush is a landscape-only .NET MAUI puzzle game built on .NET 10 and the latest C# language version. Players place pipe sections on a grid to connect one or more start points to one or more finish points before fluid reaches an open end.

The solution also includes a server that manages user profiles, released levels, level downloads, score submission, and synchronization of offline progress.

Read `docs\product-vision-and-scope.md` for the plain-language product framing and `specs\01-product-vision-and-scope.md` for the implementation-oriented version.

## Current repository shape
- `src\Game\HexMaster.FloodRush.Game` contains the .NET MAUI client application.
- `src\Game\HexMaster.FloodRush.Game.Core` contains the platform-agnostic game domain and engine.
- `src\Server\HexMaster.FloodRush.Api` contains the ASP.NET Core modular monolith host.
- `src\Server\HexMaster.FloodRush.Server.Abstractions` contains CQRS interfaces and shared server-only helpers.
- `src\Server\HexMaster.FloodRush.Server.Profiles` contains the profiles module and device authentication flow.
- `src\Server\HexMaster.FloodRush.Server.Levels` contains the levels module.
- `src\Server\HexMaster.FloodRush.Server.Scores` contains the scores module.
- `src\Shared\HexMaster.FloodRush.Shared.Contracts` contains client/server shared contracts.
- `src\Aspire\HexMaster.FloodRush.Aspire` contains local orchestration projects.
- `src\Game` exists in the solution as a placeholder folder and is the intended home for the .NET MAUI client.
- `docs` contains product and project documentation derived from the numbered specs.
- `specs` contains the implementation specifications and is the source of truth for product behavior until code catches up.

## Preferred implementation order
1. Establish shared domain models and contracts from the numbered specs.
2. Build the game board, pipe placement rules, and flow simulation engine.
3. Add local persistence and offline-first synchronization behavior.
4. Expand the API for profiles, levels, release gating, and score submission.
5. Implement MAUI screens, navigation, and settings around the domain model.

## Gameplay rules to preserve
- The board is a grid with explicit start and finish points.
- Flow starts after a level-defined delay.
- Placeable pipe types are horizontal, vertical, corner left-to-top, corner right-to-top, corner left-to-bottom, corner right-to-bottom, and cross.
- Each pipe type has its own score value when traversed by fluid.
- A cross can score twice: once for vertical traversal and once for horizontal traversal, with the second traversal awarding bonus points.
- Fixed level tiles include a fluid basin and a split section.
- A fluid basin delays downstream flow while filling and grants bonus points.
- A split section creates multiple active flows, can require multiple finish points, and applies a speed modifier only to downstream segments on split branches.
- Each level defines a base flow speed indicator from 1 to 100.

## MAUI styling rules
- All colours must be defined in `Resources/Styles/Colors.xaml` as named resources and referenced by `{StaticResource}` key. Never hardcode hex values on pages or views.
- All element styles (buttons, labels, cards, pages) must be defined in `Resources/Styles/Styles.xaml`.
- Default button (implicit style) → amber gradient primary. Override with `Style="{StaticResource SecondaryButtonStyle}"` or `DangerButtonStyle`.
- New pages must use `Shell.NavBarIsVisible="False"` and be registered as a route in `AppShell.xaml.cs` and `AppRoutes.cs`.
- New ViewModels must extend `BaseViewModel` and be registered as transient in `MauiProgram.cs`; pages are also transient.
- ViewModels must navigate via `INavigationService` injection — no direct `Shell.Current` usage in ViewModels.


- The MAUI client must run in landscape only.
- The client is offline-first: gameplay, settings, and cached levels must continue to work without connectivity.
- When connectivity is available, local state syncs with the server without losing offline progress.
- The welcome screen exposes `Play` or `Continue`, `Load Level`, and `Settings`.
- `Play` changes to `Continue` when local progress already exists.
- `Load Level` must only show levels released to the signed-in or local player profile.
- The initial product stays focused on single-player puzzle gameplay and explicitly excludes multiplayer, user-generated levels, in-game editing, social systems, and monetization.

## Server expectations
- Favor explicit contracts and versionable DTOs.
- Keep API endpoints aligned with the offline sync model: profile sync, level catalog sync, level download, score upload, and settings sync.
- Treat release state and score integrity as server-owned concerns.
- Implement the server as a modular monolith with one project per module.
- Use feature slices and CQRS in server modules: each feature namespace should own its command or query and its handler.
- Keep server-only shared code in `src\Server`; keep only client/server shared contracts in `src\Shared`.
- Use Azure Table Storage for server persistence and orchestrate local development storage through Aspire.

## Observability expectations
- Both the server and the MAUI client must emit structured logs through `ILogger` and OpenTelemetry so local connected runs can be inspected in the Aspire dashboard.
- Keep server observability wired through `src\Aspire\HexMaster.FloodRush.Aspire\HexMaster.FloodRush.Aspire.ServiceDefaults`; do not add one-off telemetry configuration in individual server modules unless a spec requires it.
- Keep MAUI OpenTelemetry wiring centralised in `MauiProgram.cs`, and add app-specific spans/metrics through the shared game telemetry source instead of ad hoc telemetry objects.
- Instrument important player and sync flows explicitly: navigation, device login, released-level refresh, cache fallback, gameplay level loading, quit confirmation, and settings changes.
- When a resource is started from the AppHost and should appear in the Aspire dashboard, ensure the AppHost resource uses `WithOtlpExporter()`.

## Coding guidance
- Reuse shared domain types instead of duplicating board or scoring rules.
- Keep flow simulation deterministic and testable without UI dependencies.
- Separate game engine logic from MAUI views and platform-specific services.
- Use pragmatic DDD in the core game domain: prefer explicit domain models with public getters, private setters, and validated `Set{Property}` methods instead of loose bags of primitives.
- Model offline data with sync metadata such as timestamps, version tokens, and pending operations.
- Prefer small, composable services over large manager classes.
- Maintain at least 80% code coverage for the core projects: `HexMaster.FloodRush.Game.Core`, `HexMaster.FloodRush.Server.Profiles`, `HexMaster.FloodRush.Server.Levels`, and `HexMaster.FloodRush.Server.Scores`.

## When adding code
- Read the relevant numbered spec first and implement to the spec instead of inventing behavior.
- Check `docs\` when you need product context or contributor-facing documentation updates.
- Update the spec when behavior intentionally changes.
- Add or update unit tests for core logic and keep the 80% coverage target intact for the core projects.
- Keep user-visible text and terminology consistent: use `fluid basin`, `split section`, `finish point`, and `flow speed indicator`.

## Git commit workflow

After each **logical step** — a self-contained unit of work where the codebase compiles and is internally consistent — commit the changes using the GitHub MCP.

### What counts as a logical step
- Implementing a complete feature slice (command + handler + registration)
- Adding or updating a set of tests for a feature
- Fixing a bug (source fix + any test update together)
- Adding or updating documentation or specs
- Changing configuration, CI, or tooling
- A refactor that leaves behaviour identical

Do **not** commit partial work (broken build, unfinished handler, test that fails by design mid-task). Complete the logical unit first, then commit.

### How to commit with the GitHub MCP

1. **Resolve the current branch and repo** before the first commit in any session:
   ```powershell
   git --no-pager branch --show-current
   git remote get-url origin
   ```
   Parse `owner` and `repo` from the remote URL (e.g. `https://github.com/nikneem/floodrush` → owner `nikneem`, repo `floodrush`).

2. **Use `github-push_files`** to push all files changed in the logical step as a single commit:
   - `owner` and `repo` from step 1
   - `branch` from step 1
   - `files` — every file created or modified in this step (use exact repo-relative paths with forward slashes)
   - `message` — a conventional commit message (see format below)

3. **Use `github-create_or_update_file`** only when exactly one file changed.  
   Provide the `sha` of the file being replaced; obtain it with:
   ```powershell
   git rev-parse HEAD:<repo-relative/path/to/file>
   ```

### Commit message format

```
<type>(<scope>): <short imperative summary>

<optional body — what changed and why>

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

| Type | When to use |
|---|---|
| `feat` | New behaviour visible to users or callers |
| `fix` | Bug correction |
| `test` | Adding or updating tests only |
| `refactor` | Internal restructure with no behaviour change |
| `docs` | Documentation or spec updates |
| `ci` | CI/CD or workflow changes |
| `chore` | Config, tooling, dependencies |
| `perf` | Performance improvement |

**Scope** should be the short project or layer name: `game-core`, `server-profiles`, `server-levels`, `server-scores`, `api`, `maui`, `aspire`, `shared`, `ci`, `docs`.

Example:
```
feat(server-levels): add released-level catalog endpoint

Adds GET /levels returning only levels released to the authenticated
player profile. Handler filters by ReleaseState and PlayerTier from
Azure Table Storage.

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
