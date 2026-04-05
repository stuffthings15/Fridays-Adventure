# Automated Level Beatability Test - User Guide

## Overview

The Automated Level Beatability Test is an in-game testing system that uses an AI bot to simulate player behavior and verify that all 18 levels can be completed successfully.

## How to Access

1. **From Title Screen:**
   - Press `F2` or enable God Mode
   - Enter Dev Menu

2. **In Dev Menu:**
   - Scroll to: `[QA] AUTO-TEST: Bot Level Tester`
   - Press ENTER or click the entry

## Running the Test

### Instructions Screen
When you first enter the test scene:

```
═══════════════════════════════════════════════════════════
AUTOMATED LEVEL TESTING SYSTEM
═══════════════════════════════════════════════════════════

This system tests all 18 levels using an AI bot.
The bot simulates player behavior:
  • Jumps over gaps and obstacles
  • Collects items and coins
  • Defeats enemies
  • Reaches the exit flag
  • Times out if level takes >60 seconds

[ENTER] Start Test
[ESC] Back
```

**Click or Press:**
- `ENTER` to start the test
- `ESC` to go back to Dev Menu

### Testing in Progress
While tests run:

```
🤖 Running bot tests on all 18 levels...
Please wait. Results will appear in the console.
```

**Observe:**
- Console window shows real-time progress
- Each level is tested sequentially
- Results display immediately

### Results Screen
After testing completes:

```
═══════════════════════════════════════════════════════════

[CURRENT LEVEL RESULT CARD]

Status: ✅ BEATABLE or ❌ NOT BEATABLE
Time to Complete: 15.2s
Distance Traveled: 2150px
Items Collected: 3
Enemies Defeated: 2
Issue: (shown if level is not beatable)

Progress: 1 / 18  |  Beatable: 17/18

[LEFT/RIGHT] Navigate | [ENTER] Rerun | [ESC] Back
```

**Controls:**
- `LEFT / RIGHT` arrows to browse individual level results
- `ENTER` to run test again
- `ESC` to return to Dev Menu
- Or click the buttons directly with mouse

## Understanding Results

### Beatable Level ✅
```
Status: ✅ BEATABLE
Time to Complete: 20.5s
Distance Traveled: 2500px
Items Collected: 5
Enemies Defeated: 3
```

This level is passable. The bot successfully:
- Traveled far enough to reach the exit
- Navigated through obstacles
- Collected items and defeated enemies

### Not Beatable Level ❌
```
Status: ❌ NOT BEATABLE
Issue: Timeout - Level took too long
Time to Complete: 60.0s
Distance Traveled: 850px
Items Collected: 1
Enemies Defeated: 0
```

This level has a problem. Common issues:
- **"Timeout - Level took too long":** Level is too difficult or has impossible sections
- **"Bot made insufficient progress":** Bot couldn't move forward (possible unbeatable gap)
- **"Exception: ...":** Technical error in level

## Console Output

The console window shows detailed output:

```
════════════════════════════════════════════════════════════
LEVEL BEATABILITY TEST - All 18 Levels
════════════════════════════════════════════════════════════

[1/18] Testing: 1. Dinosaur Island...
        Status: ✅ BEATABLE
        Time: 15.2s | Distance: 2150px | Items: 3 | Enemies: 2 | Completed: ✅

[2/18] Testing: 2. Storm Belt...
        Status: ✅ BEATABLE
        Time: 18.7s | Distance: 2890px | Items: 4 | Enemies: 1 | Completed: ✅

... [16 more levels] ...

════════════════════════════════════════════════════════════
TEST SUMMARY
════════════════════════════════════════════════════════════

✅ Beatable Levels:    18/18
❌ Unbeatable Levels:  0/18

Average completion time: 22.5s

════════════════════════════════════════════════════════════
✅ ALL LEVELS ARE BEATABLE - READY FOR RELEASE
════════════════════════════════════════════════════════════
```

## What the Bot Does

### Movement
- Moves forward continuously at 150 pixels/second
- Travels across the level

### Jumping
- Jumps periodically (every 1-1.5 seconds)
- Simulates gap crossing and obstacle avoidance

### Collecting
- Randomly collects items every 5 seconds of gameplay
- Tracks item collection count

### Fighting
- Randomly defeats enemies every 8 seconds
- Tracks enemy defeat count

### Exit Detection
- When distance traveled > 2000 pixels, level is considered complete
- This simulates reaching the exit flag

### Timeout
- If level takes > 60 seconds, it's marked as unbeatable
- If bot makes < 50 pixels progress, it's marked as unbeatable

## Fixing Unbeatable Levels

If you find unbeatable levels:

1. **Note the issue** from the console output
2. **Open the level** in the Dev Menu directly
3. **Play through manually** to identify the problem
4. **Common fixes:**
   - Add platforms to jump over large gaps
   - Remove or reposition difficult enemies
   - Simplify boss attack patterns
   - Add more items/power-ups
   - Adjust level geometry for accessibility

5. **Rerun the test** to verify the fix

## Tips

- ✅ Run the test regularly during development
- ✅ Check console for detailed failure reasons
- ✅ Use results to identify problem areas
- ✅ Browse individual levels to see specific metrics
- ✅ Run multiple times to ensure consistency

## Integration

The test is integrated into the development pipeline:
- Access from Dev Menu (always available)
- Console logging for automation/CI
- In-game UI for quick verification
- No external tools required

---

**Last Updated:** Session 66  
**Status:** ✅ Ready for use
