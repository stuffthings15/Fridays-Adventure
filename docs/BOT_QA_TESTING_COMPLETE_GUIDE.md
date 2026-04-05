# Bot QA Testing Complete Pipeline Guide

## Overview

The bot is now a comprehensive Quality Assurance system that:
- **Plays through the entire game** with synthetic input
- **Tracks every action** (attacks, jumps, pickups, enemies, minigames)
- **Auto-detects failures** (attack cooldown, pickup collisions, stuck states)
- **Generates diagnostic reports** with specific fix recommendations
- **Logs to file** for later analysis

## How to Run a Bot Test

### Step 1: Launch the Game
1. Open the project in Visual Studio
2. Run the game (F5 or Ctrl+F5)

### Step 2: Enter Visual QA Mode
1. From the main menu, press **Key 2** to enter Visual QA Mode
2. OR press **Key 3** for statistical simulation mode (not recommended - estimates only)

### Step 3: Select a Level
- Choose any level from the visual mode menu
- The bot will automatically play it

### Step 4: Watch Bot Play
- The bot will:
  - Sprint right while jumping periodically
  - Attack enemies (~0.5s cooldown)
  - Fire frost balls (~4s cooldown)
  - Collect pickups automatically
  - Navigate through CardRoulette by selecting cards
  - Complete the level
- **Small HUD overlay** shows timer and progress bar

### Step 5: Check Console Output
1. **While playing**: Debug logs show real-time events
   ```
   [ATTACK] Melee attack fired. Cooldown set to 0.50s
   [PICKUP] Berry collected. Value: 1, Total this level: 42
   [CARD_ROULETTE] Card 2 stopped. Face: 3
   ```

2. **After level completes**: Comprehensive diagnostic report prints
   ```
   COMPREHENSIVE BOT TEST DIAGNOSTIC REPORT
   LEVEL: Dinosaur Island (dino)
   COMPLETION: ✅ PASSED
   TIME SPENT: 45.2s
   
   STATISTICS:
     Attacks: 25 fired, 3 blocked
     Pickups: 42 collected, 0 missed
     Enemies: 8 defeated
     Minigames: 1 (CardRoulette)
   ```

### Step 6: Review Diagnostic Report
- **Console output** shows immediate summary
- **File saved** to `Logs/bot-tests/bot-test-{levelId}-{timestamp}.txt`
- **Report includes**:
  - Statistics (attacks, jumps, pickups, enemies)
  - Detailed timeline of every event
  - Issues detected (if any)
  - Automatic recommendations to fix issues

## Understanding Diagnostic Reports

### Report Sections

**STATISTICS**
```
Attacks: 25 fired, 3 blocked
Jumps: 45
Pickups Collected: 42
Pickups Missed: 0
Enemies Defeated: 8
Minigames: 1
FAILURES: 0
```
- Shows what the bot successfully did
- "Blocked" means cooldown prevented action
- "Missed" means pickup wasn't collected

**ISSUES DETECTED**
Examples of automatic issue detection:
```
⚠️  CRITICAL: Attack ability blocked too frequently
   → Too many attacks blocked means cooldown is broken

⚠️  CONCERN: Pickups missed
   → Collision detection issue - hitbox isn't overlapping

⚠️  CRITICAL: CardRoulette started but no cards selected
   → Input not working - cards won't advance

⚠️  CRITICAL: Bot stuck in minigame for 45.2s
   → Minigame hung - scene not advancing
```

**DETAILED TIMELINE**
Shows every event chronologically:
```
[0.05s] ABILITY    JUMP                 (jump event)
[0.50s] PICKUP     BERRY                Value: 1
[1.20s] ABILITY    ATTACK_FIRED         (melee attack)
[2.30s] ENEMY      DEFEATED             Goomba
[15.00s] MINIGAME  CARD_ROULETTE_START  (CardRoulette entered)
[15.50s] MINIGAME  CARD_SELECTED        Card 1
[16.00s] MINIGAME  CARD_SELECTED        Card 2
[16.50s] MINIGAME  CARD_ROULETTE_COMPLETE
[45.00s] STATE     LEVEL_COMPLETE
```

**AUTOMATIC RECOMMENDATIONS**
Specific fixes based on detected issues:
```
→ Check Character.TryAttack() cooldown - may be too restrictive
→ Verify Hitbox collision detection in UpdateBerries()
→ CardRoulette input handler broken - verify InteractPressed check
```

## Common Issues & Fixes

### Issue: Attack Only Fires Once
**Symptom**: Report shows "Attacks: 1 fired, 100+ blocked"

**Possible Causes**:
1. `AttackCooldownMax` set to value > 60s (max level time)
2. `IsAttacking` flag not being reset properly
3. Character in special state blocking attacks

