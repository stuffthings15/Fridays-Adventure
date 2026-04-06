# Bot Architecture — Autonomous Level Completion System

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     BotPlayLevelScene                            │
│  ┌──────────┐  ┌───────────────────┐  ┌──────────────────────┐  │
│  │ Inner     │  │ BotPlayerController│  │ Completion Tracker   │  │
│  │ Scene     │←─│ (Input Injection)  │  │ (Path A / Path B)    │  │
│  │ .Update() │  │                    │  │                      │  │
│  └─────┬─────┘  └────────┬──────────┘  └──────────┬───────────┘  │
│        │                 │                         │              │
│        │ draws           │ reads decisions          │ detects      │
│        │ physics         │ injects keys             │ flags/depth  │
│        ▼                 │                         │              │
│  ┌──────────┐           │                         │              │
│  │ Player   │           │                         │              │
│  │ Entity   │◄──────────┘                         │              │
│  └──────────┘                                     │              │
│        ▲                                          │              │
│        │ observes                                  │              │
│        │                                          │              │
│  ┌─────┴───────────────────────────────────┐      │              │
│  │     UnifiedComprehensiveBot             │      │              │
│  │  ┌─────────┐ ┌──────────┐ ┌─────────┐  │      │              │
│  │  │Perceive │→│ Decide   │→│ Output  │  │      │              │
│  │  │(reflect)│ │(per-scene│ │ShouldX  │  │      │              │
│  │  │enemies  │ │ logic)   │ │flags    │  │      │              │
│  │  │items    │ │          │ │         │  │      │              │
│  │  │hazards  │ │          │ │         │  │      │              │
│  │  │platforms│ │          │ │         │  │      │              │
│  │  └─────────┘ └──────────┘ └─────────┘  │      │              │
│  └─────────────────────────────────────────┘      │              │
└─────────────────────────────────────────────────────┘
```

---

## 1. Perception System (Object & State Detection)

### Architecture: Reflection-Based Field Scanning

The bot uses **cached reflection fields** (set once in the constructor) to read
private lists from the inner scene every frame:

| Field | Type | Source |
|-------|------|--------|
| `_enemies` | `List<Enemy>` | All scene types |
| `_healthPickups` | `List<HealthPickup>` | IslandScene |
| `_berries` | `List<Berries>` | IslandScene |
| `_hazards` | `List<Hazard>` | IslandScene, SkyIslandScene |
| `_exitFlag` / `_exitZone` | `Rectangle` | All scene types |
| `_platforms` | `List<Rectangle>` | All scene types |
| `_groundY` | `int` | IslandScene |
| `_strikes` | `List<LightningStrike>` | StormScene |
| `_boss` | `Enemy` | BossScene, WarlordBossScene |

### Ground/Gap Detection

```
HasGroundAt(probeX):
  1. Check _groundY baseline (skip if inside WaterPit)
  2. Check every Rectangle in _platforms
  3. Return true if any surface found within GROUND_SEARCH_DEPTH (200px)

HasGroundAhead(dist):
  probeX = player.leading_edge + dist (direction-aware)
  return HasGroundAt(probeX)
```

### Hazard Classification

| Hazard Type | Detection | Response |
|-------------|-----------|----------|
| WaterPit | `HazardType.WaterPit`, ground gap scanner | Jump over, wall-jump recovery |
| FireSource | `HazardType` check in RunHazardAvoidance | Jump over when within 100px |
| SeaStoneZone | Same | Jump over |
| Lightning | StormScene `_strikes` reflection | Run opposite direction |

---

## 2. Decision-Making System

### Priority-Based State Machine (per frame)

```
1. STUCK_ESCAPE (1.5s no progress → escape manoeuvre)
2. HEALTH_MANAGEMENT (HP < 40% → use medkit)
3. HAZARD_AVOIDANCE (fire/stone ahead → jump)
4. SCENE-SPECIFIC LOGIC:
   ├─ StormScene     → STORM_DODGE / STORM_SURVIVE
   ├─ BossScene      → BOSS_FIGHT (approach, stomp, melee, dash)
   ├─ SkyIslandScene → VERTICAL CLIMB (platform-by-platform)
   ├─ UnderwaterScene→ UNDERWATER_GOAL (swim to exit)
   └─ IslandScene    → NORMAL (combat → goal → items → platform)
