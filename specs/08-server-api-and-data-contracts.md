# 08. Server API and Data Contracts

## Objective
Define the minimum server capabilities and API surface needed by the client.

## Server architecture constraints
- The API is implemented as a modular monolith.
- Each business module lives in its own project under `src\Server`.
- Each module contains a `Features` namespace.
- Each feature has its own namespace and contains its `Command` or `Query` type together with its handler.
- CQRS is the default pattern for feature implementation.
- Shared client/server DTOs live under `src\Shared\HexMaster.FloodRush.Shared.Contracts`.
- Server-only shared logic belongs under `src\Server`, not `src\Shared`.
- Server persistence uses Azure Table Storage, orchestrated locally through Aspire.

## Core server responsibilities
- Create and manage user profiles
- Return released levels for a profile
- Deliver level content and revision metadata
- Accept score submissions
- Store and return settings relevant to online sync

## Required API areas
### Device authentication
- Device registration endpoint that binds a generated device identifier to a device-owned public key over HTTPS
- Challenge-response login endpoints that require proof of possession of the device private key
- Server-issued short-lived JWT access token for subsequent authenticated requests
- Opaque rotating refresh token support for continued authenticated requests without re-running the full login flow on every call
- Protected endpoint support using bearer token authentication

### Profiles
- Device registration and token issuance live in this module.
- Create profile
- Get profile
- Update profile details and sync state

### Levels
- Playable level definitions live in this module.
- List released levels for a profile
- Get level content by level identifier and revision
- Return release metadata for incremental sync

### Scores
- Worldwide score storage and comparison live in this module.
- Submit score
- Query score history for the current profile
- Query top scores per level

### Settings
- Upload settings
- Download settings

## Contract guidance
- Use explicit DTOs rather than exposing persistence entities.
- Include revision, timestamp, and correlation metadata needed for sync.
- Prefer additive contract evolution for future compatibility.
- Device JWTs must include the device identifier as a claim that downstream endpoints can trust after signature validation.
- Device authentication must not rely on an embedded shared client secret.
- Refresh tokens must be opaque server-managed secrets that are stored hashed on the server and rotated after every successful use.
- Module endpoints should stay thin and delegate behavior to feature handlers.
- In development, the API should expose a Scalar API reference over the generated OpenAPI document so contributors can inspect and exercise the contract quickly.

## Server-side validation
- Reject malformed device identifiers.
- Reject malformed public keys, signatures, refresh tokens, and login challenge identifiers.
- Reject expired or already-consumed login challenges.
- Detect refresh-token reuse and revoke the affected token family.
- Reject score submissions that reference unknown or unreleased levels.
- Validate profile ownership for requested data.
- Validate required fields and supported schema versions.
- Keep Azure Table row and partition keys stable and explicit per module.

## Acceptance criteria
- The client can register a device key, complete a challenge-response login, rotate refresh tokens, and then perform authenticated profile creation, released-level sync, level download, score upload, and settings sync using stable contracts.
- The server codebase is organized by module and feature slice rather than by technical layer alone.
- Running the API project directly in development opens the browser to the Scalar endpoint by default.
