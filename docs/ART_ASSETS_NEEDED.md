# ART ASSETS NEEDED — FRIDAY'S ADVENTURE II
## What to Import to Make the Game More Palatable

All assets go in `Assets\Sprites\` unless a sub-folder is specified.
The game already loads by exact file name — match names exactly.

---

## 🎭 CHARACTER SPRITES (Critical — drawn as coloured rectangles now)

| File Name | What It Is | Priority |
|-----------|-----------|----------|
| `Assets\Sprites\player_Miss_Friday.png` | Miss Friday full-body sprite (48×81 px recommended) | 🔴 HIGH |
| `Assets\Sprites\player_Orca.png` | Orca full-body sprite (48×81 px) | 🔴 HIGH |
| `Assets\Sprites\player_Swan.png` | Swan full-body sprite (48×81 px) | 🔴 HIGH |

**Style guide:** SMB3-inspired pixel art, side-view, transparent background (PNG).
Colours: Miss Friday = teal/white, Orca = dark blue/silver, Swan = white/gold.

---

## 🌍 ISLAND BACKGROUNDS (High Impact — shown full-screen during levels)

Folder: `Assets\Backrounds\` *(note: single 'r' — matches existing code path)*

| File Name | Scene | Suggested Palette |
|-----------|-------|------------------|
| `Dinosaur Island.png` | Dinosaur Island | Lush green jungle, warm amber sky |
| `Ancient ruins island.png` | Sky Island | Pastel sky blue, floating stone ruins |
| `Blade Nation.png` | Blade Nation (Wano) | Deep crimson + gold, Japanese castle |
| `Harbor Town.png` | Harbor Town | Sandy docks, teal sea, warm sunset |
| `Coral Reef.png` | Coral Reef | Turquoise water, pink/orange coral |
| `Tundra Peak.png` | Tundra Peak | Icy white/blue, snow-capped peaks |
| `Dive Gate.png` | Dive Gate | Dark teal gradient, submarine glow |
| `Sunken Gate.png` | Sunken Gate | Deep blue, broken stone arches |
| `Kelp Maze.png` | Kelp Maze | Dark green water, swaying kelp |
| `Vent Ruins.png` | Vent Ruins | Orange volcanic glow, dark rock |
| `Abyss.png` | Abyss | Near-black, faint bioluminescence |
| `Warlord Sudo.png` | Fire Lord Sudo boss | Molten lava chamber |
| `Warlord Vanta.png` | Storm Lord Vanta boss | Purple stormclouds, lightning |
| `Centipede of the Deep.png` | Centipede boss | Deep-sea void, faint glowing cracks |
| `Marine Blockade.png` | Fortress / Blockade | Grey warship deck, ocean horizon |
| `Tempest Strait.png` | Airship / Storm2 | Dark storm, airship silhouette |

**Recommended size:** 900×510 px (canvas size). PNG or JPG both accepted.

---

## 🗺️ OVERWORLD MAP

| File Name | What It Is |
|-----------|-----------|
| `Assets\Sprites\bg_overworld.png` | World map background (900×600) — ocean with island silhouettes. Currently drawn procedurally (blue gradient). A painted map image dramatically improves first impressions. |

---

## 🎨 TITLE SCREEN

| File Name | What It Is |
|-----------|-----------|
| `Assets\Sprites\bg_title.png` | Title screen background (900×600). Key art — ship at sea, dramatic sky. |
| `Assets\Sprites\bg_deck.jpg` | Fallback title background (ship deck). |

---

## 👾 ENEMY SPRITES (Medium Priority — enemies drawn as coloured boxes now)

| File Name | Enemy | Size |
|-----------|-------|------|
| `Assets\Sprites\enemy_marine.png` | Standard marine soldier | 36×56 px |
| `Assets\Sprites\enemy_armored.png` | Armored marine variant | 40×60 px |
| `Assets\Sprites\enemy_goomba.png` | SMB3 Goomba (Phase 3) | 32×32 px |
| `Assets\Sprites\enemy_koopa.png` | SMB3 Koopa (Phase 3) | 32×40 px |
| `Assets\Sprites\enemy_hammer_bro.png` | Hammer Bro (Phase 3) | 36×52 px |

---

## 🍄 POWER-UP / ITEM ICONS (Medium Priority — shown in HUD)

| File Name | Item |
|-----------|------|
| `Assets\Sprites\item_mushroom.png` | Super Mushroom (32×32) |
| `Assets\Sprites\item_fireflower.png` | Fire Flower (32×32) |
| `Assets\Sprites\item_leaf.png` | Super Leaf / Raccoon (32×32) |
| `Assets\Sprites\item_star.png` | Invincibility Star (32×32) |
| `Assets\Sprites\item_pwing.png` | P-Wing (32×32) |

---

## 🎵 AUDIO ASSETS (Highly Recommended — game is currently silent)

Folder: `Assets\Audio\`

| File Name | What It Is |
|-----------|-----------|
| `music_overworld1.mp3` | Overworld map theme (looping) |
| `music_island1.mp3` | Generic island level theme |
| `music_island2.mp3` | Second island theme variation |
| `music_boss1.mp3` | Boss battle theme |
| `music_toadhouse1.mp3` | Toad House / bonus room jingle |
| `music_hub1.mp3` | Character select / crew screen |
| `music_victory1.mp3` | Victory / course clear fanfare |

**Format:** MP3, 128–192 kbps, looping tracks should have clean loop points.
All SMB3-style chiptune or orchestral-chiptune arrangements work great.

---

## 🎯 QUICK WINS (Import These First for Maximum Impact)

1. **`bg_title.png`** — Players see this first. Best ROI.
2. **`bg_overworld.png`** — Second thing players see.
3. **`Dinosaur Island.png`** — First level background.
4. **`player_Miss_Friday.png`** — Default character sprite.
5. **`music_overworld1.mp3`** — Immediate atmosphere improvement.
6. All 11 island backgrounds (bulk import).

---

## 📐 TECHNICAL SPECIFICATIONS

| Property | Value |
|----------|-------|
| Canvas size | 900 × 600 px |
| Player sprite size | 48 × 81 px |
| Enemy sprite size | 32–40 × 40–60 px |
| Background size | 900 × 510 px (bottom 90px is HUD band) |
| Format | PNG (transparent sprites), PNG or JPG (backgrounds) |
| Color depth | 32-bit RGBA for sprites |
| Style | SMB3-inspired pixel art OR clean 2D cartoon |

---

## 🔧 HOW TO ADD ASSETS

1. Copy files into the correct `Assets\` sub-folder
2. Right-click the file in Visual Studio Solution Explorer → **Properties**
3. Set **Copy to Output Directory** → **Copy if newer**
4. Rebuild — the game loads assets at runtime from the output folder

No code changes needed for any asset in this list — all paths are already hardcoded.
