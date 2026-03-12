# AGENTS.md

## Purpose
This repository contains FloodRush, a .NET 10 solution for a landscape-only .NET MAUI game and its supporting server services. Use this file together with the numbered specs in `specs\` to guide implementation work.

## Repository layout
- `src\Server\HexMaster.FloodRush.Api` - ASP.NET Core API for profiles, levels, scores, and sync.
- `src\Aspire\HexMaster.FloodRush.Aspire` - local orchestration and service defaults.
- `src\Game` - intended location for the MAUI client and related game projects.
- `specs` - ordered product and engineering specifications.

## Working agreements
- Target .NET 10 and the latest C# version for all new projects unless a spec states otherwise.
- Keep the MAUI client landscape-only.
- Treat the client as offline-first. Local play must remain functional when the server is unavailable.
- Preserve deterministic game rules. Core flow and scoring logic should be UI-agnostic and easy to test.
- Keep contracts explicit between client, server, and persistence layers.

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
- Validate level data aggressively, especially coordinates, connection compatibility, and multi-finish requirements.
- Keep sync behavior idempotent on the server and conflict-aware on the client.
- Avoid hidden behavior in UI code; document non-obvious rules in specs and tests.

## Documentation expectations
- If implementation changes the intended behavior, update the matching file in `specs\`.
- Keep new specs numbered and aligned with implementation order.
- Use concise acceptance criteria so future contributors can translate specs into tests.
