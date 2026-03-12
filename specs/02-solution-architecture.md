# 02. Solution Architecture

## Objective
Define the technical structure for the FloodRush client, server, and shared domain code.

## Proposed solution structure
- `src\Game\HexMaster.FloodRush.Game` - .NET MAUI application shell, pages, view models, and platform integration.
- `src\Game\HexMaster.FloodRush.Game.Core` - gameplay engine, board rules, scoring, and level validation.
- `src\Game\HexMaster.FloodRush.Game.Infrastructure` - local persistence, sync queue coordination, connectivity services, and device configuration.
- `src\Game\HexMaster.FloodRush.ApiClient` - all server communication, including device login, JWT acquisition and refresh strategy, and typed API access for profiles, levels, scores, and settings.
- `src\Server\HexMaster.FloodRush.Api` - ASP.NET Core API for profiles, levels, scores, and settings.
- `src\Shared\HexMaster.FloodRush.Contracts` - DTOs shared across client and server.

## Architectural rules
- UI code must not contain core flow or scoring logic.
- Core simulation must be deterministic and runnable in pure unit tests.
- API contracts should be stable and versionable.
- API communication must be routed through `HexMaster.FloodRush.ApiClient` rather than directly from MAUI pages or view models.
- Local persistence models may differ from DTOs, but mapping must be explicit.
- Offline-first behavior is a first-class requirement, not a fallback.

## Key subsystems
- Board and tile model
- Flow simulation engine
- Score calculation engine
- Level catalog and release service
- Local storage and sync queue
- Dedicated API client for authentication and remote operations
- Profile and settings service
- Server release and leaderboard pipeline

## Cross-cutting concerns
- Serialization compatibility for level data
- Versioning for levels and sync payloads
- Conflict handling for profile, settings, and score sync
- Observability for API failures and sync retries

## Acceptance criteria
- Shared contracts capture level, profile, settings, and score concepts.
- Core engine can run without MAUI dependencies.
- The client has a dedicated project responsible for token acquisition and all authenticated server calls.
- Server and client can evolve independently behind explicit contracts.
