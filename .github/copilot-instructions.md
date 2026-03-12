# FloodRush Copilot Instructions

## Product summary
FloodRush is a landscape-only .NET MAUI puzzle game built on .NET 10 and the latest C# language version. Players place pipe sections on a grid to connect one or more start points to one or more finish points before fluid reaches an open end.

The solution also includes a server that manages user profiles, released levels, level downloads, score submission, and synchronization of offline progress.

## Current repository shape
- `src\Server\HexMaster.FloodRush.Api` contains the ASP.NET Core API.
- `src\Aspire\HexMaster.FloodRush.Aspire` contains local orchestration projects.
- `src\Game` exists in the solution as a placeholder folder and is the intended home for the .NET MAUI client.
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

## Client expectations
- The MAUI client must run in landscape only.
- The client is offline-first: gameplay, settings, and cached levels must continue to work without connectivity.
- When connectivity is available, local state syncs with the server without losing offline progress.
- The welcome screen exposes `Play` or `Continue`, `Load Level`, and `Settings`.
- `Play` changes to `Continue` when local progress already exists.
- `Load Level` must only show levels released to the signed-in or local player profile.

## Server expectations
- Favor explicit contracts and versionable DTOs.
- Keep API endpoints aligned with the offline sync model: profile sync, level catalog sync, level download, score upload, and settings sync.
- Treat release state and score integrity as server-owned concerns.

## Coding guidance
- Reuse shared domain types instead of duplicating board or scoring rules.
- Keep flow simulation deterministic and testable without UI dependencies.
- Separate game engine logic from MAUI views and platform-specific services.
- Model offline data with sync metadata such as timestamps, version tokens, and pending operations.
- Prefer small, composable services over large manager classes.

## When adding code
- Read the relevant numbered spec first and implement to the spec instead of inventing behavior.
- Update the spec when behavior intentionally changes.
- Add tests for game rules, scoring, branching flow, offline sync logic, and API contracts.
- Keep user-visible text and terminology consistent: use `fluid basin`, `split section`, `finish point`, and `flow speed indicator`.
