# 01. Product Vision and Scope

## Objective
Define the product boundaries for FloodRush so gameplay, platform, and online capabilities are implemented consistently.

## Vision
FloodRush is a fast-paced pipe-routing puzzle game where the player races against an advancing fluid flow. Each level presents a grid with predefined start and finish points. The player must place pipe segments before and around the advancing fluid so the flow reaches every required finish point.

## Target platforms
- .NET MAUI application
- Mobile-first experience
- Landscape-only orientation
- Support for offline play with later online synchronization

## In scope
- Single-player puzzle gameplay
- User profile creation and persistence
- Level download and release gating per user
- Local progress, score, and settings storage
- Online sync for profiles, levels, settings, and scores
- Fixed tiles, branching flow, and time pressure mechanics

## Out of scope for initial release
- Real-time multiplayer
- User-generated levels
- In-game level editor
- Social chat or guild systems
- Monetization features

## Primary user journey
1. User launches the game and lands on the welcome page.
2. User selects `Play` or `Continue`, `Load Level`, or `Settings`.
3. User loads an available level.
4. User builds a pipe path under time pressure.
5. Game evaluates completion, score, and progression.
6. Local progress is saved immediately.
7. When online, local state syncs to the server and newly released levels are downloaded.

## Success criteria
- A player can complete levels entirely offline after they have been downloaded.
- The same rules produce the same outcome for a level regardless of device or frame rate.
- Online sync never destroys unsynced local progress.
- Released levels remain filtered by the player profile.
