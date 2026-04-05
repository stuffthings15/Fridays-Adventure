# Bot Diagnostics Quick Reference

## Overview
When you run Visual QA Mode (key 2), the bot now outputs a comprehensive diagnostic report on exit showing exactly what happened during the level.

## Running the Test
1. Open the game in Visual Studio  
2. Start a level
3. Press key **2** to enter Visual QA Mode (bot testing)
4. Watch the bot play the level
5. When the level ends or times out, check the **Console Output**
6. The diagnostic report will print automatically

## Reading the Report

### OVERALL STATS Section
```
Level Completed: ✅ YES (or ❌ NO)
Time Spent: 23.4s
Items Collected: 12
Enemies Defeated: 3
Attacks Fired: 18
Jumps Performed: 12
Failure Reason: (if applicable)
```

**What to look for:**
- **Level Completed: ❌** = bot didn't reach exit in time
- **Attacks Fired: 0** = attack ability isn't working
- **Items Collected: 0** = collection zones not detected/triggered
- **Jumps Performed: 0** = jump isn't triggering
- **Time Spent: >90s** = took too long (likely stuck somewhere)

### DETAILED TIMELINE Section
Shows every major action with timestamp:
```
[0.00s] INPUT      HOLD    Key.Right (bot sprinting)
[0.00s] ABILITY    SUCCESS Jump (total: 1) key injected
[2.34s] ABILITY    SUCCESS Attack (total: 1) key injected
[2.95s] ABILITY    SUCCESS Jump (total: 2) key injected
[5.00s] ENEMY      DEFEATED Goomba (total defeated: 1)
[8.50s] COLLECTION ITEM    Berries (total items: 5)
[45.00s] SCENE     TRANSITION IslandScene → CardRouletteScene
[45.30s] MINIGAME  ADVANCE   CardRouletteScene
[46.00s] SCENE     TRANSITION CardRouletteScene → CourseClearScene
```

**What each type means:**
- **INPUT HOLD Key.Right** = bot is moving right (should be constant)
- **ABILITY SUCCESS Jump** = bot attempted jump (check frequency ~every 0.55s)
- **ABILITY SUCCESS Attack** = bot attempted attack (check frequency)
- **ENEMY DEFEATED** = enemy hit and defeated
- **COLLECTION ITEM** = item collected (berries, health, coins)
- **SCENE TRANSITION** = moved to a different scene
- **MINIGAME ADVANCE** = interacted with CardRoulette/result screen

### DIAGNOSTIC ANALYSIS Section
Shows detected problems:
```
⚠ WARNING: Attack ability never fired (check cooldown or input blocking)
⚠ WARNING: No items collected (check collection zones)
⚠ WARNING: Excessive scene transitions (10+) — possible infinite loop
```

## Troubleshooting Guide

### Bot Attacks Never Fire
**Problem:** `Attacks Fired: 0`

**Causes:**
1. Player.TryAttack() cooldown too long
2. Attack input not being registered
3. Fire Flower cooldown stuck

**Fix:**
- Check `Character.AttackCooldownMax` (should be ~0.5s)
- Check if `IslandScene.HandleInput()` properly calls `TryAttack()`
- Check `_fireballShotCooldown` isn't permanently stuck

### Bot Doesn't Jump
**Problem:** `Jumps Performed: 0` or very few

**Causes:**
1. Jump input not registered
2. Bot stuck on ground (collision issue)
3. Jump interval too long

**Fix:**
- Check `BotPlayerController.JumpInterval` = 0.55s
- Check `IslandScene.HandleInput()` calls `TryJump()`
- Verify player isn't stuck in falling/flying state

### No Items Collected
**Problem:** `Items Collected: 0`

**Causes:**
1. Item collection zones not spawned
2. Collision detection not working
3. Items not being added to scene

**Fix:**
- Check item spawning in level constructor
- Verify `Player.OnTriggerStay()` calls `CollectItem()`
- Check item positions match bot path

### Bot Gets Stuck on CardRoulette
**Problem:** Scene transitions show CardRoulette but never advances past it

**Causes:**
1. CardRoulette doesn't respond to Enter key
2. Mini-game interaction not implemented
3. Scene stack not being managed properly

**Fix:**
- Bot should inject Enter key every 0.3s (check `BotPlayLevelScene`)
- CardRoulette must handle KeyPress events
- Verify scene pops after selection

### Takes >90 seconds (Timeout)
**Problem:** `Time Spent: >90s` and Level not completed

**Causes:**
1. Bot stuck on platform/obstacle
2. Infinite enemy spawning
3. Level layout impassable

**Fix:**
- Check level geometry for dead ends
- Verify enemy spawning is reasonable
- Test with human player to see if beatable

## Example: Analyzing a Failed Test

```
OVERALL STATS:
  Level Completed: ❌ NO
  Time Spent: 90.0s
  Items Collected: 8
  Enemies Defeated: 2
  Attacks Fired: 0
  Jumps Performed: 25

[0.00s] INPUT      HOLD    Key.Right
[0.00s] ABILITY    SUCCESS Jump (total: 1) key injected
[2.34s] ABILITY    SUCCESS Jump (total: 2) key injected
[4.50s] ENEMY      DEFEATED Goomba (total defeated: 1)
[8.00s] COLLECTION ITEM    Berries (total items: 2)
[25.30s] SCENE     TRANSITION IslandScene → WaterZoneScene
[55.00s] COLLECTION ITEM    Berries (total items: 8)
[89.99s] STATE      CHANGE   Running → Failed (timeout)

⚠ WARNING: Attack ability never fired (check cooldown or input blocking)
```

**Analysis:**
- Bot is moving and jumping ✅
- Bot is collecting items ✅
- Bot defeated 2 enemies ✅
- Bot **never attacked** (0 attacks) ❌
- Bot hit 90s timeout and failed ❌
- Entered water zone at 25s mark

**Conclusion:** Attack ability isn't firing. Likely causes:
1. IslandScene.HandleInput() isn't calling TryAttack()
2. Fire Flower is active and cooldown stuck
3. Input mask is blocking Z key in water zone

**Next Step:** Check `IslandScene.HandleInput()` around line 1155 to verify attack handling.

