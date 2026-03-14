# FloodRush Domain Model and Game Rules

This document is the documentation-friendly companion to `specs\03-domain-model-and-game-rules.md`.

## Modeling style
FloodRush uses pragmatic DDD for the core game domain.

- Important game concepts are represented as explicit domain types.
- Mutable domain objects expose public getters and private setters.
- State changes happen through validated `Set{Property}` methods or similarly explicit behavior.
- The goal is not DDD ceremony. The goal is a clear, trustworthy model that stays valid.

## Core domain concepts
- Rectangular level boards with stable zero-based coordinates
- Placeable pipe sections for the seven player-controlled pipe types
- Fixed tiles for start points, finish points, fluid basins, and split sections
- Flow speed indicators constrained to the level-defined 1 to 100 range
- Level aggregates that validate board rules and fixed-tile reachability

## Invariant expectations
- Coordinates cannot be negative.
- Flow speed indicators must stay in range.
- Fixed tiles cannot overlap.
- Levels must define at least one start point and one finish point.
- Split sections must define one entry direction and two distinct exits.
- Fluid basins must define distinct entry and exit directions.
- Finish points must be theoretically reachable before a level is considered valid.
- If a `FluidBasinTile` has `IsMandatory = true`, every mandatory basin must be visited before a session can transition to `Succeeded`. A level that reaches all finish points without visiting a mandatory basin is marked `Failed`.

## Why this matters
The game rules need to be deterministic and easy to test. A pragmatic DDD model helps keep the rules explicit without turning the codebase into an over-engineered domain framework.
