# SPEC.md — Royal Kingdom-like (Match-3 + Kingdom Meta) Solo MVP (Unity)

## 0. Constraints
- Engine: Unity 2023 LTS (2D)
- Target: iOS + Android (portrait)
- Scope: Offline-first; optional cloud save stub
- No PvP; no real-time multiplayer
- Hot-update: Addressables for content (levels/meta configs). Code hotfix optional (HybridCLR) but NOT required for MVP.
- Data-driven: all gameplay tunables in JSON (Addressables) + ScriptableObject wrappers for editor convenience.

---

## 1. Deliverables
A Unity project implementing:
1. Match-3 core gameplay with level configs loaded from Addressables JSON
2. PvE "Boss Attack" variant levels (damage boss via match events)
3. Meta "Kingdom Progress" (chapter nodes unlock/build) driven by config
4. Currency, boosters, lives, simple shop stubs
5. Save/Load (local JSON) with versioning + migration hook
6. Analytics event interface (no vendor required; implement in-memory logger + file export)
7. Testing & QA system (automated tests + replay + device/beta checklist)

---

## 2. High-level Architecture

### 2.1 Assemblies (recommended)
- `Game.Core` (pure logic, no UnityEngine usage where possible)
- `Game.Unity` (MonoBehaviours, Views, Animations, input)
- `Game.Content` (Addressables loading, config parsing, validation)
- `Game.Persistence` (save/load, migrations)
- `Game.Analytics` (event emitter)
- `Game.Tests` (EditMode tests for core logic)
- `Game.QA` (replay, test helpers, device diagnostics)

### 2.2 Update model
- Core simulation in discrete steps:
  - Player input -> Swap request -> Resolve -> Cascades -> End turn -> Check win/lose
- Animation is view-layer only; logic must be deterministic and runnable without rendering.

---

## 3. Folder / File Structure

```
Assets/
  _Game/
    Scenes/
      Boot.unity
      Match3.unity
      Kingdom.unity
      QA_ReplayRunner.unity            # optional
    Scripts/
      Core/
        Match3/
        Meta/
        Economy/
        Common/
      Unity/
        UI/
        VFX/
        Input/
        Flow/
      Content/
        Addressables/
        Configs/
        Validators/
      Persistence/
      Analytics/
      QA/
        Replay/
        Diagnostics/
        TestHelpers/
    Addressables/
      configs/
        levels/
          level_0001.json
          …
        meta/
          kingdom_nodes.json
        economy.json
        remote_tuning.json
      qa/
        replays/
          replay_smoke_0001.json
          replay_boss_0010.json
    Resources/ (avoid except Bootstrap)
    Tests/
      EditMode/
      PlayMode/
```

---

## 4. Boot + App Flow

### 4.1 Scenes
- `Boot`:
  - Initialize DI/service locator
  - Init Addressables
  - Load configs: economy, remote_tuning, meta nodes
  - Load player save
  - Route:
    - If in progress level => `Match3`
    - Else => `Kingdom`

- `Kingdom`:
  - Show kingdom map (node list)
  - On node click:
    - If locked => show requirements
    - If buildable => consume currency, play build anim, update save
    - If node requires puzzle => start next level (route to Match3)

- `Match3`:
  - Load requested level config
  - Run match gameplay
  - On win => reward + advance meta progress => return to Kingdom
  - On lose => lives decrement; retry offer

- `QA_ReplayRunner` (optional):
  - Load a replay file (Addressables `qa/replays/*`)
  - Execute actions deterministically and assert expected end state

### 4.2 Navigation
Provide `IGameRouter`:
- `GoToKingdom()`
- `GoToMatch3(levelId)`
- `ReloadCurrent()`
- `GoToReplayRunner(replayId)` (optional)

---

## 5. Content System (Addressables)

### 5.1 Required Config Files
- `configs/economy.json`
- `configs/remote_tuning.json`
- `configs/meta/kingdom_nodes.json`
- `configs/levels/level_XXXX.json`

### 5.2 Loading
Implement `IContentService`:
- `Task<T> LoadJsonAsync<T>(string address)`
- `Task PreloadCoreConfigsAsync()`
- Cache results in memory
- Validate against JSON schema-like checks (see Validators)

