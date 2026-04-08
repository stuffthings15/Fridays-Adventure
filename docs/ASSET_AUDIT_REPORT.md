# ASSET AUDIT REPORT — Miss Friday's Adventure II

**Generated:** Session 127  
**Engine:** .NET Framework 4.7.2 / WinForms / GDI+  
**Art Style:** Hand-painted character sprites + procedural GDI geometry for platforms/levels  

---

## 1. EXISTING ASSETS INVENTORY

### Sprites (`Assets/Sprites/`) — 49 files

| Category | Count | Files |
|----------|-------|-------|
| **Backgrounds** | 31 | `bg_dinoIsland.png`, `bg_skyisland.png`, `bg_bladenation.png`, `bg_Blade_Nation.png`, `bg_island.png`, `bg_overworld.png`, `bg_title.png`, `bg_Marine_Blockade.png`, `bg_Centipede_of_the_Deep.png`, `bg_Warlord_Sudo.png`, `bg_Warlord_Vanta.png`, `bg_Tempest_Strait.png`, `bg_Harbor_Town.png`, `bg_Coral_Reef.png`, `bg_Tundra_Peak.png`, `bg_Dive_Gate.png`, `bg_Sunken_Gate.png`, `bg_Kelp_Maze.png`, `bg_Vent_Ruins.png`, `bg_Abyss.png`, `bg_Dinosaur_Island.png`, `bg_Storm_Belt.png`, `bg_Storm_island.png`, `bg_Sea_Serpent.png`, `bg_Desert_island_kingdom.png`, `bg_Giant_tree_island.png`, `bg_Tropical_jungle_island.png`, `bg_Ancient_ruins_island.png`, `bg_Volcano_island.png`, `bg_deck.jpg`, `clouds.png` |
| **Player Characters** | 4 | `player_missfriday.png`, `player_Miss_Friday.png`, `player_Orca.png`, `player_Swan.png` |
| **Enemies** | 9 | `enemy_boss.png`, `enemy_Garp.png`, `enemy_marine.png`, `enemy_Cloud_Lancer.png`, `enemy_Oni_Ashigaru.png`, `enemy_Raptor_Marauder.png`, `enemy_Ronin_Enforcer.png`, `enemy_Thunder_Mask_Priest.png`, `enemy_Triceratops_Brute.png` |
| **Bosses** | 2 | `boss_Garp.png`, `GARP.png` |
| **UI** | 1 | `ui_panel.png` |
| **Misc** | 1 | `level_1.png` |

### Character Model Art (`Assets/Character Models/`) — 10 files
- `Boy Orca/Orca.png` — Orca character concept art
- `Girl Swan/Swan.png` — Swan character concept art
- `Concept Model Art/Swan and orca concept.png`
- `Enemies/Cloud_Lancer.png`, `Garp.png`, `Oni_Ashigaru.png`, `Raptor Marauder.png`, `Ronin_Enforcer.png`, `Thunder_Mask_Priest.png`, `Triceratops Brute.png`

### Audio (`Assets/Audio/`) — 24 music files
- 12 game music tracks (boss, combat, exploration, event, finale, hub, island, overworld, theme)
- 12 podcast/lecture recordings (not game assets)

### SFX (`Assets/SfxCache/`) — 14 sound effects
- `attack.wav`, `berry.wav`, `breakwall.wav`, `coin.wav`, `freeze.wav`, `heal.wav`, `hurt.wav`, `ice.wav`, `introambient.wav`, `jump.wav`, `seastone.wav`, `sink.wav`, `stomp.wav`, `victoryfanfare.wav`

