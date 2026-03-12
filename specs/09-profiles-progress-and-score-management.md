# 09. Profiles, Progress, and Score Management

## Objective
Specify how user identity, progression, and scores behave across offline and online play.

## Profile model
A profile should minimally support:
- Stable profile identifier
- Display name
- Local creation timestamp
- Last sync timestamp
- Release entitlement metadata

## Progress model
Progress should track:
- Last played level
- In-progress session snapshot, if resumable
- Completed level revisions
- Best local score per level revision
- Pending score uploads

## Score rules
- Scores are tied to a profile, level identifier, and level revision.
- A score submission contains detailed breakdowns sufficient for auditing totals.
- The server may store best score, latest score, and historical scores separately.

## Continue behavior
- `Continue` resumes the most recent resumable session.
- If no resumable session exists, `Continue` falls back to the most relevant next level or is hidden in favor of `Play`.

## Acceptance criteria
- The client can determine whether to show `Play` or `Continue` from local state alone.
- Scores remain attributable and comparable across level revisions.