### 5.3 Addressable Keys
- `config:economy`
- `config:remote_tuning`
- `config:kingdom_nodes`
- `level:{id}` e.g. `level:0001`
- `qa:replay:{id}` e.g. `qa:replay:smoke_0001`

---

## 6. Match-3 Core (Logic)

### 6.1 Board Model
- Grid: width x height (default 9x9), configurable per level
- Cell contains:
  - `BaseTile` (color gem) OR `Blocker` OR `Empty`
  - Optional `Overlay` (ice, chains, slime) with HP/layers
- Gravity: downward
- Spawn: from top with weighted distribution

### 6.2 Tiles
- Colors: enum `TileColor { Red, Blue, Green, Yellow, Purple, Orange }` (configurable subset)
- Special tiles:
  - `Rocket` (row or column clear)
  - `Bomb` (3x3 clear)
  - `Disco` (color clear)
- Obstacles:
  - `Crate` (requires 1 hit)
  - `Stone` (requires N hits, not swappable)
- Overlays:
  - `Ice` (layers; blocks movement until removed)

### 6.3 Moves / Input
- Swap adjacent cells only
- Swap valid if results in match OR if one of swapped tiles is special and triggers (allow special activation)
- After each move: resolve until stable

### 6.4 Match Detection
- Detect matches length >= 3 in rows/cols
- L/T shapes:
  - If a swap creates a match with intersection => create `Bomb`
- 4 in a row/col => create `Rocket` (orientation by match axis)
- 5 in a line => create `Disco`
- Special combination rules (MVP minimal):
  - Rocket + Rocket => cross clear
  - Disco + color => clear all of that color with boosted effects
  - (Implement minimal set: Rocket, Bomb, Disco + basic combos Rocket+Rocket, Disco+Color)

### 6.5 Resolution Order
1. Apply swap
2. Find matches
3. Convert one tile in each qualifying match into special (based on rules); remove other matched tiles
4. Apply damage to obstacles/overlays adjacent or within cleared cells
5. Trigger cascades:
   - Apply gravity to drop tiles
   - Spawn new tiles
   - Repeat find matches until none
6. End-of-turn checks:
   - decrement remaining moves by 1 per player action
   - check win condition
   - if moves==0 and not win => lose

### 6.6 Determinism
- All randomness derived from seeded PRNG:
  - seed = `save.sessionSeed ^ levelId ^ attemptIndex`
- Spawn distribution specified in level config; implement weighted selection.

---

## 7. Level Types & Goals

### 7.1 LevelType
- `Normal`
- `BossAttack`

### 7.2 Win Conditions (Normal)
Support one or more:
- `CollectColor`: collect N tiles of certain colors
- `ClearTiles`: clear N target cells (e.g., "jelly" cells)
- `DestroyObstacles`: destroy crates/stone counts
- `Score`: reach target score
- (MVP: implement at least `CollectColor` + `DestroyObstacles`)

### 7.3 BossAttack Rules
- Boss has HP
- Each tile cleared produces damage:
  - base damage per tile = `remote_tuning.boss.damagePerTile`
  - Special tiles apply bonuses:
    - Rocket: +`rocketDamageBonus`
    - Bomb: +`bombDamageBonus`
    - Disco: damage accumulates from cleared tiles in chain
- Win: bossHP <= 0
- Lose: moves==0 and bossHP>0

### 7.4 Boosters
- Pre-level boosters:
  - `StartRocket` place a rocket on random valid cell
  - `StartBomb`
- In-level boosters (consumable):
  - `Hammer` remove one tile/obstacle
- (MVP: implement `Hammer` at minimum)

---

## 8. Meta (Kingdom) System

### 8.1 Model
- Kingdom consists of Nodes in a progression graph (can be simple linear)
- Node types:
  - `BuildNode` (consume soft currency; unlock cosmetics state)
  - `PuzzleGate` (requires beating a specific levelId)
- Node state: `Locked`, `Available`, `Completed`

### 8.2 Unlock Rules
- Node becomes Available if all prerequisites completed
- Completing a PuzzleGate marks completed and unlocks subsequent nodes
- BuildNode completion consumes currency and sets built state + plays animation

### 8.3 Kingdom View (Unity)
- Minimal clickable list UI is acceptable (no isometric requirement)
- Each node button shows: title, state, cost or required level

---

## 9. Economy

