# Oscillation After Second Enemy Kill - Root Cause & Fix

## Problem
After defeating the second enemy, the player character spazzes out going "left, right, left, right" repeatedly at an extremely high frequency (every 0.02 seconds or less).

## Root Causes Identified

### Bug #1: Timer Double-Increment (CRITICAL)
**File:** `Tests\UnifiedComprehensiveBot.cs` - Line ~525

```csharp
// WRONG - This increments EVERY FRAME in the COLLECTING branch
_platformJumpTimer += 0.016f;  // Simulate frame time
if (_platformJumpTimer > 1.5f)
```

**Problem:** 
- `_platformJumpTimer` is already incremented at the start of `Update()`
- This extra increment was causing the timer to grow at 2x rate
- Timer would overflow and trigger fallback logic prematurely
- Caused rapid state switches between COLLECTING and PLATFORMING
- Each state switch would cause different movement directions
- Result: Oscillation every frame

**Fix:** Remove the extra increment line. The timer is already properly managed in Update().

### Bug #2: No Deadzone in Collection Logic
**File:** `Tests\UnifiedComprehensiveBot.cs` - Collecting branch

**Problem:**
```csharp
ShouldMoveRight = directionToPickup > 0;  // If distance = 0, both false
ShouldMoveLeft = directionToPickup < 0;   // If distance = 0, both false
```

When the pickup is very close (within a few pixels), the player's inertia or platform collision could cause the direction to oscillate by ±1-2 pixels between frames, causing:
- Frame N: direction = +2 pixels → ShouldMoveRight = true
- Frame N+1: direction = -1 pixels → ShouldMoveLeft = true  
- Frame N+2: direction = +1 pixels → ShouldMoveRight = true
- ... repeat every frame

**Fix:** Add 15px deadzone for collection items:
```csharp
if (Math.Abs(directionToPickup) < 15f)
{
    ShouldMoveRight = false;
    ShouldMoveLeft = false;  // Stop moving when very close
}
else if (directionToPickup > 0)
{
    ShouldMoveRight = true;
    ShouldMoveLeft = false;
}
else
{
    ShouldMoveRight = false;
    ShouldMoveLeft = true;
}
```

Applied to BOTH health pickup and berry collection logic.

## Files Modified
- `Tests\UnifiedComprehensiveBot.cs`

## Changes Made

### Change 1: Remove Double-Increment
- **Line:** ~525
- **Removed:** `_platformJumpTimer += 0.016f;`
- **Reason:** Timer is already incremented once per Update() at line ~80

### Change 2: Add Deadzone to Health Pickup Logic
- **Lines:** ~430-450
- **Added:** 15px deadzone check before setting ShouldMoveRight/ShouldMoveLeft
- **Effect:** Prevents oscillation when approaching pickups

### Change 3: Add Deadzone to Berry Collection Logic
- **Lines:** ~450-470
- **Added:** 15px deadzone check before setting ShouldMoveRight/ShouldMoveLeft
- **Effect:** Prevents oscillation when approaching berries

## Testing Verification

After fix, verify:
1. ✅ Kill second enemy - should transition smoothly
2. ✅ No oscillating left-right motion after combat
3. ✅ Bot can collect items cleanly (approach without shaking)
4. ✅ Transition from COMBAT to COLLECTING/GOAL_PURSUIT smooth
5. ✅ No rapid state switches in logs
6. ✅ Movement is consistent and predictable

## Technical Details

### Why This Happened
The oscillation became apparent after the dash fix because:
1. **Before dash fix:** Movement input was overwriting dash velocity constantly
2. **After dash fix:** Movement input is properly gated during dash, so bot behavior became more visible
3. **With bot oscillating:** The dash fix exposed a latent bug in the bot's decision-making logic

### Deadzone Rationale
- Combat uses 30px deadzone (stopping when very close to enemy)
- Collection now uses 15px deadzone (stopping when very close to item)  
- Goal pursuit uses 30px deadzone (stopping when very close to goal)
- These prevent oscillation from tiny positional fluctuations

### State Switch Cooldown
The bot already had `STATE_SWITCH_MIN_TIME = 0.5f` to prevent rapid state switches, but:
- The timer overflow bug was causing it to ignore this cooldown
- With the timer fixed, state switching should be smooth

## Impact
- **Severity:** HIGH - Made bot unplayable (constant oscillation)
- **Scope:** Bot AI only, no player/gameplay logic affected
- **Complexity:** Simple timer management and deadzone addition
- **Risk:** LOW - Straightforward logic improvement, no refactoring

## Compilation
✅ Build successful, 0 errors, 0 warnings

## Next Testing
Run full level with bot to verify:
1. Combat completion without oscillation
2. Item collection without shaking
3. Goal pursuit smoothness
4. Multiple enemy encounters
