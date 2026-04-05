# Miss Friday's Adventure Part II

A 2D action-platformer built with **C# + WinForms** targeting `.NET Framework 4.7.2`,
styled after **Super Mario Bros. 3** — gameplay, HUD, power-ups, world map, and visual identity.

---

## 🎮 How to Play

| Key | Action |
|-----|--------|
| Arrow Keys / WASD | Move |
| Z / Space | Jump (hold for higher jump) |
| X / Left Ctrl | Attack |
| Q | Ice Wall ability |
| E | Character ability (Orca: Tidal Slam, Swan: Wing Dash, Friday: Freeze Flash) |
| R | Break Wall ability |
| I | Open Inventory |
| Esc | Open Options / Pause |
| 1, 2, 3 | Quick-select character on Character Select screen |
| Enter / Z | Confirm selection |

**Dev Menu:** Enter the name **"Luffy"** or **"Loofy"** (case-insensitive) on the title screen.

---

## 🗂 SESSION LOGGING REQUIREMENT

After **every** work session, update:  
`docs/WEEK_10_LOG_TEMPLATE.md`  
Include: date/time · features implemented · bugs fixed · docs updated · build status · next steps.

See `.github/copilot-instructions.md` for full code comment standards.

---

## ✅ Project Status

**Build:** ✅ PASSING (0 errors, 0 warnings)

### Phase 1 — Core Gameplay (110 features) ✅ COMPLETE
The actual playable game — platforming, combat, enemies, levels, bosses, HUD, power-ups, world map, character selection, save/load, audio, and all visual effects.

### Phase 2 — Systems & Tools (110 items) ✅ IMPLEMENTED
Difficulty modifiers, settings menu, hot-reload config, frame histogram, error log rotation, producer dashboards, tech lead tools, design systems, narrative systems, gameplay systems, UI systems, engine systems, and build tools. Most are data-layer / dashboard implementations accessible from the Dev Menu.

### Phase 3 — Expansion Systems (110 items) ✅ IMPLEMENTED
Producer systems, systems foundations, tech lead systems, director systems, engine systems, design systems, level concepts, narrative systems, and gameplay systems. These are mostly system stubs and validation dashboards accessible from the Dev Menu.

---

## 🏗 Architecture

```
Fridays Adventure II/
├── Engine/          — Game singleton, SceneManager, InputManager
├── Scenes/          — All game screens (Title → Gameplay → Victory)
├── Entities/        — Player, Enemy, Berries, PowerUp, etc.
├── Systems/         — HUD, ParticleSystem, AchievementSystem, etc.
├── Hazards/         — FireSource, WaterPit, SeaStoneZone
├── Rules/           — DevilFruitRules
├── Data/            — SaveData, DialogueLine, ContentLoader
├── Audio/           — AudioManager, ProceduralSfx
├── Abilities/       — IceWall, WingDash, etc.
├── docs/            — Session logs, phase trackers, documentation
└── Assets/          — Sprites, audio, data files
```

**Entry point:** `Form1.cs` → `Game.cs` → `SceneManager`  
**Target:** C# 7.3 · .NET Framework 4.7.2 · WinForms

---

## 🐛 Error Log Debugger

**File:** `Systems\ErrorLogDebugger.cs`  
**Class:** `DebugLogger` (static)

- Writes to `Logs\debug-YYYY-MM-DD.log` (daily rotation)
- Auto-cleans logs older than 7 days
- Screenshots on error via `VisualDebugger`

```csharp
DebugLogger.LogInfo("Context", "Message");
DebugLogger.LogError("Context", exception);
```

---

## 📋 Key Documents

| Document | Purpose |
|----------|---------|
| `docs/WEEK_10_LOG_TEMPLATE.md` | Running session log (update after every prompt) |
| `docs/AI_DOCS.md` | Architecture reference for AI assistants |
| `docs/PHASE_2_PROGRESS_TRACKER.md` | Phase 2 checklist |
| `docs/PHASE_3_IMPLEMENTATION_ROADMAP.md` | Phase 3 checklist |
| `.github/copilot-instructions.md` | Code standards and session logging rules |
| 5 | GC gen0/gen1/gen2 counters | `GetGCInfo()` |
| 6 | Draw-call counter | `ResetDrawCalls()`, `IncrementDrawCalls()` |
| 7 | Thread-safe singleton guard | Game.Instance |
| 8 | Hot-key registry | InputManager integration |
| 9 | Build hash in debug overlay | `BuildInfo.Summary` |
| 10 | Context stack dump | `PushContext()`, `DumpContext()` |