### 9.1 Currencies
- `SoftCurrency` (coins)
- `HardCurrency` (gems) optional; stub only

### 9.2 Lives
- Max lives configurable
- Lose level => lives-1
- Win level => no change
- Refill timer optional; for MVP allow instant refill via debug button

### 9.3 Rewards
- Win reward coins by level config
- Optional star system (can omit)

### 9.4 Shop (Stub)
- UI + service interface:
  - Buy coins with gems (no real IAP required)
  - Buy booster items with coins

---

## 10. Persistence (Save System)

### 10.1 Save Data
- Local file: `Application.persistentDataPath/save.json`
- JSON with version:
  - `saveVersion: int`
  - `playerId: string` (GUID)
  - `economy: { coins, gems, lives, boostersInventory }`
  - `progress: { currentLevelId, maxUnlockedLevelId, attemptIndexByLevel }`
  - `kingdom: { completedNodeIds: [], builtNodeIds: [] }`
  - `sessionSeed: ulong`
  - `timestamps: { lastSaveUtc }`

### 10.2 Migration
Implement `ISaveMigration`:
- `bool CanMigrate(int fromVersion)`
- `SaveData Migrate(SaveData old)`
- Provide placeholder migration from v1->v2.

### 10.3 Cloud Save (Optional Stub)
Interface `ICloudSaveService` with empty implementation.

---

## 11. Analytics

### 11.1 Event Interface
`IAnalytics`:
- `Track(string eventName, Dictionary<string, object> props)`
- `Flush()`

### 11.2 Required Events
- `app_start`
- `level_start` { levelId, attempt, type }
- `level_end` { levelId, attempt, result, movesUsed, durationSec, boostersUsed, bossHpRemaining? }
- `kingdom_node_view` { nodeId, state }
- `kingdom_node_complete` { nodeId, cost }
- `booster_use` { boosterType, context }

Implement default analytics logger to memory + append to file `analytics_log.jsonl`.

---

## 12. UI Requirements (Minimal)
- Main screens:
  - Kingdom screen with nodes list
  - Match3 screen with:
    - moves counter
    - goal panel
    - boss HP bar (BossAttack only)
    - boosters panel (Hammer)
    - win/lose overlay
- Accessibility: big tap targets; haptics optional

---

## 13. VFX / Juice (Minimal Implementation)
- Use DOTween (free) for:
  - tile swap tween
  - tile pop scale
  - board shake on big clears
- Particle placeholders: simple prefab per color
- Sound placeholders: click/pop/win/lose
- (Assets can be placeholders; provide hooks and prefab slots.)

---

## 14. JSON Config Specs

### 14.1 `economy.json`
```json
{"version":1,"startingCoins":500,"startingGems":50,"maxLives":5,"boosters":{"Hammer":{"startingCount":3,"coinPrice":200}}}
```

### 14.2 `remote_tuning.json`
```json
{"version":1,"spawnWeightsDefault":{"Red":1,"Blue":1,"Green":1,"Yellow":1,"Purple":1,"Orange":1},"boss":{"damagePerTile":1,"rocketDamageBonus":6,"bombDamageBonus":10}}
```

### 14.3 `kingdom_nodes.json`
```json
{"version":1,"nodes":[{"id":"n001","type":"BuildNode","title":"Town Gate","costCoins":300,"prereq":[]},{"id":"n002","type":"PuzzleGate","title":"Clear the Road","levelId":"0001","prereq":["n001"]}]}
```

### 14.4 `level_XXXX.json` (Normal)
```json
{"version":1,"id":"0001","type":"Normal","board":{"width":9,"height":9,"colors":["Red","Blue","Green","Yellow","Purple"],"spawnWeights":{"Red":1,"Blue":1,"Green":1,"Yellow":1,"Purple":1}},"moves":20,"goals":[{"kind":"CollectColor","color":"Red","count":25},{"kind":"DestroyObstacles","obstacle":"Crate","count":8}],"layout":{"cells":[]},"rewards":{"coins":60}}
```

### 14.5 `level_XXXX.json` (BossAttack)
```json
{"version":1,"id":"0010","type":"BossAttack","board":{"width":9,"height":9,"colors":["Red","Blue","Green","Yellow","Purple"]},"moves":18,"boss":{"hp":120},"rewards":{"coins":120}}
```

---

