# Friday's Adventure — AI / Copilot Documentation

> **For AI assistants working in this codebase.**
> Full architecture reference, API guide, integration map, and known bugs/fixes.
> Start every session by reading this file.

---

## ✅ MANDATORY SESSION LOGGING

After **every** prompt, update `Assets/The Forge/Week10 Log_.docx` with:  
`date/time · features implemented · bugs fixed · docs updated · build status · next steps`

---

## Phase Status

| Phase | Features | Status |
|-------|---------|--------|
| Phase 1 | 110 | ✅ COMPLETE |
| Phase 2 Wave 1 | 110 | ✅ COMPLETE |
| Phase 2 Wave 2 | 190 | ✅ COMPLETE |
| Phase 3 | 110 | 📋 Designed, ready |
| **Total built** | **410** | ✅ PASSING build |

**Build:** C# 7.3 · .NET Framework 4.7.2 · WinForms  
**Entry point:** `Form1.cs` → `Game.cs` → `SceneManager`  
**Known errors fixed:** see § "Bugs Fixed" below

---

## Architecture

```
Game (singleton)
 ├─ SceneManager          push/pop/replace scene stack
 ├─ InputManager          keyboard state snapshot
 ├─ AudioManager          music moods + SFX beeps
 ├─ SMB3Hud               composite HUD (lives, coins, timer, P-meter, boss bar)
 ├─ PowerUpSystem         SMB3 power-up state machine (8 suits/items)
 ├─ ParticleSystem        lightweight 2D particle bursts
 ├─ FloatingTextManager   score pop-up text
 ├─ ScreenShake           trauma-based camera shake
 ├─ AchievementSystem     unlock + banner
 ├─ SessionStats          play-time, stomp count, death count
 ├─ FeatureFlags          runtime feature toggles from feature-flags.cfg
 ├─ DifficultyModifiers   enemy speed/HP/damage scale per difficulty
 ├─ WeatherSystem         rain/snow/storm effects
 ├─ EventBus              type-safe publish/subscribe
 ├─ ObjectPool            reusable entity pool
 ├─ ErrorLogDebugger      daily rotating file + JSON report + VisualDebugger bridge
 └─ VisualDebugger        screenshot capture + HTML gallery + F10 overlay
```

### All Scenes
| Scene | File | Description |
|-------|------|-------------|
| `TitleScene` | Scenes\TitleScene.cs | Animated title, name entry |
| `CharacterSelectScene` | Scenes\CharacterSelectScene.cs | Orca / Swan / Miss Friday |
| `DifficultySelectScene` | Scenes\DifficultySelectScene.cs | Easy / Normal / Hard |
| `LoadingScene` | Scenes\LoadingScene.cs | Asset pre-loader with tips |
| `OverworldScene` | Scenes\OverworldScene.cs | SMB3 world map, 11 islands |
| `IslandScene` | Scenes\IslandScene.cs | Standard platformer levels |
| `FortressScene` | Scenes\FortressScene.cs | Fortress/castle levels |
| `AirshipLevelScene` | Scenes\AirshipLevelScene.cs | Airship autoscroll levels |
| `UnderwaterScene` | Scenes\UnderwaterScene.cs | Underwater swim levels |
| `SkyIslandScene` | Scenes\SkyIslandScene.cs | Sky island levels |
| `BossScene` | Scenes\BossScene.cs | Generic boss fight |
| `WarlordBossScene` | Scenes\WarlordBossScene.cs | Warlord boss (named HP bar) |
| `StormScene` | Scenes\StormScene.cs | Storm/weather encounter |
| `CardMiniGameScene` | Scenes\CardMiniGameScene.cs | N-Spade card matching |
| `OptionsScene` | Scenes\OptionsScene.cs | Accessibility + display |
| `SettingsScene` | Scenes\SettingsScene.cs | Controls + audio |
| `DevMenuScene` | Scenes\DevMenuScene.cs | Dev jump-to-scene + QA log |

---

## Key Files Reference

### `Systems\TeamFeatures_Wave2.cs`
Contains all 19 team feature classes for Wave 2 (190 features total).

```csharp
// Static feature classes — all in namespace Fridays_Adventure.Systems
GameDirectorFeatures    ProducerFeatures        TechLeadFeatures
PowerUpFeatureSet       LevelDesignerFeatures   NarrativeFeatures
GameplayFeatures        SystemsFeatures         UIFeatures
EngineFeatures          BuildEngineerFeatures   ArtDirectorFeatures
CharacterArtFeatures    EnvironmentFeatures     UIArtFeatures
AnimationFeatures       VFXFeatures             SoundFeatures
QAFeatures
```

### `Systems\ErrorLogDebugger.cs`
```csharp
// Usage
DebugLogger.LogInfo("Context", "msg");
DebugLogger.LogWarning("Context", "msg");
DebugLogger.LogError("Context", "msg or Exception");

// Wire screenshot provider once in Form1.cs
DebugLogger.ScreenshotProvider = () => CaptureCanvas();

// Log levels
DebugLogger.MinFileLevel    = LogLevel.Info;
DebugLogger.MinConsoleLevel = LogLevel.Warning;
```

### `Systems\VisualDebugger.cs`
```csharp
// Called automatically by DebugLogger.LogError
VisualDebugger.RecordError("ctx", "details", screenshotPath);

// Overlay toggle (hooked to F10 in Game.cs)
VisualDebugger.DrawOverlay(g, W, H);

// On app exit
VisualDebugger.WriteSessionClose();

// Outputs
// Logs\ErrorShots\<timestamp>.png     — per-error screenshots
// Logs\visual-report.html             — HTML gallery
// Logs\qa-availability.log            — session open/close + error list
```