**Fix**:
1. Check `Entities/Character.cs`: `public float AttackCooldownMax = 0.5f;`
2. Verify `TickCooldowns()` properly decrements `AttackCooldown`
3. Check `Player.TakeDamage()` - if bot dies, all abilities stop

---

### Issue: Pickups Not Collected
**Symptom**: Report shows "Pickups Missed: 5" or "Collected: 0"

**Possible Causes**:
1. Berry hitbox too small or positioned incorrectly
2. Player hitbox not overlapping with pickup
3. Collection code not being called

**Fix**:
1. Check `Scenes/IslandScene.cs` `UpdateBerries()` - verify `_player.Hitbox.IntersectsWith(b.Hitbox)`
2. Verify berry spawn positions are within level bounds
3. Run bot and check console - should log `[PICKUP] Berry collected`

---

### Issue: Bot Stuck on CardRoulette
**Symptom**: Report shows "CRITICAL: CardRoulette started but no cards selected"

**Possible Causes**:
1. Input check broken - not detecting Enter/Interact key
2. `InteractPressed` not mapped to correct key
3. Scene in wrong state

**Fix**:
1. Check `Scenes/CardRouletteScene.cs`: Input condition should be:
   ```csharp
   if (!_resultShown && (JumpPressed || InteractPressed || AttackPressed))
   ```
2. Verify `InputManager` properly maps keys to `InteractPressed`
3. Ensure `_resultShown` starts as false

---

### Issue: Minigame Not Advancing
**Symptom**: Report shows "Bot stuck in minigame for 60.0s"

**Possible Causes**:
1. Minigame scene not responding to input
2. `SceneTransition.Begin()` not being called
3. Callback not set properly

**Fix**:
1. Add logging to minigame's `Update()` to verify input is detected
2. Check `_onContinue` callback is set and not null
3. Verify `SceneTransition` system is working

---

## Report File Locations

All diagnostic reports are saved to:
```
{GameDirectory}/Logs/bot-tests/bot-test-{levelId}-{timestamp}.txt
```

Example:
```
C:\...\Logs\bot-tests\bot-test-dino-2024-01-15-143022.txt
C:\...\Logs\bot-tests\bot-test-sky-2024-01-15-143045.txt
```

You can:
- **Compare reports** across levels to identify patterns
- **Archive reports** for debugging specific level issues
- **Share reports** with team to show what works/doesn't work

## QA Workflow

### Daily Testing Routine
1. Launch game in Visual QA Mode
2. Play through each level with bot
3. **Check console** for any failures
4. **Review report file** - look for ⚠️ warnings
5. **Note failures** - common issues mean bugs to fix
6. **Repeat next day** after fixes

### Regression Testing
1. After a code change, run bot on affected levels
2. Compare new reports with previous runs
3. If new failures appear, the change broke something
4. If failures disappeared, the fix worked!

### Comprehensive Test (All Levels)
1. Could be automated with a loop, but currently manual
2. Takes ~15-30 minutes depending on level difficulty
3. Provides full coverage of entire game pipeline

## Technical Details

### Event Categories Tracked

| Category | Events | Purpose |
|----------|--------|---------|
| INPUT | Key presses | Verify bot input injection working |
| ABILITY | Attack, Jump, FrostBall | Verify cooldowns and firing |
| PICKUP | Berry, Health, PowerUp | Verify collection system |
| ENEMY | Encounter, Defeated | Verify combat working |
| SCENE | Transition, Complete | Verify level progression |
| STATE | Timeout, Complete | Verify game state |
| HEALTH | Status, Damage | Track health changes |
| MINIGAME | CardRoulette, Results | Verify minigame mechanics |

### Automatic Issue Detection

The logger automatically flags issues when:
- Attack blocked > 2x fired (cooldown problem)
- Pickups missed > 0 (collision problem)
- CardRoulette started but no cards selected (input problem)
- Minigame > 30s without completing (stuck)
- Level > 90s timeout (too hard)
- Attack = 0 (completely broken)
- Enemies = 0 AND not completed (no combat)

## Tips for QA Success

✅ **DO:**
- Test each level multiple times
- Keep reports organized by date
- Note patterns (all water levels failing?)
- Test after every code change
- Check console while playing

❌ **DON'T:**
- Ignore warnings in console output
- Rely on statistical mode (simulation, not real)
- Run tests with GodMode off (affects results)
- Assume one passing test means it's fixed

## Key Files

- `Tests/BotPlayLevelScene.cs` - Main bot orchestration
- `Tests/BotPlayerController.cs` - Input injection logic
- `Tests/BotDiagnostics.cs` - Simple event tracker
- `Tests/BotComprehensiveTestLogger.cs` - Advanced analyzer
- `Scenes/CardRouletteScene.cs` - Minigame handling
- `Scenes/IslandScene.cs` - Pickup collection

---

**Questions?** Check console output or review report file for specific issues!

