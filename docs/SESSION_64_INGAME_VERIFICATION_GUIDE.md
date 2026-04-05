# Session 64: In-Game Verification Guide
## Complete Level Progression System Testing

**Build Status:** ✅ PASSING (0 errors, 0 warnings)  
**Implementation Date:** April 5, 2026  
**Last Updated:** April 5, 2026  

---

## 📊 COUNTER DISPLAY VERIFICATION

### What You Should See On Overworld Map (Right Side):

```
╔════════════════════════╗
║ CAMPAIGN PROGRESS: 0/11║  ← MAIN COUNTER (Story Islands Only)
║ Islands                ║
╠════════════════════════╣
║ • 1. Dinosaur Island   │
║ • 2. Sky Island        │
║ • 3. Blade Nation      │
│ • 4. Harbor Town       │
│ • 5. Coral Reef        │
│ • 6. Tundra Peak       │
│ • 7. Dive Gate         │
│ • 8. Sunken Gate       │
│ • 9. Kelp Maze         │
│ • 10. Vent Ruins       │
│ • 11. Abyss            │
╚════════════════════════╝

╔════════════════════════╗
║ ALL LEVELS             │
║         ┌─────────┐   │  ← TOTAL COUNTER (All 18 Levels)
║         │ 0 / 18  │   │
║         └─────────┘   │
╠════════════════════════╣
║ ✓ = Completed         │
║ • = Locked            │
║ Gold = Victory        │
║ ████░░░░░░ 0%        │
╚════════════════════════╝
```

### Counter Progression During Gameplay:

| Island Completed | Display | Counter | Progress Bar | Notes |
|---|---|---|---|---|
| Start | • • • • • • • • • • • | 0/11, 0/18 | 0% empty | All bullets (locked) |
| After Island 1 | ✓ • • • • • • • • • • | 1/11, 1/18 | 5% filled | First checkmark |
| After Island 5 | ✓ ✓ ✓ ✓ ✓ • • • • • • | 5/11, 5/18 | 28% filled | Halfway through world 1 |
| After Island 10 | ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ • | 10/11, 10/18 | 56% filled | Almost there! |
| After Island 11 | ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ | **11/11**, 18/18 | **100% GOLD** | VICTORY! |

---

## 🎮 STEP-BY-STEP IN-GAME TEST

### Test 1: Fresh Game Start

1. **Launch Game** → Title Screen
2. **New Game** → Character Select
3. **Select Character** → Difficulty Select
4. **Enter Overworld**
   - [ ] Both counter panels visible on right side
   - [ ] Main panel shows: "CAMPAIGN PROGRESS: 0/11 Islands"
   - [ ] Secondary panel shows: "0 / 18"
   - [ ] All islands show `•` (bullet = locked)
   - [ ] Progress bar is empty/grey

### Test 2: Complete First Island

1. **Click on Dinosaur Island node** → Load IslandScene
2. **Play through level** → Reach exit flag
3. **Complete level** → CourseClearScene plays
4. **Return to Overworld**
   - [ ] Counter now shows: **1/11** (main panel)
   - [ ] Total counter shows: **1/18** (secondary panel)
   - [ ] Dinosaur Island shows `✓` in green
   - [ ] Progress bar ~5% filled
   - [ ] "Progress saved." message appeared

### Test 3: Test Skipping Bosses (Optional)

