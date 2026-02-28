# README.md — Royal Kingdom-like MVP (Unity)

## What this repo is
A solo-friendly MVP spec and implementation target for a **Royal Kingdom-like** mobile game:
- Match-3 puzzle core (data-driven levels)
- Lightweight kingdom meta progression (node-based build/unlock)
- BossAttack level variant
- Offline-first persistence
- Deterministic replay + automated tests

Key source of truth: `SPEC.md`

---

## Prerequisites
- Unity 2023 LTS
- (Optional) DOTween
- iOS build: Xcode (or Unity Cloud Build/TestFlight)
- Android build: Android SDK/NDK via Unity Hub

---

## Project layout (recommended)
See `SPEC.md` section **Folder / File Structure**.

---

## How to run
1. Open the Unity project
2. Open scene: `Assets/_Game/Scenes/Boot.unity`
3. Press Play

Expected flow:
- Boot -> Kingdom -> Start level -> Match3 -> Win/Lose -> Back to Kingdom

---

## Content configs
All gameplay is driven by Addressables JSON:

- `Assets/_Game/Addressables/configs/economy.json`
- `Assets/_Game/Addressables/configs/remote_tuning.json`
- `Assets/_Game/Addressables/configs/meta/kingdom_nodes.json`
- `Assets/_Game/Addressables/configs/levels/level_XXXX.json`

Addressable keys convention:
- `config:economy`
- `config:remote_tuning`
- `config:kingdom_nodes`
- `level:{id}`

---

## Save data
Local save file:
- `Application.persistentDataPath/save.json`

Includes:
- versioned schema + migration hook
- economy, progress, kingdom nodes, sessionSeed

---

## Running tests
Use Unity Test Runner:
- EditMode tests: `Assets/_Game/Tests/EditMode`
- PlayMode tests: `Assets/_Game/Tests/PlayMode`

Minimum required tests are listed in `SPEC.md` section **Testing & QA**.

---

## Replay-based QA
Replays are deterministic action scripts used for regression tests and bug reproduction.

Replay assets location:
- `Assets/_Game/Addressables/qa/replays/*.json`

Minimum required:
- `replay_smoke_0001.json`
- `replay_boss_0010.json`

A replay record contains:
- levelId, seed, attemptIndex
- ordered actions (swap/hammer)
- expected end result (win/lose, movesUsed, etc.)

Optional runner scene:
- `Assets/_Game/Scenes/QA_ReplayRunner.unity`

---

## Debug panel
In-game debug panel must exist (see `SPEC.md`):
- Add coins / lives
- Jump to level
- Mark node complete
- Export analytics log
- Reset save
- Export last replay
- Load + run replay by id
- Diagnostics snapshot (fps/mem/device)

---

## Beta distribution (manual)
- Android: Google Play Internal Testing
- iOS: TestFlight
- Build stamp in UI: versionName + buildNumber + git hash (if available)

Telemetry baseline (no vendor required):
- app_start
- level_start / level_end
- crash marker on next boot

---

## License
TBD (set your preferred license).