### Team 4 — Lead Game Designer (`PowerUpFeatureSet`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Super Mushroom — grow bigger | `ApplySuperMushroom()`, `IsSuperForm` |
| 2 | Fire Flower — shoot fireballs | `CollectFireFlower()`, `HasFireFlower` |
| 3 | Starman — 10s invincibility | `ActivateStarman()`, `StarmanActive` |
| 4 | Raccoon Leaf — tail whip + float | `CollectRaccoonLeaf()`, `HasRaccoonLeaf` |
| 5 | Frog Suit — better swimming | `CollectFrogSuit()`, `HasFrogSuit` |
| 6 | Hammer Suit — throw hammers | `CollectHammerSuit()`, `HasHammerSuit` |
| 7 | Tanooki Suit — statue mode | `SMB3SuitSystem` |
| 8 | P-Wing — skip level | `Game.UsePWing()` |
| 9 | 1-Up Mushroom — extra life | `Grant1Up()` |
| 10 | Cloud power-up — phase once | `CollectCloud()`, `UseCloud()` |

### Team 5 — Level Designer (`LevelDesignerFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Moving platforms | `MovingPlatform` entity |
| 2 | Spring pad | `SpringPadForce = -900f` |
| 3 | Pipe warp zones | Level event system |
| 4 | Ice floor sliding | `IceFriction = 0.02f` |
| 5 | Question blocks | `ResolveBlockContent()` |
| 6 | Breakable brick blocks | `Hazard` system |
| 7 | Secret exit doors | Level event flags |
| 8 | Autoscroll segments | `StartAutoscroll()`, `StopAutoscroll()` |
| 9 | Rotating coin rings | Coin entity groups |
| 10 | Wind current zones | `SetWind()`, `WindForce` |

### Team 6 — Narrative Designer (`NarrativeFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | World introduction text cards | `GetWorldIntro()` |
| 2 | Character backstory codex | NarrativeManager |
| 3 | Enemy Bestiary | `GetBestiaryEntry()` |
| 4 | Island lore tablets | NarrativeFlags |
| 5 | Letter system (A–L) | `CollectLetter()`, `AllLettersCollected` |
| 6 | Ending epilogue text | VictoryScene integration |
| 7 | NPC sailor hints | DialogueScene |
| 8 | Boss pre-fight banter | `GetRandomBossBanter()` |
| 9 | Crew journal entries | CaptainsLog |
| 10 | "Did you know?" secrets | `GetRandomSecret()` |

### Team 7 — Gameplay Programmer (`GameplayFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Crouching | `IsCrouching`, `CrouchHeightMult` |
| 2 | Running start for tail spin | P-Meter integration |
| 3 | Enemy shell kicking | Entity interaction |
| 4 | Stomp chain → Starman at 5 | `RegisterStomp()` |
| 5 | Spin jump (kills spiked enemies) | `BeginSpinJump()`, `IsSpinJumping` |
| 6 | Underwater stroke system | `SwimStrokeForce = -260f` |
| 7 | Item throw | PowerUpInventory |
| 8 | Carry enemy shell | Entity carry system |
| 9 | Slide under walls | Crouch + run collision |
| 10 | Mid-air direction control | SMB3PhysicsExtensions |

### Team 8 — Systems Programmer (`SystemsFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Combo multiplier tracker | `AddComboHit()`, `ComboMultiplier` |
| 2 | Weather system integration | `WeatherSystem` |
| 3 | Random loot drop table | `ShouldDropItem()` |
| 4 | In-level event timeline | `LevelEventSystem` |
| 5 | Collectible item manifest | Item entity tracking |
| 6 | Score multiplier display | `DrawComboDisplay()` |
| 7 | Enemy spawn pool | `ObjectPool` |
| 8 | Cutscene system stub | `NarrativeManager` |
| 9 | World state snapshot | `BuildEngineerExtensions.WriteBuildManifest()` |
| 10 | Asset dependency graph | Content loader |