1. **Skip storm encounters** (don't enter them)
2. **Go directly to Sky Island**
3. **Complete Sky Island**
   - [ ] Counter: **2/11**, **2/18** ← Both increment correctly
   - [ ] "✓" appears next to Sky Island
   - [ ] Skipped boss nodes still show `•`

### Test 4: Rapid Progression

1. **Complete islands in sequence:** Harbor → Coral → Tundra
   - [ ] Counter: **3/11** → **4/11** → **5/11**
   - [ ] Counter: **X/18** increments proportionally
   - [ ] Each completed island changes `•` → `✓`
   - [ ] Progress bar visibly fills

### Test 5: Victory Condition (11/11)

1. **Complete all 11 story islands** (in any order)
2. **Return to Overworld after 11th island**
   - [ ] **Main panel border turns GOLD** ← Critical!
   - [ ] Counter shows: **11/11** in **GOLD TEXT**
   - [ ] Message appears: **"★ ALL ISLANDS CONQUERED ★"** in gold
   - [ ] Progress bar is completely filled, turns gold
   - [ ] Total counter shows: **11-18/18** (boss count varies)

3. **Click any node to proceed**
   - [ ] **VictoryScene** appears
   - [ ] Displays: "ALL ISLANDS CONQUERED!"
   - [ ] Shows: "All 11 Islands Explored"
   - [ ] Click to continue → **CreditsScene**

### Test 6: Color Transitions

Monitor color changes as you progress:

```
0-5 islands:   TEXT = LIME GREEN
5-10 islands:  TEXT = LIME GREEN, COUNTER = CYAN
11 islands:    EVERYTHING = GOLD ★
```

- [ ] Green text while making progress (0-10 islands)
- [ ] Cyan numbers in secondary counter while mid-game
- [ ] Gold appears only at complete victory (11/11)
- [ ] No flickering or color glitches

### Test 7: Save/Load Persistence

1. **Complete 5 islands**
   - [ ] Counter: 5/11, 5/18
   - [ ] Save game (via pause menu or auto-save)

2. **Exit to title, restart game**
   - [ ] Load the saved game
   - [ ] Return to Overworld
   - [ ] Counter still shows: **5/11, 5/18** ← Must persist!
   - [ ] Same islands show `✓`
   - [ ] Progress bar in same state

### Test 8: Boss Counter Verification

1. **Intentionally visit all bosses**: Storm Belt, Blockade, Warlord Sudo, Tempest, Warlord Vanta, Centipede
2. **Complete several bosses**
   - [ ] Boss encounters increment total counter (18)
   - [ ] Main counter (11) only increases for story islands
   - [ ] Total counter (18) increases for both

3. **Example progression:**
   - Complete 3 story islands: 3/11, 3/18
   - Complete 2 bosses: 3/11, 5/18 ← Story stays same, total increases
   - Complete 2 more islands: 5/11, 7/18 ← Both increment

---

## 🐛 KNOWN EDGE CASES TO TEST

### Edge Case 1: Double-Complete Same Island
- **Procedure:** Complete Island 1, return to island, complete it again
- **Expected:** Counter stays 1/11 (no double-counting)
- **Result:** ✅ / ❌

### Edge Case 2: All Bosses, Only 10 Islands
- **Procedure:** Complete all 7 boss encounters, but only 10 story islands
- **Expected:** Counter shows 10/11 (not victory), but 17/18 total
- **Result:** ✅ / ❌

### Edge Case 3: Non-Sequential Completion
- **Procedure:** Complete islands in this order: 11, 5, 2, 8, 3, 1, 9, 7, 6, 4, 10
- **Expected:** Counter increments to 11/11 regardless of order
- **Result:** ✅ / ❌

### Edge Case 4: Rapid Clicking
- **Procedure:** Complete islands very quickly (skip clear scene animations)
- **Expected:** Counter still accurate, no crashes or glitches
- **Result:** ✅ / ❌

---

## 📋 VISUAL CHECKLIST

### HUD Panel Elements:

- [ ] Main panel border is gold (not green) when applicable
- [ ] Secondary panel border is blue/cyan
- [ ] Checkmarks (✓) are bright lime green when complete
- [ ] Bullets (•) are dark gray when locked
- [ ] Island names are white when complete, dark gray when locked
- [ ] Counter font is larger than list font (emphasis on number)
- [ ] Progress bar has visible fill animation
- [ ] Legend text is readable and accurate
- [ ] Victory message uses gold color
- [ ] No text overlap or clipping issues
- [ ] No rendering glitches or artifacts

### Functional Checklist:

- [ ] Counter increments immediately after level completion
- [ ] Counter doesn't increment on failed/quit levels
- [ ] Auto-save message appears
- [ ] Progress persists through save/load
- [ ] Victory screen triggers at exactly 11/11
- [ ] All 18 levels tracked and counted
- [ ] No duplicate counting
- [ ] No missed level tracking
- [ ] Color transitions happen smoothly
- [ ] No lag or performance issues

---

## 🎯 SUCCESS CRITERIA

**Test is PASSING if:**
1. ✅ Counter starts at 0/11, 0/18
2. ✅ Each island completion increments by 1
3. ✅ Victory screen appears at 11/11 exactly
4. ✅ Colors change from green → cyan → gold
5. ✅ Progress bar fills 0-100%
6. ✅ Save/load preserves counter state
7. ✅ All 18 levels counted (bosses + islands)
8. ✅ No glitches, crashes, or visual errors
9. ✅ Gold victory message appears
10. ✅ Game flows correctly: Overworld → Level → Clear → Overworld (repeat)

---

## 📸 SCREENSHOT MARKERS

Take screenshots at these moments:

1. **Fresh overworld (0/11)** - Baseline
2. **After first island (1/11)** - Counter incremented
3. **Mid-game (5/11)** - Cyan counter visible
4. **Almost victory (10/11)** - One more to go
5. **Victory unlocked (11/11)** - Gold everywhere
6. **Victory screen** - Confirmation
7. **Reloaded save** - Counter persisted

---

## 🔗 RELATED CODE LOCATIONS

If debugging is needed, check:
- `Scenes/OverworldScene.cs` - Line 635-730 (DrawIslandChecklist)
- `Scenes/OverworldScene.cs` - Line 100-160 (OnResume for completion tracking)
- `Scenes/OverworldScene.cs` - Line 175-187 (AllIslandsCompleted method)
- `Entities/Player.cs` - Save state persistence
- `Engine/Game.cs` - CurrentLevel counter management

---

**Build Date:** April 5, 2026  
**Status:** Ready for in-game testing  
**Next Step:** Play through game and verify all conditions pass ✓