### Third-Party Assets (`Assets/third_party/vendor/`) — Already Downloaded
| Pack | Files | License | Status |
|------|-------|---------|--------|
| **Kenney Pixel Platformer** | 231 tiles (backgrounds, characters, terrain) | CC0 | ✅ Downloaded, **NOT USED** in code |
| **Kenney Pixel Platformer Blocks** | 336 tiles (marble, rock, sand, stone) | CC0 | ✅ Downloaded, **NOT USED** in code |
| **Kenney Pixel UI Pack** | 38 assets (9-slice panels, buttons) | CC0 | ✅ Downloaded, **NOT USED** in code |
| **Kenney Input Prompts Pixel 16** | 821 tiles (keyboard/gamepad icons) | CC0 | ✅ Downloaded, **NOT USED** in code |
| **AmbientCG Textures** | ~15 textures (dirt, grass, rock, wood) | CC0 | ✅ Downloaded, **NOT USED** in code |

---

## 2. MISSING SPRITES — Referenced in Code but Not in `Assets/Sprites/`

| File | Referenced By | Impact | Severity |
|------|---------------|--------|----------|
| **`enemy_goomba.png`** | `SMB3EnemyTypes.cs:163` — Goomba enemy sprite | Goomba renders as invisible (null sprite, GDI draws nothing) | 🔴 HIGH |
| **`enemy_hammer_bro.png`** | `SMB3EnemyTypes.cs:770` — Hammer Bro enemy sprite | Hammer Bro renders invisible | 🔴 HIGH |
| **`enemy_koopa.png`** | `SMB3EnemyTypes.cs:354` — Koopa enemy sprite | Koopa renders invisible | 🔴 HIGH |
| **`portrait_friday.png`** | `DialogueLine.cs:19` — NPC dialogue portrait | Portrait box shows blank (cosmetic only) | 🟡 MEDIUM |

**Note:** `SpriteManager.Get()` returns `null` for missing files. Entity `Draw()` methods check for null sprites and skip rendering. **No crashes**, but enemies appear invisible in gameplay.

---

## 3. UNUSED ASSETS — On Disk but Never Referenced in Code

### Backgrounds (7 files never loaded)
| File | Reason |
|------|--------|
| `bg_Storm_Belt.png` | Code loads `bg_island.png` for storm; this file exists but isn't mapped |
| `bg_Storm_island.png` | Duplicate/unused storm background |
| `bg_Sea_Serpent.png` | No scene references this background |
| `bg_Desert_island_kingdom.png` | Concept art, no level uses it |
| `bg_Giant_tree_island.png` | Concept art, no level uses it |
| `bg_Tropical_jungle_island.png` | Concept art, no level uses it |
| `bg_Blade.png` | Duplicate of `bg_Blade_Nation.png` / `bg_bladenation.png` |

### Duplicate backgrounds
- `bg_bladenation.png` AND `bg_Blade_Nation.png` — both exist, code uses `bg_bladenation.png`
- `bg_dinoIsland.png` AND `bg_Dinosaur_Island.png` — both exist, code uses `bg_dinoIsland.png`
- `player_missfriday.png` AND `player_Miss_Friday.png` — both exist

### Kenney Packs — 1,426 tiles completely unused
The entire `Assets/third_party/vendor/kenney/` directory with 4 asset packs (1,426 files) is downloaded but **zero files are referenced** in any `.cs` source file. The game uses procedural GDI+ drawing for all platforms, terrain, and UI — not tilesheet-based rendering.

---

## 4. ASSET NEEDS LIST — What to Get

### 🔴 CRITICAL (Enemies are invisible without these)