## 15. Validation Rules (must implement)

Validators run at boot and fail-fast in dev builds:
- `economy.json`:
  - maxLives >= 1
  - startingCoins >= 0
- `remote_tuning.json`:
  - spawn weights non-negative and sum > 0
  - boss.damagePerTile >= 0
- levels:
  - id matches filename
  - width/height within [5..12]
  - moves within [1..60]
  - goal counts > 0
  - colors list size >= 3
  - layout cells length == width*height if present
- kingdom nodes:
  - unique ids
  - prereq ids exist
  - PuzzleGate levelId exists

---

## 16. Core APIs (interfaces)

### 16.1 Match3
`IMatch3Game`:
- `GameState State { get; }`
- `void Initialize(LevelConfig level, uint seed)`
- `SwapResult TrySwap(Vector2Int a, Vector2Int b)`
- `IReadOnlyList<LogicEvent> ConsumeEvents()`
- `EndResult? GetEndResult()`

### 16.2 Meta
`IKingdomService`:
- `IReadOnlyList<KingdomNodeState> GetNodes()`
- `bool TryCompleteBuild(string nodeId)`
- `bool TryStartPuzzle(string nodeId, out string levelId)`

### 16.3 Persistence
`ISaveService`:
- `SaveData Current { get; }`
- `void LoadOrCreate()`
- `void Save()`
- `void ResetForDebug()`

### 16.4 Content
`IContentService`:
- `Task PreloadCoreConfigsAsync()`
- `Task<LevelConfig> LoadLevelAsync(string levelId)`

### 16.5 QA / Replay
`IReplayService`:
- `ReplayRecord StartRecording(string levelId, uint seed, int attemptIndex)`
- `void RecordAction(ReplayAction action)`
- `ReplayRecord StopRecording()`
- `Task<ReplayRecord> LoadReplayAsync(string replayId)`
- `ReplayRunResult RunReplay(IMatch3Game game, ReplayRecord replay, LevelConfig level)` (pure logic)

`IDiagnosticsService`:
- `DeviceDiagnostics GetSnapshot()` (fps, memory, resolution, device model, platform)

---

## 17. Logic Events (for animation)

Define `LogicEvent` union-like:
- `SwapEvent { a, b }`
- `MatchClearEvent { cells[], source: "Match"|"Special" }`
- `SpecialCreatedEvent { cell, kind }`
- `TileFellEvent { from, to }`
- `TileSpawnedEvent { at, color }`
- `ObstacleDamagedEvent { cell, obstacle, remainingHp }`
- `BossDamagedEvent { amount, remainingHp }`

View layer consumes events sequentially and animates; logic never waits for animation.

---

## 18. Acceptance Tests (EditMode)

Write tests in `Game.Tests`:
1. Swap that doesn't create match => rejected (unless special activation allowed)
2. 3-in-row => clears exactly 3 and spawns new tiles, stable end
3. 4-in-row => creates Rocket at correct position
4. 5-in-row => creates Disco
5. BossAttack damage equals clearedTiles*damagePerTile + bonuses
6. Save/Load roundtrip retains progress and economy
7. Kingdom prereq unlock: node available only after prereqs completed

---

## 19. Debug / Developer Tools (must)

In-game debug panel (toggle by 5 taps on version label):
- Add coins
- Add lives
- Jump to levelId
- Mark node complete
- Export analytics log
- Reset save
- Export last replay (if enabled)
- Load and run replay by id
- Diagnostics snapshot (fps/mem/device)

---

## 20. Testing & QA (must implement)

### 20.1 Testing Layers
Maintain 4 layers:
1. EditMode tests (fast, deterministic): core logic, config parsing/validation, persistence.
2. PlayMode tests (Unity integration): scene routing, UI wiring, event-to-animation pipeline basic assertions.
3. Replay-based deterministic tests: run recorded action sequences and assert end state.
4. Device/Beta checklists: manual scripts for performance/touch/backgrounding and distribution tests.

### 20.2 Unity Test Framework Setup
- Tests path:
  - `Assets/_Game/Tests/EditMode/*`
  - `Assets/_Game/Tests/PlayMode/*`
- Must be runnable headless via Unity batchmode (CI-friendly).

### 20.3 Required Automated Test Cases