5. PIT/LEDGE AVOIDANCE (edge probes, gap crossing state machine)
6. FALL RECOVERY (wall-jump, sinking mash)
```

### Scene-Specific Strategies

**IslandScene (RunNormalLogic):**
- P1: Combat if enemy within 150px
- P2: Goal pursuit (walk to _exitFlag)
- P3: Item collection (berries, health pickups)
- P4: Default platforming (move right, periodic jump)

**SkyIslandScene (RunVerticalLogic):**
- Find current platform (feet within 5px of top)
- Find next platform above (within 320px double-jump reach)
- Compute launch position (outside target's X span, near edge)
- Walk to launch → Jump + drift → Double-jump at apex → Land

**StormScene (RunStormLogic):**
- Dodge lightning within 120px
- Stay near ship centre
- Collect health when hurt

**BossScene (RunBossFightLogic):**
- Maintain 60-100px range
- Stomp + melee combo
- Dash through every 2s
- Collect health when below 50%

**UnderwaterScene (RunUnderwaterLogic):**
- Swim toward exit zone
- Periodic upward swim for exploration

### Fallback Logic

| Failure Mode | Recovery |
|-------------|----------|
| Stuck 1.5s | Alternate direction escape (Sky) or forward jump (normal) |
| Fell in pit | Wall-jump recovery → seek nearest wall → mash jump |
| Sinking status | Mash all buttons to escape water |
| Player death | BotPlayLevelScene detects HP=0, calls Finish(false) |
| Scene corruption | Depth/Current check, pop rogue scenes |
| Exception | Catch-all → ShouldMoveRight = true |

---

## 3. Navigation & Hazard Avoidance

### Platform Climbing (SkyIsland)

**Physics (computed from source):**
```
Gravity      = 860 px/s²
JumpForce    = -520 px/s
Single jump  = 157 px height
Double jump  = 314 px height (0.18s cooldown between)
Horizontal   = 290 px/s × 1.21s air = 350px (single), 696px (double)
Platform gap = 250 px vertical, 18 px thick
```

**All 12 platforms mapped:**
```
[0]  GROUND:  X=[0,900]     Y=3140
[1]  Plat 1:  X=[80,260]    Y=2920
[2]  Plat 2:  X=[250,410]   Y=2670  (enemy)
[3]  Plat 3:  X=[500,700]   Y=2420
[4]  Plat 4:  X=[650,820]   Y=2170
[5]  Plat 5:  X=[150,340]   Y=1920  (enemy)
[6]  Plat 6:  X=[420,595]   Y=1670
[7]  Plat 7:  X=[600,785]   Y=1420  (SeaStone)
[8]  Plat 8:  X=[200,400]   Y=1170  (enemy)
[9]  Plat 9:  X=[480,645]   Y=920
[10] Plat 10: X=[100,280]   Y=670
[11] Plat 11: X=[380,575]   Y=420   (enemy)
[12] Plat 12: X=[560,770]   Y=170
Exit zone:    X=[410,490]   Y=60
```

### Double-Jump Sequencing

```
BotPlayerController hold-based system:
  1. ShouldJump=true, ShouldDoubleJump=true
  2. InjectHeld(Space) for DOUBLE_JUMP_HOLD (0.85s)
  3. Frame 1: JumpPressed fires → first jump
  4. Frame ~12 (0.18s): _doubleJumpDelay expires → JumpPressed fires again → second jump
  5. Holding Space prevents variable-jump-height cap from cutting velocity
```

### Gap Crossing State Machine

```
Phase 1 (GROUNDED):
  close_ground = HasGroundAhead(48px)
  far_ground   = HasGroundAhead(120px)
  if !close_ground → EDGE_JUMP (jump + forward momentum)
  if !far_ground   → GAP_PREJUMP (early jump)

Phase 2 (AIRBORNE):
  if crossing_gap → maintain forward direction
  if fell_in_pit → wall-jump recovery

