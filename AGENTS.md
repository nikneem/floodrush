# AGENTS.md

## Purpose
This repository contains FloodRush, a .NET 10 solution for a landscape-only .NET MAUI game and its supporting server services. Use this file together with the numbered specs in `specs\` to guide implementation work.

The `docs\` folder contains the higher-level product framing. Read `docs\product-vision-and-scope.md` when you need the plain-language version of spec 1.

## Repository layout
- `src\Server\HexMaster.FloodRush.Api` - ASP.NET Core host for the modular monolith API.
- `src\Server\HexMaster.FloodRush.Server.Abstractions` - CQRS and shared server-only helpers.
- `src\Server\HexMaster.FloodRush.Server.Profiles` - profiles module.
- `src\Server\HexMaster.FloodRush.Server.Levels` - levels module.
- `src\Server\HexMaster.FloodRush.Server.Scores` - scores module.
- `src\Shared\HexMaster.FloodRush.Shared.Contracts` - shared client/server contracts.
- `src\Aspire\HexMaster.FloodRush.Aspire` - local orchestration and service defaults.
- `src\Game\HexMaster.FloodRush.Game` - .NET MAUI client application.
- `src\Game\HexMaster.FloodRush.Game.Core` - platform-agnostic game domain and engine.
- `docs` - human-friendly product and project documentation derived from the numbered specs.
- `specs` - ordered product and engineering specifications.

## MAUI client guidance
- All colours live in `Resources/Styles/Colors.xaml`; reference by key — never hardcode colours on pages.
- All element styles (buttons, labels, cards, pages) live in `Resources/Styles/Styles.xaml`.
- Primary button style: amber gradient `#ffc55a` → `#e8a030`; style key `PrimaryButtonStyle` (implicit default for all buttons).
- Secondary button style: navy gradient `#1a5a9e` → `#134573`; style key `SecondaryButtonStyle`.
- Danger button style: red gradient `#e74c3c` → `#c0392b`; style key `DangerButtonStyle`.
- Navigation is centralised in `Services/AppRoutes.cs` and `Services/INavigationService`; ViewModels must not reference `Shell` directly.
- All pages receive their ViewModel via DI constructor injection; register in `MauiProgram.cs`.
- `Shell.NavBarIsVisible="False"` and `Shell.TabBarIsVisible="False"` on all pages — no visible shell chrome.
- The device is locked to landscape: Android via `ScreenOrientation.Landscape`, iOS via `Info.plist`.
- Target .NET 10 and the latest C# version for all new projects unless a spec states otherwise.
- Keep the MAUI client landscape-only.
- Treat the client as offline-first. Local play must remain functional when the server is unavailable.
- Preserve deterministic game rules. Core flow and scoring logic should be UI-agnostic and easy to test.
- Keep contracts explicit between client, server, and persistence layers.
- Keep server-only code inside `src\Server` and only place client/server shared contracts or logic inside `src\Shared`.
- Use feature slices with CQRS in server modules: each feature namespace owns its request and handler.
- Preserve the scope defined in `docs\product-vision-and-scope.md`: single-player puzzle gameplay first, no multiplayer, no level editor, and no monetization features for the initial product.

## Implementation priorities
1. Shared domain model and level schema.
2. Core gameplay engine and scoring.
3. Local persistence and synchronization queue.
4. Server APIs for profiles, level release, level download, scores, and settings.
5. MAUI screens, navigation, and platform integration.

## Gameplay vocabulary
- **Start point**: where fluid begins after a delay.
- **Finish point**: a required destination for a successful path.
- **Flow speed indicator**: integer from 1 to 100 that drives simulation timing.
- **Cross section**: can be traversed twice, once per axis, with bonus scoring on the second traversal.
- **Fluid basin**: fixed tile that fills before continuing flow and awards bonus points.
- **Split section**: fixed tile that branches flow, can require multiple finish points, and applies downstream speed modification.