| Need | Filename | Specs | Recommended Source |
|------|----------|-------|--------------------|
| Goomba-style enemy sprite | `enemy_goomba.png` | ~32×32 or 48×48 PNG, transparent bg | [Kenney Pixel Platformer Characters](https://kenney.nl/assets/pixel-platformer) — `tile_0011.png` to `tile_0023.png` are enemy characters |
| Koopa-style enemy sprite | `enemy_koopa.png` | ~32×48 PNG, transparent bg | Same pack — turtle-like enemy tile |
| Hammer Bro enemy sprite | `enemy_hammer_bro.png` | ~32×48 PNG, transparent bg | Same pack or [OpenGameArt platformer enemies](https://opengameart.org/content/platformer-enemies) |

### 🟡 MEDIUM (Cosmetic polish)

| Need | Filename | Specs | Recommended Source |
|------|----------|-------|--------------------|
| Miss Friday dialogue portrait | `portrait_friday.png` | ~80×80 PNG, character face | Crop from existing `player_missfriday.png` |
| Orca dialogue portrait | `portrait_orca.png` | ~80×80 PNG | Crop from `player_Orca.png` |
| Swan dialogue portrait | `portrait_swan.png` | ~80×80 PNG | Crop from `player_Swan.png` |

### 🟢 LOW (Enhancement opportunities)

| Category | Need | Notes |
|----------|------|-------|
| **Tiles** | Platform tileset | Currently drawn as GDI rectangles. Could use Kenney tiles already downloaded |
| **UI** | Button/panel sprites | Currently drawn as GDI rectangles. Kenney UI pack is downloaded but unused |
| **Items** | Berry/coin sprites | Currently drawn as colored ellipses. Could use Kenney coin tiles |
| **Items** | Health pickup sprite | Currently drawn as green rectangles |
| **Items** | Star coin sprite | Currently drawn as yellow stars with GDI |
| **Effects** | Particle sprites | Currently uses colored rectangles for particles |
| **HUD** | Key prompt icons | 821 Kenney input prompt tiles sit unused |

### ⚪ NOT NEEDED (Already covered)

| Category | Status |
|----------|--------|
| Player characters (3) | ✅ Have unique sprites for Friday, Orca, Swan |
| Named enemies (7) | ✅ All have matching sprites |
| Boss (Garp) | ✅ Has sprite + portrait |
| Level backgrounds (17 levels) | ✅ All 17 level IDs have mapped backgrounds |
| Music (12 tracks) | ✅ Complete coverage for all moods |
| SFX (14 effects) | ✅ All referenced sound effects exist |

---

## 5. QUICK WINS — Zero-Download Fixes

These can be done right now using assets already on disk:

### Fix 1: Create missing enemy sprites from Kenney pack
The Kenney Pixel Platformer pack at `Assets/third_party/vendor/kenney/pixel-platformer/Tiles/Characters/` has 27 character tiles. Copy and rename:
```
tile_0011.png → Assets/Sprites/enemy_goomba.png   (mushroom-like enemy)
tile_0015.png → Assets/Sprites/enemy_koopa.png     (turtle-like enemy)  
tile_0019.png → Assets/Sprites/enemy_hammer_bro.png (armored enemy)
```

### Fix 2: Generate dialogue portraits from existing player sprites
Crop the face region of each player sprite and save as 80×80 portrait:
```
player_missfriday.png → portrait_friday.png (cropped)
player_Orca.png       → portrait_orca.png   (cropped)
player_Swan.png       → portrait_swan.png   (cropped)
```

### Fix 3: Clean up duplicates
- Delete `bg_Blade.png` (unused duplicate)
- Delete `player_Miss_Friday.png` (code uses `player_missfriday.png`)

---

## 6. INTEGRATION NOTES

### How SpriteManager works in this project:
1. All sprites load from `Assets/Sprites/` via `SpriteManager.Get("filename.png")`
2. Missing files return `null` — no crashes, but entities render invisible
3. Sprites are cached on first load; pre-scaled copies created via `GetScaled()`
4. No sprite sheet / atlas system — each sprite is a standalone PNG file
5. Backgrounds are pre-scaled to screen resolution at load time

### To add a new sprite:
1. Place the PNG in `Assets/Sprites/`
2. Ensure the file is set to **Copy to Output Directory: Copy if newer** in project properties
3. Reference it in code via `SpriteManager.Get("filename.png")` or `SpriteManager.GetScaled("filename.png", w, h)`
4. No project file changes needed — SpriteManager resolves files at runtime

### Build output note:
Sprites must be in `bin/Debug/Assets/Sprites/` at runtime. The project's build process should copy the Assets folder. Verify the `.csproj` includes:
```xml
<Content Include="Assets\Sprites\*.png">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

---

*This audit was generated by scanning all `.cs` files for sprite/audio references and cross-referencing against actual files on disk.*
