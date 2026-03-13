# 10. Settings, Quality, and Delivery

## Objective
Capture the operational requirements that support a stable first release.

## Settings
At minimum, settings should support:
- Audio preferences
- Visual or accessibility preferences
- Gameplay preferences that do not affect competitive score integrity
- Connectivity or sync preferences where appropriate

## Quality requirements
- Core engine rules need automated unit coverage.
- API contracts need integration coverage.
- Level validation needs automated tests for malformed boards and unreachable goals.
- Sync behavior needs tests for offline completion followed by reconnect.

## Diagnostics
- Log sync failures with actionable details.
- Log server validation failures without exposing sensitive data.
- Keep client diagnostics lightweight and suitable for mobile devices.
- Use OpenTelemetry for both the API and the MAUI client so local connected runs emit logs, traces, and metrics into the Aspire dashboard.
- AppHost-managed API and MAUI resources must opt into OTLP export so the dashboard can correlate client and server behavior during local development.

## Delivery notes
- The first usable milestone should produce a playable offline level with local save and deterministic scoring.
- Online sync and release gating should be added without breaking offline progress.
- Aspire may orchestrate local development services, the API host, and the MAUI client together during local development, but gameplay logic must remain runnable outside Aspire.
- Released levels downloaded from the server should be cached locally so the player can reopen and play them without connectivity.

## Local development orchestration
- The Aspire AppHost should be the default local developer entry point for connected scenarios.
- On supported desktop development environments, starting the AppHost should launch both the API and the MAUI client so client and server work can be exercised together.
- Local orchestration must not become a hard runtime dependency for the offline-first client; the MAUI app must still be able to run independently when needed.
- The Aspire dashboard may expose local-only custom commands for operational developer workflows such as seeding sample released levels into development storage.

## Acceptance criteria
- The repository has a clear path from core rules to client UX and online sync.
- The implementation order across specs supports incremental delivery of a playable game.