## Coding expectations
- Put domain rules in reusable libraries or services, not directly in pages or controllers.
- Prefer immutable records or clearly bounded entities for level and score contracts.
- Use pragmatic DDD for the game domain: explicit domain types, public getters, private setters, and validated `Set{Property}` methods where mutation is needed.
- Validate level data aggressively, especially coordinates, connection compatibility, and multi-finish requirements.
- Keep sync behavior idempotent on the server and conflict-aware on the client.
- Avoid hidden behavior in UI code; document non-obvious rules in specs and tests.
- Keep HTTP endpoints thin in the API host and push behavior into module feature handlers.
- Use Azure Table Storage as the server persistence default and wire local storage dependencies through Aspire.

## Aspire MCP workflow
- Always start the AppHost with `aspire run --detach` from the workspace root. Starting with `dotnet run` directly bypasses the Aspire CLI state registration that `aspire agent mcp` depends on for discovery.
- After starting a new AppHost (detached), call `aspire-list_apphosts` before using any other MCP tools to confirm the new instance is registered and selected. Do not assume the MCP is immediately ready.
- If `aspire agent mcp` fails to find any AppHost after a fresh start, stale backchannel socket files in `~\.aspire\cli\backchannels\` are the most likely cause. Each dead `aux.sock.*` file forces a connection timeout; with many stale files the MCP appears broken. Delete them with `Remove-Item "$env:USERPROFILE\.aspire\cli\backchannels\*" -Force` when no AppHost is running, then restart the MCP server in VS Code.
- On Windows, Unix domain socket files under `~\.aspire\cli\backchannels\` are NOT cleaned up automatically when an AppHost is killed (vs. stopped gracefully). This is a known Aspire CLI limitation — stale files accumulate over multiple detach-and-kill cycles.
- The VS Code MCP server entry for Aspire uses `--nologo --non-interactive` (see `.vscode/mcp.json`) to prevent startup banners and interactive spinners from corrupting the stdio protocol channel.

## Observability expectations
- Treat logging, tracing, and metrics as first-class behavior for both the server and the MAUI client.
- Server projects should keep using the shared Aspire service-defaults OpenTelemetry configuration rather than duplicating exporter setup in each module.
- The MAUI client should configure OpenTelemetry centrally in `MauiProgram.cs`, use a shared app `ActivitySource` and `Meter`, and emit telemetry for navigation, auth, level loading, caching, quit flow, and settings changes.
- Prefer structured `ILogger` messages with stable property names so the Aspire dashboard can filter and correlate client and server events.
- AppHost resources that should show up in the Aspire dashboard must opt in with `WithOtlpExporter()`, including executable resources such as the MAUI client.

## Documentation expectations
- Keep `docs\` in sync with the numbered specs when product framing changes.
- If implementation changes the intended behavior, update the matching file in `specs\`.
- Keep new specs numbered and aligned with implementation order.
- Use concise acceptance criteria so future contributors can translate specs into tests.

## Git commit workflow

After each **logical step** — a feature slice, bug fix, test addition, documentation change, or configuration update where the codebase is internally consistent — commit using the **GitHub MCP** (`github-push_files` for multi-file changes, `github-create_or_update_file` for a single file).

Before the first commit in any session, resolve the current branch and repo:
```powershell
git --no-pager branch --show-current
git remote get-url origin   # → owner nikneem, repo floodrush
```

Commit messages must follow conventional commits and always end with the Co-authored-by trailer:
```
<type>(<scope>): <short imperative summary>

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Types: `feat` · `fix` · `test` · `refactor` · `docs` · `ci` · `chore` · `perf`  
Scopes: `game-core` · `server-profiles` · `server-levels` · `server-scores` · `api` · `maui` · `aspire` · `shared` · `ci` · `docs`

Do **not** commit partial or broken work. Complete the logical unit first, then commit. See `.github/copilot-instructions.md` for the full commit workflow details.
