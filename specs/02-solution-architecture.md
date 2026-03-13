# 02. Solution Architecture

## Objective
Define the technical structure for the FloodRush client, server, and shared domain code.

## Proposed solution structure
- `src\Game\HexMaster.FloodRush.Game` - .NET MAUI application shell, pages, view models, and platform integration.
- `src\Game\HexMaster.FloodRush.Game.Core` - gameplay engine, board rules, scoring, and level validation.
- `src\Game\HexMaster.FloodRush.Game.Infrastructure` - local persistence, sync queue coordination, connectivity services, and device configuration.
- `src\Game\HexMaster.FloodRush.ApiClient` - all server communication, including device login, JWT acquisition and refresh strategy, and typed API access for profiles, levels, scores, and settings.
- `src\Server\HexMaster.FloodRush.Api` - ASP.NET Core host for the modular monolith and the public HTTP surface.
- `src\Server\HexMaster.FloodRush.Server.Abstractions` - CQRS abstractions, server claim helpers, and server-wide constants used by multiple modules.
- `src\Server\HexMaster.FloodRush.Server.Profiles` - profiles module with feature-sliced commands, queries, handlers, JWT device login, and profile persistence.
- `src\Server\HexMaster.FloodRush.Server.Levels` - levels module with feature-sliced queries and handlers for released level access.
- `src\Server\HexMaster.FloodRush.Server.Scores` - scores module with feature-sliced commands, queries, and handlers for score submission and top scores.
- `src\Shared\HexMaster.FloodRush.Shared.Contracts` - DTOs shared across client and server.
- `src\Aspire\HexMaster.FloodRush.Aspire` - local orchestration that runs Azure Storage, the API host, and the MAUI client together for development.

## Architectural rules
- UI code must not contain core flow or scoring logic.
- Core simulation must be deterministic and runnable in pure unit tests.
- API contracts should be stable and versionable.
- API communication must be routed through `HexMaster.FloodRush.ApiClient` rather than directly from MAUI pages or view models.
- The server is a modular monolith: each module owns its own project under `src\Server`.
- Each server module exposes features through a `Features` namespace organized by feature slice.
- Each feature slice uses CQRS and contains the request type (`Command` or `Query`) plus its corresponding handler.
- Server-only reusable logic stays under `src\Server`; only client/server shared logic belongs under `src\Shared`.
- Local persistence models may differ from DTOs, but mapping must be explicit.
- Offline-first behavior is a first-class requirement, not a fallback.
- Connected local development should surface server and MAUI telemetry in Aspire through OpenTelemetry exporters and consistent resource naming.

## Key subsystems
- Board and tile model
- Flow simulation engine
- Score calculation engine
- Level catalog and release service
- Local storage and sync queue
- Dedicated API client for authentication and remote operations
- Modular server host and module composition
- Azure Table Storage persistence orchestrated with Aspire
- Profile and settings service
- Server release and leaderboard pipeline
- Local multi-process orchestration for the API and MAUI client

## Testing architecture
- `src\Game\HexMaster.FloodRush.Game.Core` must be covered by unit tests.
- Each server module project under `src\Server` that contains business behavior must be covered by unit tests, especially `HexMaster.FloodRush.Server.Profiles`, `HexMaster.FloodRush.Server.Levels`, and `HexMaster.FloodRush.Server.Scores`.
- Unit tests should validate deterministic rules, CQRS handlers, validation logic, mapping logic, and storage-facing behavior that can be isolated behind abstractions.
- UI shell code, Aspire orchestration, and thin host wiring may use lighter test strategies, but the core business projects must not rely on manual testing alone.
- Aspire AppHost changes should still be validated by building and running the AppHost to confirm the intended resources start together.
- The core projects should maintain a minimum of 80% code coverage.

## Cross-cutting concerns
- Serialization compatibility for level data
- Versioning for levels and sync payloads
- Conflict handling for profile, settings, and score sync
- Observability for API failures and sync retries
- Shared OpenTelemetry conventions for server modules and the MAUI client
- Consistent feature-slice conventions across modules
- Clear boundaries between host, modules, and shared contracts
- Sustainable automated coverage for core logic

## Acceptance criteria
- Shared contracts capture level, profile, settings, and score concepts.
- Core engine can run without MAUI dependencies.
- The client has a dedicated project responsible for token acquisition and all authenticated server calls.
- The server modules compile independently behind a single API host.
- Azure Table Storage is the persistence model used by server modules in local development through Aspire orchestration.
- Running the Aspire AppHost starts the API host, its local storage dependencies, and the MAUI client together for development on supported platforms.
- Running the Aspire AppHost also wires OpenTelemetry export for both the API and the MAUI client so traces, metrics, and logs appear in the Aspire dashboard during connected development.
- The game core and server modules have automated unit tests with an 80% minimum code coverage target.
- Server and client can evolve independently behind explicit contracts.
