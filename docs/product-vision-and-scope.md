# FloodRush Product Vision and Scope

This document implements `specs\01-product-vision-and-scope.md` in a more approachable reference form for contributors and future product work.

## Vision
FloodRush is a fast-paced pipe-routing puzzle game where the player races against advancing fluid. Each level presents a grid with predefined start and finish points, and the player must build a valid route before the flow makes the level unwinnable.

## Product goals
- Deliver a satisfying single-player puzzle loop that is easy to understand and hard to master
- Make mobile play practical through a landscape-first board layout
- Preserve playability without network access after levels have been downloaded
- Use the online server to distribute new content, identify players, and compare scores globally

## Target platforms
- .NET MAUI application
- Mobile-first experience
- Landscape-only orientation
- Offline-capable client with later synchronization

## In scope for the initial product
- Single-player level-based puzzle gameplay
- Player profile creation and persistence
- Device-backed sign-in and authenticated server communication
- Released-level download and filtering per player
- Local storage for progress, scores, settings, and downloaded levels
- Server synchronization for player data, settings, levels, and scores
- Fixed gameplay tiles such as the fluid basin and split section
- Time pressure mechanics and score comparison

## Out of scope for the initial product
- Real-time multiplayer
- User-generated levels
- In-game level editor
- Social chat systems
- Guilds or clans
- Monetization features

## Primary player journey
1. The player launches the game and lands on the welcome page.
2. The player chooses `Play` or `Continue`, `Load Level`, or `Settings`.
3. The player opens an available level.
4. The player places pipe sections while the pre-flow timer counts down.
5. The fluid starts and the level is evaluated against all finish-point requirements.
6. Progress and score are saved locally.
7. When the device is online, the game synchronizes local changes and downloads newly released levels.

## Success criteria
- Downloaded levels remain playable offline.
- The same level and inputs always produce the same outcome.
- Online synchronization never destroys unsynced local progress.
- Level availability remains bound to the player profile or device identity.

## Guidance for implementation
- Preserve the game as a puzzle-first experience rather than an action or social game.
- Keep new features compatible with offline-first behavior.
- Avoid scope creep into live-service features before the core loop is strong.
- Treat the welcome flow, level access, and synchronization as part of the core experience.