**EditMode (logic)**
- `Config_Validators_FailFast_OnInvalidJson`
- `Match3_SwapReject_WhenNoMatch_AndNoSpecialActivation`
- `Match3_Match3_ClearsAndSettles_StableBoard`
- `Match3_Match4_CreatesRocket_OrientationCorrect`
- `Match3_Match5_CreatesDisco`
- `Boss_Damage_AccumulatesExpected`
- `Save_RoundTrip_PreservesProgress`
- `Kingdom_Prereq_UnlocksCorrectly`
- `PRNG_SameSeed_SameOutcome_ForReplay`

**PlayMode (integration)**
- `Boot_LoadsConfigs_AndRoutesToKingdom`
- `Kingdom_StartLevel_RoutesToMatch3`
- `Match3_Win_ReturnsToKingdom_AndRewardsApplied`
- `Match3_Lose_DecrementsLife_AndRetryWorks`

### 20.4 Replay System (Deterministic QA)

**Goals**
- Reproduce bugs deterministically across machines.
- Run "smoke replays" as regression tests.

**ReplayRecord Format (JSON)**
```json
{"version":1,"levelId":"0001","seed":123456789,"attemptIndex":0,"actions":[{"t":0,"type":"swap","a":[3,4],"b":[3,5]},{"t":1,"type":"hammer","cell":[4,4]}],"expected":{"result":"win","movesUsed":12,"bossHpRemaining":null,"coinsGained":60}}
```

Notes:
- `t` is action index (not wall-clock time).

**ReplayAction Types (MVP)**
- `swap`: `{ a: [x,y], b: [x,y] }`
- `hammer`: `{ cell: [x,y] }`

**Replay Runner Behavior**
- Load level config + initialize match3 with `replay.seed`
- Apply each action sequentially:
  - `swap` => `TrySwap(a,b)` assert accepted
  - `hammer` => booster API assert allowed
- Resolve to completion:
  - Continue until `GetEndResult()` non-null OR moves exhausted and board stable
- Assert expected:
  - win/lose
  - movesUsed
  - bossHpRemaining (boss only)
  - coinsGained (if deterministic)

**Required Replay Assets**
- `qa/replays/replay_smoke_0001.json`
- `qa/replays/replay_boss_0010.json`

### 20.5 Manual Device Test Checklist

Must-test scenarios (Android/iOS):
- Cold start -> Boot -> Kingdom (first-run save creation)
- Start level -> play 30 seconds -> background app -> resume (state intact)
- Rapid taps / multi-touch noise during animation (no double swap / no deadlock)
- Device rotation behavior (portrait lock; no layout break if rotation happens)
- Low-memory stress and resume (no crash; state OK)
- Addressables load failure simulation where relevant (offline-first configs should be bundled)

Performance targets (MVP):
- 60 FPS on mid devices; acceptable 30 FPS on low devices
- Avoid GC spikes during cascades:
  - no per-frame LINQ
  - use pools for tile objects
- Track via diagnostics:
  - rolling FPS avg
  - memory snapshot
  - max frame time during large cascades

### 20.6 Beta Distribution (process artifacts)
- Android: Google Play Internal Testing
- iOS: TestFlight
- Build stamp visible in UI: versionName + buildNumber + git hash (if available)
- Minimum telemetry for beta (using `IAnalytics`):
  - `level_start` / `level_end`
  - `app_start`
  - crash marker on next boot (detect unclean exit)

---

## 21. Non-goals (explicit)
- No online leaderboard
- No real IAP integration
- No ad SDK
- No advanced AI/ML level generation
- No full isometric construction mechanics (use node-based build)

---

## 22. Implementation Notes / Defaults
- Use `UnityEngine.Pool` for tile objects
- Avoid LINQ in per-frame loops
- Use Addressables AsyncOperations and central cancellation token in Boot
- Prefer ScriptableObject only for editor-time references (prefabs, sprites); runtime configs from JSON

---

## 23. Done Criteria

Project is considered done when:
- You can play from Boot -> Kingdom -> start a level -> win -> get rewards -> unlock/build nodes -> persist after restart
- At least 10 normal levels + 1 boss level are playable from JSON configs
- All acceptance tests pass
- Debug panel works
- Replay system can record + replay at least 2 smoke cases and assert expected outcomes
- Manual device checklist completed on at least 1 Android + 1 iOS device

---

END
