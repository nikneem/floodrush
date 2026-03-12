# 04. Level Format and Release Progression

## Objective
Specify how levels are represented, versioned, downloaded, and released to players.

## Level payload
Each level definition must include:
- Stable level identifier
- Version number or revision token
- Display name
- Board width and height
- Start delay
- Flow speed indicator
- Fixed tile placements
- Allowed player inventory rules, if inventory is limited
- Required finish points
- Scoring metadata for pipe and bonus tiles

## Optional metadata
- Difficulty label
- Par score or rating thresholds
- Release window
- Tutorial hints
- Tags for filtering

## Release gating
- The server is the authority for which levels are released to a profile.
- The client may cache released levels locally for offline play.
- `Load Level` only lists levels released to the current profile and available locally or from the server.

## Versioning rules
- A level revision must be immutable once released.
- If level behavior changes, publish a new revision.
- Score submissions must reference the exact level revision played.

## Local caching
- Downloaded levels are stored locally with their revision token.
- Obsolete cached levels may remain playable offline if tied to unfinished local progress.
- The sync system should fetch newly released levels without requiring a full data reset.

## Acceptance criteria
- Level data is rich enough to build the board and simulate flow.
- The client can distinguish local cache state from server release state.
- Score entries can always be traced to a concrete level revision.
