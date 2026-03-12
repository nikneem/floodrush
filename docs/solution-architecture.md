# FloodRush Solution Architecture

This document is the documentation-friendly companion to `specs\02-solution-architecture.md`.

## Architecture summary
FloodRush is split into a mobile game client, a dedicated API client layer, shared contracts, and a modular monolith server.

## Project responsibilities
- `src\Game\HexMaster.FloodRush.Game` owns the .NET MAUI shell, navigation, pages, and view models.
- `src\Game\HexMaster.FloodRush.Game.Core` owns deterministic gameplay logic such as board rules, scoring, and validation.
- `src\Game\HexMaster.FloodRush.Game.Infrastructure` owns local persistence, connectivity, sync orchestration, and device services.
- `src\Game\HexMaster.FloodRush.ApiClient` owns token acquisition and all remote server communication.
- `src\Server\HexMaster.FloodRush.Api` is the public HTTP host.
- `src\Server\HexMaster.FloodRush.Server.Abstractions` owns CQRS contracts and reusable server-only helpers.
- `src\Server\HexMaster.FloodRush.Server.Profiles` owns profile registration, device login, and profile updates.
- `src\Server\HexMaster.FloodRush.Server.Levels` owns released level access.
- `src\Server\HexMaster.FloodRush.Server.Scores` owns score submission and score querying.
- `src\Shared\HexMaster.FloodRush.Shared.Contracts` owns client/server DTOs.

## Server shape
The server is a modular monolith.

- Each module has its own project.
- Each module exposes features through a `Features` namespace.
- Each feature owns its request type and its handler.
- CQRS is the default server-side implementation pattern.
- Server-only shared code stays under `src\Server`.
- Only client/server shared contracts belong in `src\Shared`.

## Storage and orchestration
- Server persistence uses Azure Table Storage.
- Local development storage is orchestrated through Aspire.
- Aspire is used to run the API host and its storage dependencies together.

## Testing expectations
- `HexMaster.FloodRush.Game.Core` must be covered by unit tests.
- The core server module projects must be covered by unit tests.
- The minimum target for those core projects is **80% code coverage**.
- Tests should focus on gameplay rules, CQRS handlers, validation, scoring, and other deterministic business logic.

## Why this matters
This structure keeps game rules testable, server modules isolated, and API communication explicit. It also protects the most important code from regressions by setting a clear expectation for automated coverage.
