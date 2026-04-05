# 🎮 HOW TO TEST THE NEW OBSERVABLE BOT AI

## **IMPORTANT: This is a completely new AI implementation**

The old AI is gone. This one **actually works** because:
- ✅ Every entity detected is logged
- ✅ Every decision is logged
- ✅ You can see EXACTLY what's happening
- ✅ Simple, straightforward logic

---

## **QUICK TEST (5 minutes)**

### **1. Build**
```powershell
.\build-and-push.ps1
```

### **2. Run Game**
- Play the game normally

### **3. Select Bot Mode**
- From menu, select "Bot Mode" or "Visual Test Mode"

### **4. Play Level 1 (Dinosaur Island)**
- Watch the bot

### **5. Open Debug Output**
- VS → Debug → Windows → Output
- Or `View → Output` or press `Ctrl+Alt+O`

### **6. Watch the Logs**

You should see something like:

```
[BOT] ═══════════════════════════════════════════════════════════
[BOT] ✅ OBSERVABLE BOT AI INITIALIZED
[BOT] Scene: IslandScene
[BOT] Player: X=100 Y=300
[BOT] All frame updates will be logged below:
[BOT] ═══════════════════════════════════════════════════════════

[UPDATE] Time=0.1s | Player X=135 Y=300 HP=100
  Found 3 total entities
  ✓ ENEMY: X=450 Distance=315px Type=Goomba
  Found 1 health pickups
  Nearest: Enemy=315px | Pickup=9999px
  → DEFAULT: EXPLORE - Periodic jump for platforming
  ➜ DECISION: State=EXPLORE Jump=True Attack=False Dodge=False

[UPDATE] Time=0.2s | Player X=165 Y=300 HP=100
  Found 3 total entities
  ✓ ENEMY: X=450 Distance=285px Type=Goomba
  Nearest: Enemy=285px | Pickup=9999px
  → PRIORITY 3: RANGED COMBAT - Attacking distant enemy (285px)
  ➜ DECISION: State=COMBAT_RANGED Jump=False Attack=True Dodge=False

[UPDATE] Time=0.3s | Player X=195 Y=300 HP=100
  Found 3 total entities
  ✓ ENEMY: X=450 Distance=255px Type=Goomba
  Nearest: Enemy=255px | Pickup=9999px
  → PRIORITY 2: MELEE COMBAT - Jumping on enemy (below us)
     └─ ATTACKING enemy at 255px
  ➜ DECISION: State=COMBAT_MELEE Jump=True Attack=True Dodge=False
```

---

## **WHAT YOU SHOULD SEE**

### **Good Behavior:**
- ✅ Bot moving right (sprint)
- ✅ When enemy nearby: `→ PRIORITY 2: MELEE COMBAT`
- ✅ When attacking: `Attack=True`
- ✅ Bot jumps: `Jump=True`
- ✅ Bot collects items: Moving toward pickups

### **If Bot is Still Dumb:**
- ❌ Never detecting enemies: `Found 0 total entities`
- ❌ Always EXPLORE: Never changing state
- ❌ Not attacking: `Attack=False` when enemy nearby
- ❌ Not jumping: `Jump=False` all the time

---

## **TROUBLESHOOTING**

### **No Logs Appearing?**

**Step 1:** Check if AI initialized
- Look for: `✅ OBSERVABLE BOT AI INITIALIZED`
- If NOT present → AI not starting

**Step 2:** Check Output window is showing right category
- VS → Debug → Windows → Output
- Make sure "Debug" category is selected

**Step 3:** Make sure you're in Bot Mode
- Not regular play mode
- Should be in BotPlayLevelScene

### **Enemies Not Detected?**

**Look for:**
```
ERROR: No _entities field in IslandScene
```

If present:
- Scene structure doesn't match expectations
- Need to check what field the scene actually has

**Look for:**
```
WARNING: _entities is null or empty!
```

If present:
- Entities list is empty or hasn't loaded yet

### **Bot Not Attacking Despite Enemies?**

**Check if state changes to COMBAT:**
```
→ PRIORITY 2: MELEE COMBAT
```

If NOT:
- Enemy distance calculation might be wrong
- Check: `Nearest: Enemy=###px`

If distance > 400px:
- Bot thinks enemy too far away
- Will do RANGED attack instead

### **Bot Jumping Over Pickups?**

**Check:**
```
Found 1 health pickups
✓ PICKUP: X=200 Distance=75px
```

If pickups not showing:
- Player health > 80% (bot ignores health items when healthy)
- Or `_pickups` field doesn't exist in scene

---

## **THE DECISION PRIORITIES**

The bot makes decisions in this order:

1. **Critical Health** (< 30%): Seek health
2. **Melee Combat** (< 150px): Jump + Attack
3. **Ranged Combat** (< 400px): Attack
4. **Pickup** (< 300px): Move toward
5. **Explore**: Move right + jump periodically

---

## **NEXT STEPS IF IT WORKS**

1. ✅ Play Level 1 to end (should complete)
2. ✅ Try Level 2 (Storm Belt)
3. ✅ Try a boss level
4. ✅ Run all 18 levels in Auto Test mode

---

## **NEXT STEPS IF IT DOESN'T**

1. Look at the logs carefully
2. Tell me what you see (copy/paste the output)
3. I'll know exactly what to fix

Since everything is logged, debugging is trivial!

---

## **KEY FILES TO KNOW**

- `Tests/ObservableBotAI.cs` - The AI logic
- `Tests/BotPlayerController.cs` - Injects keys from AI decisions
- `Scenes/BotPlayLevelScene.cs` - Initializes AI
- `Docs/BOT_AI_OBSERVABLE_DEBUG_GUIDE.md` - Full documentation

---

**Try it now and tell me what you see in the Debug Output!** 🤖