Phase 3 (RECOVERY):
  if touching_wall → wall_jump + push away
  if not_touching  → seek nearest wall
```

---

## 4. Adaptation Mechanisms

### Stuck Detection & Escape

```
Every frame:
  total_moved = distance(player, anchor)
  if total_moved < 30px for 1.5s:
    trigger escape manoeuvre
    reset anchor after escape
```

**Sky Island escape:** Alternate left/right side-stepping (no jumping).
**Normal escape:** Jump forward over obstacle. If pit ahead, jump in place then move.

### Item Abandonment System

```
If chasing same item for 15s → add to abandoned set
Never pursue abandoned items again this level
Prevents infinite loops on unreachable collectibles
```

### Combat Timeout

```
If stomping enemy for 1s without kill → dash through (E key)
Reset timer, try stomp again
```

---

## 5. Failure Handling & Logging

### Structured Event Logging

Every bot decision writes to `Logs/bot_<SceneName>_<timestamp>.log`:

```
[0.00s] [BOT_INIT] Scene=IslandScene Player X=100 Y=340 HP=100
[0.52s] [GOAL_PURSUIT] FlagX=3200 Dist=3100
[1.20s] [EDGE_JUMP] No ground 48px ahead — jumping + moving to clear gap
[2.35s] [COMBAT_START] New target Enemy X=450
[3.10s] [COMBAT_DASH] Enemy alive after 1.0s — pressing E to dash through
[5.80s] [HAZARD_JUMP] Jumping over Fire at dist=60px
[8.00s] [MEDKIT] Used medkit at HP=35/100
[12.5s] [STUCK_ESCAPE] Stuck 1.5s at X=500 Y=340 — escaping
```

### Diagnostic Outputs

BotPlayLevelScene tracks:
- Path used (A = stack push, B = reflection flag)
- Total elapsed time
- Completion status
- Diagnostics report printed to console on exit

---

## 6. Testing & Validation

### Level Coverage Matrix

| Level ID | Scene Type | Completion Path | Bot Strategy |
|----------|-----------|----------------|--------------|
| dino | IslandScene | Path A (Push) | Normal + combat |
| wano | IslandScene | Path A | Normal + combat |
| harbor | IslandScene | Path A | Normal + hazards |
| tundra | IslandScene | Path A | Normal + ice |
| storm1/2 | StormScene | Path B (Pop) | Dodge lightning |
| sky | SkyIslandScene | Path B | Vertical climb |
| coral/kelp/etc. | UnderwaterScene | Path B | Swim to exit |
| blockade | BossScene | Path B | Boss fight |
| warlord1/2 | WarlordBossScene | Path B | Boss fight |
| centipede_final | BossScene | Path B | Boss fight |

### Automated Test Modes

1. **Demo Mode** (DemoModeScene): Plays through level list visually
2. **Visual Test** (AutoTestLevelScene → Visual): Single-level bot playthrough
3. **Statistical Test** (AutoTestLevelScene → Statistical): Fast batch simulation

### Key Metrics

- **Completion rate**: Track per-level pass/fail over multiple runs
- **Time to complete**: Should be well under 90s timeout
- **HP on completion**: Measures combat effectiveness
- **Stuck count**: Should decrease with tuning

---

## 7. Identified Risks & Recommendations

### Current Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Enemy damage kills bot before completing | High | Medkit auto-use at 40% HP |
| Sky Island: horizontal distance too far | Medium | Double-jump covers 696px |
| Boss fights: bot can't dodge telegraphed attacks | Medium | Periodic dash + jump |
| Card Roulette hangs | Low | Auto-stop timer (0.8s/card) |
| DialogueScene blocks progress | Low | AutoAdvance flag (1.5s timer) |

### Recommendations for Near-100% Reliability

1. **Add retry logic** — on death, re-enter the level instead of reporting failure
2. **Adaptive combat range** — disengage if HP drops below 30% and no medkits
3. **Boss attack pattern learning** — track when boss attacks, dodge windows
4. **Per-level timing profiles** — some levels need more than 90s
5. **Platform-specific stuck escape** — if stuck on a moving platform, ride it