### Team 9 — UI Programmer (`UIFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Animated coin counter | `DrawCoinCounter()`, `UpdateCoinSpin()` |
| 2 | Lives counter ♥ × N | `DrawLivesCounter()` |
| 3 | Power-up status icon | `SMB3Hud` reserve box |
| 4 | Health hearts display | `DrawHearts()` |
| 5 | Score pop-up on collect | `FloatingTextManager.Spawn()` |
| 6 | Ability cooldown pie timer | `DrawPieTimer()` |
| 7 | Checkpoint notification | `ShowCheckpointBanner()` |
| 8 | Pause menu with resume/restart/quit | `PauseScene` |
| 9 | HUD slide-in animation | `SMB3Hud.TriggerGetReady()` |
| 10 | "NEW BEST!" badge | `ShowNewBestBadge()` |

### Team 10 — Engine Programmer (`EngineFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Bitmap cache (LRU) | `SpriteRegistry` |
| 2 | Frame-rate smoothing (EMA) | `SmoothDt()` |
| 3 | Viewport culling | `IsVisible()` |
| 4 | Background thread asset loader | ContentLoader |
| 5 | Struct memory pool | `ObjectPool` |
| 6 | Lazy initialization | EngineOptimiser |
| 7 | Double-buffer render target | GameCanvas |
| 8 | Per-scene asset manifests | Scene.OnEnter/OnExit |
| 9 | Pixel-art GDI+ settings | `ApplyPixelArtSettings()` |
| 10 | Exit-time asset disposal | `Track()`, `DisposeAll()` |

### Team 11 — Build Engineer (`BuildEngineerFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Version stamp overlay | `DrawVersionStamp()` |
| 2 | Asset manifest verifier | `StartupValidator.Run()` |
| 3 | Crash dump writer | `DebugLogger` + `VisualDebugger` |
| 4 | Log rotation (7 days) | `DebugLogger.CleanOldLogs(7)` |
| 5 | Platform info reporter | `GetPlatformInfo()` |
| 6 | Config validation | `FeatureFlags.Load()` |
| 7 | Debug cheat codes | `ProcessCheatInput()` |
| 8 | Build date in version stamp | `BuildInfo.BuildDate` |
| 9 | Startup self-test | `RunStartupSelfTest()` |
| 10 | Remote config stub | `feature-flags.cfg` |

### Team 12 — Art Director (`ArtDirectorFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Per-world color palettes | `GetWorldSkyColor()` |
| 2 | Tile grid debug overlay | `RenderDebugModes` |
| 3 | Scanline CRT effect | `DrawScanlines()`, `ScanlinesEnabled` |
| 4 | Character silhouette outline | `DrawSpriteOutline()` |
| 5 | Day/night tint system | `SetDayNight()`, `DrawDayNightTint()` |
| 6 | Parallax background layers | `ParallaxBackground` |
| 7 | Entity tint/damage flash | `CharacterArtFeatures` |
| 8 | Screen vignette overlay | `DrawVignette()` |
| 9 | SMB3 palette cycling | `ColorPalette` |
| 10 | Island-theme foreground tint | Scene-specific tint |

### Team 13 — Character Artist (`CharacterArtFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Crouch sprite swap | `IsCrouching` + Player sprite logic |
| 2 | Super-form size visual | `IsSuperForm` + scale |
| 3 | Damage flash (white tint) | `TriggerDamageFlash()`, `IsDamageFlashing` |
| 4 | Death spiral animation | Player death sequence |
| 5 | Victory pose | `DrawVictoryPose()` |
| 6 | Character face HUD icon | `SMB3Hud` portrait |
| 7 | Power-up transform flash | `SMB3Hud.ShowToast()` |
| 8 | Wall-jump lean | Player sprite flip |
| 9 | Skid particles | `ParticleSystem` |
| 10 | Ground shadow | `DrawCharacterShadow()` |

### Team 14 — Environment Artist (`EnvironmentFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Parallax sky + cloud layer | `DrawParallaxClouds()` |
| 2 | Animated water ripple | `DrawWaterSurface()` |
| 3 | Moving cloud sprites | `DrawParallaxClouds()` |
| 4 | Flickering torch | `DrawTorchFlame()` |
| 5 | Snow particles | `ParticleSystem` |
| 6 | Lava bubble particles | `ParticleSystem` |
| 7 | Animated kelp | `EnvironmentArtist` |
| 8 | Caustic light rays | `EnvironmentArtist` |
| 9 | Star field | `DrawStarField()` |
| 10 | Fog gradient | `DrawFog()` |

