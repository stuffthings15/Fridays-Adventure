# Self-Healing Asset Pipeline — System Guide

## Overview

The **Self-Healing Asset Pipeline** is a fully automated system that detects missing
game assets at runtime, resolves them from local vendor packs or by generating
placeholders, validates the results, and produces a detailed report. It can run
in an unattended loop via the PowerShell orchestrator script.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    GAME RUNTIME                               │
│                                                              │
│  SpriteManager.Get()  ──miss──►  AssetGapDetector.RecordMiss │
│  AudioManager.PlayFile() ─miss─► AssetGapDetector.RecordMiss │
│  ProceduralSfx.EnsureLoaded() ─► AssetGapDetector.RecordMiss │
│                                                              │
│  LoadingScene ──► AssetHealingPipeline.RunFullPipeline()     │
│      │               │                                       │
│      │               ├─ Phase 1: Scan known references       │
│      │               ├─ Phase 2: Resolve via AssetAutoResolver│
│      │               ├─ Phase 3: Validate resolved files     │
│      │               └─ Phase 4: Generate report             │
│      │                                                       │
│      └─► (--heal-assets mode) Application.Exit()             │
│                                                              │
│  Game.Stop() ──► Flush latest_healing_report.txt             │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                 ORCHESTRATOR SCRIPT                           │
│           Tools/AssetHealingLoop.ps1                         │
│                                                              │
│  for cycle = 1..MaxCycles:                                   │
│    1. MSBuild project                                        │
│    2. Launch game --heal-assets                              │
│    3. Wait for exit (90s timeout)                            │
│    4. Read latest_healing_report.txt                         │
│    5. If unresolved == 0 → done                              │
│    6. Else → next cycle                                      │
└──────────────────────────────────────────────────────────────┘
```

---

## Components

### 1. AssetGapDetector (`Data/AssetGapDetector.cs`)
- **Thread-safe** dictionary of all missing asset references
- Records filename, category, miss count, first-miss timestamp, keywords
- `RecordMiss(fileName)` — called by SpriteManager, AudioManager, ProceduralSfx
- `GetUnresolvedMisses()` — snapshot of unresolved entries
- `GetAllMisses()` — all entries including resolved
- `MarkResolved(fileName, path)` — marks an entry as resolved
- `GenerateReport()` — plaintext report with resolution status

### 2. AssetAutoResolver (`Data/AssetAutoResolver.cs`)
- **Strategy 1: Known Mapping** — maps specific filenames to Kenney vendor tiles
- **Strategy 2: Keyword Search** — searches vendor dirs for filename keyword matches
- **Strategy 3: Placeholder Generation** — creates colored placeholder PNGs at runtime
- `ResolveAllGaps()` — resolves all unresolved misses using 3-strategy cascade
- `InvalidateIndex()` — refreshes the vendor file index

### 3. AssetHealingPipeline (`Data/AssetHealingPipeline.cs`)
- Orchestrates the full scan → resolve → validate → report cycle
- **Phase 1:** Scans known sprite/audio references against disk
- **Phase 2:** Calls AssetAutoResolver to fix all gaps
- **Phase 3:** Validates resolved files (checks they exist, images aren't corrupt)
- **Phase 4:** Generates and saves the report
- `RunFullPipeline()` — runs all phases synchronously
- `RunFullPipelineAsync(onStatus)` — runs with status callbacks

### 4. Audio Hooks
- **AudioManager.PlayFile()** — records miss when music file not found
- **ProceduralSfx.EnsureLoaded()** — records miss when SFX WAV not found

### 5. Game Integration
- **`--heal-assets` CLI flag** — enables auto-heal mode
- **LoadingScene** — runs pipeline after audio/asset init; auto-exits in heal mode
- **Game.Stop()** — flushes `latest_healing_report.txt` on every exit

### 6. PowerShell Orchestrator (`Tools/AssetHealingLoop.ps1`)
- Builds project → launches in heal mode → reads report → repeats if needed
- Configurable max cycles (default 5)
- MSBuild auto-detection for VS 2022/2019/18
- `-SkipBuild` flag for iteration without recompiling

---

## Usage

### Manual (In-Game)
1. Open **Dev Menu** → click **"Self-Heal Assets"**
2. Pipeline runs immediately and shows results in a toast

### CLI (Single Run)
```powershell
& "bin\Debug\Fridays Adventure.exe" --heal-assets
```
Game launches, runs healing pipeline, writes report, and exits.

### Automated Loop
```powershell
.\Tools\AssetHealingLoop.ps1                   # default 5 cycles
.\Tools\AssetHealingLoop.ps1 -MaxCycles 10     # up to 10 cycles
.\Tools\AssetHealingLoop.ps1 -SkipBuild        # skip MSBuild
```

### Report Location
- **Per-run reports:** `bin\Debug\Logs\asset-healing\healing_report_YYYYMMDD_HHMMSS.txt`
- **Latest report:** `bin\Debug\Logs\asset-healing\latest_healing_report.txt`

---

## Asset Sources (CC0 Only)

| Source | License | Content |
|--------|---------|---------|
| Kenney Pixel Platformer | CC0 | Characters, tiles, items |
| Kenney Blocks | CC0 | Block tiles |
| Kenney UI Pack | CC0 | UI elements |
| Kenney Input Prompts | CC0 | Keyboard/gamepad icons |
| OpenGameArt.org | CC0 | Various game assets |

All non-CC0 and non-public-domain assets are **rejected** by the pipeline.

---

## Constraints

- Does **NOT** download paid assets
- Does **NOT** overwrite existing valid assets
- Does **NOT** require internet after first-launch download
- Placeholder generation is deterministic (same filename = same color)
- All operations are non-destructive and idempotent
