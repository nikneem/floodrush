# 07. Local Persistence and Offline Sync

## Objective
Define how FloodRush stores local state and synchronizes with the server safely.

## Offline-first principle
Gameplay must continue without internet access using locally stored profile data, settings, levels, and progress.

## Local data sets
- Active profile
- Downloaded level catalog
- Level revisions and level content
- In-progress games
- Completed scores pending upload
- Settings and configuration
- Sync metadata and retry state

## Sync model
- Local writes happen first.
- Server sync happens asynchronously when connectivity is available.
- Failed sync operations remain queued with retry metadata.
- Sync operations must be idempotent where possible.

## Conflict strategy
- Scores are append-only and should not be overwritten by older data.
- Settings use last-writer-wins unless a later spec introduces per-setting conflict rules.
- Released levels are server-authoritative, but already-downloaded released levels remain locally playable offline.

## Connectivity handling
- The client observes connectivity changes.
- Sync may trigger automatically on startup, resume, and connectivity restoration.
- Gameplay must not block on sync.

## Acceptance criteria
- A player can finish a level offline and later see the score sync successfully.
- Settings changes made offline are preserved and synchronized when connectivity returns.
