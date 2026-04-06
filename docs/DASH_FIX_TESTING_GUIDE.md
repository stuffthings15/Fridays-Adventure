# Dash Fix - Quick Testing Guide

## Problem Summary
- Dash was moving only ~1cm instead of character lengths
- Character shook in seizure-like jittering motion
- Root cause: Input handler was overwriting dash velocity every frame

## Solution Applied
Added velocity preservation check in `Scenes/IslandScene.cs` HandleInput method:
- When `IsDashing` is true → preserve velocity, ignore input
- When `HasEffect(StatusEffect.Dodging)` is true → preserve velocity, ignore input
- Otherwise → apply normal movement input

## Quick Test Checklist

### Test 1: Swan WingDash Basic
```
1. Start level with Swan character
2. Press E (WingDash)
3. Expected: Swan launches forward ~3.4 character widths
4. NOT expected: Tiny movement, jittering, seizure motion
Result: ✓ PASS or ✗ FAIL
```

### Test 2: Enemy Evasion
```
1. Approach enemy
2. When enemy attacks, press E to dash away
3. Expected: Clear forward movement, successful evasion
4. Expected: No jittering or fighting for control
Result: ✓ PASS or ✗ FAIL
```

### Test 3: Multiple Dashes
```
1. Dash forward (E)
2. Wait ~4 seconds (cooldown)
3. Dash again
4. Expected: Consistent distance both times
5. Expected: Energy properly drained (30 per dash)
Result: ✓ PASS or ✗ FAIL
```

### Test 4: Dash Direction Control
```
1. Press E + Right arrow together
2. Expected: Swan dashes right, no jittering
3. Release both, press E + Left arrow
4. Expected: Swan dashes left, clean motion
Result: ✓ PASS or ✗ FAIL
```

### Test 5: Input Priority During Dash
```
1. Dash forward (E while right held)
2. During dash, press left arrow
3. Expected: Continues forward (dash velocity preserved)
4. NOT expected: Reverses or jitters
Result: ✓ PASS or ✗ FAIL
```

### Test 6: Other Characters
```
1. Test with Orca (uses TidalSlam, 3500 px/s dash)
2. Test with Miss Friday (uses FlashFreeze, not a dash)
3. Expected: All work correctly without regression
Result: ✓ PASS or ✗ FAIL
```

## Visual Indicators of Success
- ✅ Smooth forward motion during dash
- ✅ Clear distance traveled (~3-7 character lengths)
- ✅ No visible jittering or seizure-like shaking
- ✅ Dash initiates and completes cleanly
- ✅ No energy/cooldown issues

## Visual Indicators of Failure
- ❌ Minimal movement (~1cm)
- ❌ Jittering/shaking motion
- ❌ Character appears to fight itself
- ❌ Velocity seems to reset mid-dash
- ❌ Energy drains without visible movement

## Debug Output Expected
When WingDash activates:
1. VelocityX should be set to 620 (or -620)
2. Dodging effect should be active for 0.22s
3. Input should not override velocity for 0.22s
4. After 0.22s, velocity may be replaced by input

## Regression Testing
- ✓ Normal walking still works
- ✓ Jumping unaffected
- ✓ Combat still works
- ✓ Wall jumps unchanged
- ✓ Other abilities unaffected
- ✓ P-Meter sprint unaffected

## If Issues Persist

Check:
1. Is `IsDashing` property actually being set?
2. Is `Dodging` status effect being applied?
3. Are status effects actually expiring after duration?
4. Is HandleInput being called every frame?
5. Is there another place overwriting velocity post-dash?

## File Changes
- `Scenes/IslandScene.cs` - HandleInput method only
- Added ~3 lines of condition checking
- No other files modified
- Fully backward compatible