### Team 15 — UI/UX Artist (`UIArtFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | SMB3 alternating tile border | `DrawSMB3Border()` |
| 2 | Button hover glow | `DrawButtonGlow()` |
| 3 | Blinking selection cursor | `DrawCursor()`, `CursorVisible` |
| 4 | Animated coin icon | `AnimationFeatures.DrawSpinningCoin()` |
| 5 | Score digit roll-up | `EasingFunctions` |
| 6 | Map node hover enlargement | `OverworldScene` |
| 7 | Press-Start animated blink | `DrawPressStart()` |
| 8 | Fade-in scene transitions | `SceneTransition` |
| 9 | Character name plate | Character select panel |
| 10 | "NEW!" badge | `DrawNewBadge()` |

### Team 16 — 2D Animator (`AnimationFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Frame sequencer | `GetFrame()` |
| 2 | Squash-and-stretch on landing | `TriggerLanding()`, `GetLandingScale()` |
| 3 | Coin spin animation | `DrawSpinningCoin()` |
| 4 | Enemy walk cycle | `GetFrame()` on enemy |
| 5 | Character run cycle | `GetFrame()` on player |
| 6 | Bounce easing | `EaseBounce()` |
| 7 | Power-up pickup spin | `VFXFeatures.SpawnStarBurst()` |
| 8 | Level-clear flag wave | `DrawWavingFlag()` |
| 9 | Hurt knockback slide | Player velocity + DamageFlash |
| 10 | Death star-spiral | Player death sequence |

### Team 17 — VFX Artist (`VFXFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Star-collect sparkle burst | `SpawnStarBurst()` |
| 2 | Fireball travel arc | Projectile + particle |
| 3 | Stomp dust puff | `SpawnStompPuff()` |
| 4 | Ground-pound shockwave | `DrawShockwaveRing()` |
| 5 | Power-up collect flash | `VFXFeatures` + `SMB3Hud.ShowToast()` |
| 6 | Hit spark (4-point star) | `DrawHitSpark()` |
| 7 | Coin orbit ring | `ParticleSystem` |
| 8 | Ice wall shatter particles | `IceSystem` + `ParticleSystem` |
| 9 | Level-clear star burst | `SpawnStarBurst()` at player |
| 10 | Boss death explosion | `VFXFeatures` + `ScreenShake` |

### Team 18 — Sound Designer (`SoundFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | Jump sound | `PlayJump()` → `BeepJump()` |
| 2 | Stomp sound | `PlayStomp()` → `BeepStomp()` |
| 3 | Coin collect sound | `PlayCoin()` → `BeepCoin()` |
| 4 | Power-up collect sound | `PlayPowerUp()` → `BeepPowerup()` |
| 5 | Player damage sound | `PlayDamage()` → `BeepHurt()` |
| 6 | Level-clear fanfare | `PlayCourseClear()` → `BeepLevelClear()` |
| 7 | Boss warning sound | `PlayBossWarning()` → `BeepBossIntro()` |
| 8 | 1-Up sound | `Play1Up()` → `PlayVictoryFanfare()` |
| 9 | Game-over jingle | `PlayGameOver()` → `ContinueOrPlay("gameover")` |
| 10 | Starman music cue | `PlayStarMusic()` → `ContinueOrPlay("starman")` |

### Team 19 — QA Tester / Community Manager (`QAFeatures`)
| # | Feature | Key |
|---|---------|-----|
| 1 | In-game bug report button | `WriteBugReport()` |
| 2 | Session availability log | `VisualDebugger` `qa-availability.log` |
| 3 | Error count badge | `VisualDebugger.ErrorCount` |
| 4 | Screenshot gallery in QA scene | `QAReportScene` |
| 5 | Regression test checklist | `RunRegressionChecklist()` |
| 6 | Frame-time spike logger | `CheckFrameSpike()` |
| 7 | Community feedback form | `WriteFeedback()` |
| 8 | QA build watermark | `BuildInfo.Summary` in reports |
| 9 | Error trend chart | `VisualDebugger` HTML report |
| 10 | Export QA report as HTML | `VisualDebugger.WriteHtmlReport()` |

---

## 🗂 Documentation Index