### `Systems\SMB3Hud.cs`
```csharp
SMB3Hud.Draw(g, W, H, player, game);
SMB3Hud.TriggerGetReady();              // "GET READY" banner
SMB3Hud.SetBossBar("WARLORD KUMA", 0.75f);
SMB3Hud.ClearBossBar();
SMB3Hud.ShowToast("RACCOON LEAF!");
```

### `Systems\PowerUpSystem.cs`
```csharp
PowerUpSystem.Collect(PowerUpType.FireFlower);
PowerUpSystem.Activate(PowerUpType.Starman);
PowerUpSystem.IsActive(PowerUpType.Starman);  // → bool
PowerUpSystem.HasItem(PowerUpType.RaccoonLeaf);
```

### `Audio\AudioManager.cs`
```csharp
AudioManager.SetMood(MusicMood.Boss);    // Overworld/Battle/Boss/Calm/Underwater
AudioManager.PlaySfx("jump");
AudioManager.PlaySfx("coin");
AudioManager.StopMusic();
```

### `Data\SpriteManager.cs`
```csharp
// Always use SpriteManager — never new Bitmap() directly
var bmp = SpriteManager.Get("Orca.png");
var scaled = SpriteManager.GetScaled("GARP.png", 48, 48);
SpriteManager.Preload(new[]{"Orca.png","Swan.png"});
```

### `Engine\Game.cs` — Key Properties
```csharp
Game.Instance             // singleton
Game.CurrentLives         // int
Game.Coins                // int
Game.Score                // long
Game.WorldLevelLabel      // "WORLD 3-4" string
Game.PlayerCharacter      // "Orca" | "Swan" | "MissFriday"
Game.Difficulty           // DifficultyLevel enum
Game.UsePWing()           // skip current level
Game.AddCoins(n)          // auto 1UP at 100
```

---

## Rendering Rules

### Pixel-art GDI+ settings (always apply at start of Draw)
```csharp
EngineFeatures.ApplyPixelArtSettings(g);
// Sets CompositingMode=SourceOver, InterpolationMode=NearestNeighbour,
// PixelOffsetMode=Half, SmoothingMode=None
```

### Draw order (per scene)
1. Background image (`_bg != null → g.DrawImage`)
2. Environment effects (parallax clouds, star field, water ripple)
3. Platforms and tiles
4. Items and collectibles
5. Enemies
6. Player
7. VFX particles
8. HUD overlay (`SMB3Hud.Draw`)
9. Debug/QA overlay (F10)

### Sprite rendering (Entity base)
```csharp
// Entity.Draw() in Entity.cs already checks Sprite != null
// Override DrawPlaceholder() and add the same check:
protected override void DrawPlaceholder(Graphics g)
{
    if (Sprite != null) { g.DrawImage(Sprite,(int)X,(int)Y,Width,Height); return; }
    // ... fallback geometry
}
```

---

## Using Directives Required

Any file using Wave 2 feature classes must include:
```csharp
using Fridays_Adventure.Systems;
```

Scenes that call `EngineFeatures`, `ArtDirectorFeatures`, `UIArtFeatures`, etc.  
(e.g. `TitleScene.cs`, `IslandScene.cs`, `CharacterSelectScene.cs`)  
**all require this using directive.**

---

## Bugs Fixed (All Sessions)

### Session 4 — Enemy Models Not Rendering
**File:** `Entities\Enemy.cs`  
**Issue:** `DrawPlaceholder()` override did not check `Sprite != null`  
**Fix:**
```csharp
protected override void DrawPlaceholder(Graphics g)
{
    if (Sprite != null) { g.DrawImage(Sprite,(int)X,(int)Y,Width,Height); return; }
    // fallback placeholder geometry
}
```

### Session 4 — Character Portrait Too Small
**File:** `Scenes\CharacterSelectScene.cs`  
**Issue:** Portraits rendered at 64×96 — too small to see model detail  
**Fix:** Increased to 128×192

### Session 5 — `TitleScene.cs` Missing `using Fridays_Adventure.Systems`
**File:** `Scenes\TitleScene.cs`  
**Issue:** `EngineFeatures`, `ArtDirectorFeatures`, `EnvironmentFeatures`, `UIArtFeatures`,  
`BuildEngineerFeatures` referenced without the Systems namespace imported → CS0103  
**Fix:** Added `using Fridays_Adventure.Systems;` on line 8

---

## Asset Paths (Canonical)

```
Assets\Models\Orca\Orca.png          — Orca hero sprite
Assets\Models\Swan\Swan.png          — Swan hero sprite
Assets\Sprites\player_missfriday.png — Miss Friday hero sprite
Assets\Sprites\GARP.png              — Standard enemy model (GARP)
Assets\Backrounds\                   — All 23 level backgrounds (note: "Backrounds" typo preserved)
Logs\                                — Runtime logs (auto-created)
Logs\ErrorShots\                     — Per-error screenshots
```

---

## Phase 3 — What's Next

110 expansion features designed and ready. Start with:
1. `docs\PHASE_3_FEATURES_MASTER.md` — read all specs
2. `docs\PHASE_3_IMPLEMENTATION_ROADMAP.md` — order to implement
3. Create `Systems\TeamFeatures_Wave3.cs` using same pattern as Wave 2
4. Wire into Game.cs OnUpdate / OnRender
5. Update this file + README + Week 10 log
