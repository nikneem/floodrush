# 08. Server API and Data Contracts

## Objective
Define the minimum server capabilities and API surface needed by the client.

## Core server responsibilities
- Create and manage user profiles
- Return released levels for a profile
- Deliver level content and revision metadata
- Accept score submissions
- Store and return settings relevant to online sync

## Required API areas
### Device authentication
- Device login endpoint that accepts a unique device identifier over HTTPS
- Server-issued JWT token for subsequent authenticated requests
- Protected endpoint support using bearer token authentication

### Profiles
- Create profile
- Get profile
- Update profile sync state

### Levels
- List released levels for a profile
- Get level content by level identifier and revision
- Return release metadata for incremental sync

### Scores
- Submit score
- Query score history for the current profile

### Settings
- Upload settings
- Download settings

## Contract guidance
- Use explicit DTOs rather than exposing persistence entities.
- Include revision, timestamp, and correlation metadata needed for sync.
- Prefer additive contract evolution for future compatibility.
- Device JWTs must include the device identifier as a claim that downstream endpoints can trust after signature validation.

## Server-side validation
- Reject malformed device identifiers.
- Reject score submissions that reference unknown or unreleased levels.
- Validate profile ownership for requested data.
- Validate required fields and supported schema versions.

## Acceptance criteria
- The client can obtain a JWT using its device identifier and then perform authenticated profile creation, released-level sync, level download, score upload, and settings sync using stable contracts.