| File | Contents |
|------|----------|
| `README.md` | This file — full project reference |
| `docs\AI_DOCS.md` | Architecture, integration guide, API reference |
| `docs\COMPREHENSIVE_TEAM_FEATURE_AUDIT.md` | Phase 1 complete 110-feature breakdown |
| `docs\PHASE_2_FEATURES_WAVE_1.md` | Wave 1 feature specs |
| `docs\PHASE_2_FEATURES_WAVE_2.md` | Wave 2 feature specs (this session) |
| `docs\PHASE_3_FEATURES_MASTER.md` | Phase 3 expansion specs |
| `docs\MASTER_DOCUMENTATION_INDEX.md` | Navigate all docs |
| `docs\FEATURE_COMPLETION_AUDIT.md` | Wiring verification matrix |
| `docs\FIXES_APPLIED.md` | Enemy rendering fix, portrait size fix |
| `docs\SESSION_4_COMPREHENSIVE_LEGAL_AUDIT.md` | Session 4 audit + fixes |
| `.github\copilot-instructions.md` | Code comment standards |
| `Assets/The Forge/Week10 Log_.docx` | **SESSION LOG — UPDATE AFTER EVERY PROMPT** |

---

## 🏗 Architecture Overview

```
Fridays Adventure II
├── Engine\
│   ├── Game.cs            — singleton: tick, render, all sub-systems
│   ├── GameCanvas.cs      — WinForms paint target
│   ├── InputManager.cs    — keyboard state
│   └── SceneManager.cs    — push/pop/replace scene stack
├── Scenes\                — all game screens (TitleScene → VictoryScene)
├── Entities\              — Player, Enemy, Character, Item, PowerUp
├── Systems\
│   ├── ErrorLogDebugger.cs     — DebugLogger, LogEntry, LogLevel
│   ├── VisualDebugger.cs       — screenshot + HTML report + F10 overlay
│   ├── TeamFeatures_Wave2.cs   — ALL 19 teams Wave 2 (190 features)
│   ├── SMB3Hud.cs              — composite HUD renderer
│   ├── PowerUpSystem.cs        — SMB3 power-up state machine
│   ├── ParticleSystem.cs       — lightweight 2D particles
│   ├── FloatingText.cs         — score pop-up text
│   ├── ScreenShake.cs          — trauma-based camera shake
│   ├── AchievementSystem.cs    — unlock + banner
│   ├── SessionStats.cs         — play-time, stomp count, etc.
│   ├── FeatureFlags (ProducerSystems.cs)
│   ├── DifficultyModifiers.cs
│   ├── WeatherSystem.cs
│   ├── EventBus.cs
│   └── ObjectPool.cs
├── Audio\
│   ├── AudioManager.cs    — music moods + beep SFX
│   └── ProceduralSfx.cs   — NES-style waveform generator
├── Data\
│   ├── SaveData.cs        — slot save/load (3 slots)
│   ├── SpriteManager.cs   — cached bitmap loader
│   ├── ContentLoader.cs   — async background loader
│   └── NarrativeFlags.cs  — story progression flags
├── AI\
│   └── EnemyAI.cs         — patrol + chase + attack FSM
└── Abilities\             — IceWall, FlashFreeze, TidalSlam, WingDash, BreakWall
```

---

## 🎮 Controls

| Action | Keys |
|--------|------|
| Move | WASD / Arrow keys |
| Jump | Space / W |
| Attack | Z |
| Dodge | X |
| Ice Wall | Q |
| Flash Freeze | E |
| Interact / Confirm | F / Enter |
| Pause | Esc |
| Logbook | L |
| Debug Overlay | F10 |
| Dev Mode | Type "Luffy" at name prompt |

---

## 🚀 Quick Start

1. **Build:** Open `Fridays Adventure.csproj` → Build → Run
2. **Enter name:** Type your pirate name at the title prompt
3. **Navigate:** Use arrow keys + Enter on the overworld map
4. **Play:** Complete all 11 islands to win
5. **Debug:** Press F10 to toggle error overlay; check `Logs\` folder for reports

**Victory condition:** Visit and complete all 11 islands.  
Boss encounters are optional bonus challenges.

---

*Last updated: Session 5 — All 19 teams Wave 2 (190 features) + Error/Visual Debugger wiring.*

---

## Third-Party Asset Pipeline (Legally Clean)

This repo includes a repeatable third-party import pipeline under `third_party/` and `tools/`.
It imports approved non-Nintendo assets only and keeps source archives in-repo.

- Manifest: `third_party/asset_manifest.json`
- Asset index: `third_party/asset_index.json`
- Archive cache: `third_party/archives/`
- Extracted cache: `third_party/extracted/`
- Runtime import root: `Assets/third_party/vendor/`

Run:

```bash
python tools/fetch_assets.py --preset pixel
python tools/fetch_ambientcg_textures.py
```

See `docs/THIRD_PARTY_ASSETS.md` for source links, licenses, attribution, and rerun commands.
