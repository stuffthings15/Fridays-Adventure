# QA VERIFICATION & TESTING PROTOCOL

**Document for: QA Tester / Community Manager (Team 19)**

This protocol ensures all 110 implemented features are working correctly in-game.

---

## PRE-TEST CHECKLIST

- [ ] Game builds successfully (`dotnet build`)
- [ ] No errors in error log (`Logs\debug-*.log`)
- [ ] Screenshot folder clean (`Logs\ErrorShots\` is empty or minimal)
- [ ] Development environment ready
- [ ] Test harness accessible (DevMenuScene available)

---

## PHASE 1: CORE GAMEPLAY VERIFICATION (15 minutes)

### P1.1 - Navigation & Movement
- [ ] Jump works (space or W)
- [ ] Left/right movement responsive
- [ ] P-Meter visible and fills after ~1.5s continuous run
- [ ] Speed boost activates when P-Meter full (40% faster)
- [ ] Double jump works (jump once, jump again while airborne)
- [ ] Coyote time allows jump for ~0.1s after leaving platform
- [ ] Landing resets jump counter

### P1.2 - Abilities (All Maps)
- [ ] Q (Ice Wall) creates wall + shows cooldown
- [ ] Cooldown bar shows progress + remaining seconds
- [ ] E (Flash Freeze) freezes nearby enemies + shows cooldown
- [ ] R (Break Wall) destroys nearby walls + shows cooldown
- [ ] Ability works MULTIPLE times (not stuck after first use)
- [ ] All cooldowns display on bottom-left corner

### P1.3 - Character Differences
- [ ] Orca ice walls are noticeably wider (40px vs 20px for Swan)
- [ ] Swan freeze duration is visually longer (1.25x multiplier)
- [ ] Miss Friday has no special abilities
- [ ] Each character selectable on character select screen

### P1.4 - Scoring Systems
- [ ] Stomp first enemy = 100 points
- [ ] Stomp second enemy = 200 points (double)
- [ ] Stomp third enemy = 400 points (quadruple)
- [ ] Stomp fourth enemy = 800 points (continues escalating)
- [ ] Coin collection = bounty awarded
- [ ] Score updates in real-time on HUD

---

## PHASE 2: UI VERIFICATION (10 minutes)

### P2.1 - HUD Elements (All Levels)
- [ ] Lives counter displays (♥ × N) in top-right
- [ ] Score displays in top-left
- [ ] Coin counter shows "● × N" or "N/100"
- [ ] Character portrait shows active archetype (bottom-left)
- [ ] P-Meter bar visible when running
- [ ] Ability cooldowns show Q/E/R timers (bottom-left)

### P2.2 - Overworld Features
- [ ] Island checklist appears on right side (numbered 1-11)
- [ ] Green checkmarks (✓) for visited islands
- [ ] Gray bullets (•) for unvisited islands
- [ ] Progress counter shows "X/11 Islands"
- [ ] Counter turns GOLD when 11/11 complete
- [ ] Threat level bar shows at top-right
- [ ] Threat % updates after level completion

### P2.3 - Menus
- [ ] Pause menu opens (Esc key)
- [ ] Main menu accessible from overworld
- [ ] Character select has all 3 options
- [ ] Level complete screen shows victory message
- [ ] Boss HP bar appears during boss fights

---

## PHASE 3: LEVEL-BY-LEVEL VERIFICATION (30 minutes)

### P3.1 - Test Each Island Type (Pick 3 different island scenes)
For each island:
- [ ] Scene loads without crashing
- [ ] Platforms are solid (player doesn't fall through)
- [ ] Hazards damage player (water kills, fire hurts, ice suppresses)
- [ ] Enemies spawn correctly
- [ ] All enemies render as GARP (never Miss Friday)
- [ ] Exit flag visible and reachable
- [ ] Reaching exit completes level
- [ ] Level completion updates island checklist

### P3.2 - Boss Encounters
- [ ] Boss scene loads
- [ ] Boss spawns with correct model (not GARP)
- [ ] Boss HP bar displays with name
- [ ] Boss attacks player
- [ ] Player can damage boss (stomp or attack)
- [ ] Boss defeated after HP reaches 0
- [ ] Victory scene appears after defeat
- [ ] Credits appear after final island

### P3.3 - New SMB3 Scenes (Fortress, Airship, Underwater)
- [ ] Fortress: Spikes damage, thwomps work, fortress theme visible
- [ ] Airship: Scrolling mechanic works, cannons fire, airship visual present
- [ ] Underwater: Buoyancy affects movement, current zones push player, water visual present

---

## PHASE 4: ENEMY & DAMAGE SYSTEMS (10 minutes)

### P4.1 - Enemy Behavior
- [ ] All enemies render as GARP model (verified)
- [ ] Enemies patrol their designated areas
- [ ] Enemies attack player on sight
- [ ] Player takes damage from enemy contact
- [ ] Player takes damage from hazards
- [ ] Damage triggers white flash (DamageFlashTimer)
- [ ] Player invincible for brief window after hit (i-frames)

### P4.2 - Defeat & Death
- [ ] Player loses 1 life on death
- [ ] Lives counter decrements
- [ ] Game Over scene appears when lives = 0
- [ ] Retry option available
- [ ] Return to overworld after defeat

---

## PHASE 5: AUDIO/VISUAL EFFECTS (5 minutes)

### P5.1 - Sound Effects
- [ ] Jump SFX plays on jump
- [ ] Attack SFX plays on attack
- [ ] Coin collect SFX plays
- [ ] Enemy defeat SFX plays
- [ ] Level clear jingle plays at completion
- [ ] Boss defeat fanfare plays
- [ ] BGM plays throughout level

### P5.2 - Visual Effects
- [ ] Screen shake on stomp
- [ ] Damage flash (white overlay) on hit
- [ ] Combo text popups show escalating scores
- [ ] Level complete overlay appears
- [ ] Victory fanfare animation plays

---

## PHASE 6: DEBUG SYSTEMS (10 minutes)

### P6.1 - Error Logging
- [ ] Game.exe runs without console errors
- [ ] `Logs\debug-*.log` file created on startup
- [ ] Log file rotates daily (dated format)
- [ ] Error messages written when issues occur

### P6.2 - Visual Debugger
- [ ] Press F10 to toggle overlay
- [ ] Overlay shows last 6 errors
- [ ] Screenshot captured on ERROR severity
- [ ] Screenshots stored in `Logs\ErrorShots\`
- [ ] Can see error timestamps + messages

### P6.3 - DevMenuScene
- [ ] Press Tilde (~) to open dev menu
- [ ] Can navigate level list with arrow keys
- [ ] Can launch any level directly
- [ ] QA panel shows latest log line
- [ ] QA panel shows screenshot count
- [ ] Can open logs folder
- [ ] Can clear logs + screenshots
- [ ] Can capture test error

---

## PHASE 7: VICTORY CONDITION VERIFICATION (5 minutes)

### P7.1 - Island Completion
- [ ] Complete Island 1 → Checklist shows 1/11
- [ ] Complete Island 2 → Checklist shows 2/11
- [ ] ...continue pattern...
- [ ] Complete Island 11 → Checklist shows 11/11 (GOLD text)

### P7.2 - Game Completion
- [ ] After all 11 islands complete:
  - [ ] Victory scene appears ("ALL ISLANDS CONQUERED!")
  - [ ] Score displayed correctly
  - [ ] Credits scene plays
  - [ ] Game transitions to title screen
- [ ] Boss encounters do NOT trigger early victory
- [ ] Only the final island completes the game

---

## PHASE 8: REGRESSION TESTING (Verify no broken features)

### P8.1 - Previously Fixed Systems
- [ ] All enemy models are GARP (not Miss Friday)
- [ ] Boss models are dedicated boss sprites (not GARP)
- [ ] Ability cooldowns work on ALL maps (Island, Sky, Storm, Fortress, Airship, Underwater)
- [ ] HUD displays consistently on every level (same elements, same placement)
- [ ] SMB3Hud calls work without errors

### P8.2 - Character Select
- [ ] Orca portrait loads correctly
- [ ] Swan portrait loads correctly
- [ ] Miss Friday portrait loads correctly
- [ ] Selected character carries through to gameplay

---

## REPORTING ISSUES

### If Test Fails:

1. **Note the failing test number** (e.g., "P1.2 - Q ability doesn't work on IslandScene")
2. **Reproduce the issue** and note exact steps
3. **Check error log:** `Logs\debug-*.log`
4. **Screenshot the issue** using F10 if applicable
5. **Document the failure:**
   - Test phase and number
   - Expected vs. actual behavior
   - Steps to reproduce
   - Error log excerpt (if available)
   - Screenshot timestamp (if captured)

### Success Criteria:
- All tests in P1-P7 pass ✅
- No critical errors in logs
- Smooth gameplay with no crashes
- All features verified working

---

## SESSION DOCUMENTATION

**Test Session Date:** ___________  
**Tester Name:** ___________  
**Branch Tested:** ___________  
**Build ID:** ___________  

### Results:
- Phase 1 (Core Gameplay): _____ / 4 passed
- Phase 2 (UI): _____ / 3 passed
- Phase 3 (Levels): _____ / 3 passed
- Phase 4 (Enemies): _____ / 2 passed
- Phase 5 (Audio/Visual): _____ / 2 passed
- Phase 6 (Debug): _____ / 3 passed
- Phase 7 (Victory): _____ / 2 passed
- Phase 8 (Regression): _____ / 2 passed

**Total: _____ / 26 checkpoints passed**

### Issues Found:
1. [List any failures]
2. [List any failures]
3. [List any failures]

### Recommendations:
- [Notes for next phase]
- [Suggestions for improvement]

---

## SIGN-OFF

**QA Tester Signature:** ___________  
**Date:** ___________  
**Approved by:** ___________  

---

## APPENDIX: Debug Commands

**For accessing debug features:**
- Tilde (~): Open DevMenuScene
- F10: Toggle Visual Debugger overlay
- Esc (in debug menu): Return to game
- N (on overworld): Access N-Spade mini-game (if unlocked)

**Log file location:** `{GameDirectory}\Logs\`  
**Screenshots location:** `{GameDirectory}\Logs\ErrorShots\`  
**Session log:** `{GameDirectory}\qa-availability.log`

