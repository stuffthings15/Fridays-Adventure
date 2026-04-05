# 🔍 BOT AI DEBUG - LOG CHECKING GUIDE

## **THE LOGS WILL NOW TELL YOU EXACTLY WHAT'S WRONG**

The ObservableBotAI now logs **EVERY FRAME** so you can see exactly what's happening.

---

## **HOW TO RUN IT & CHECK LOGS**

### **Step 1: Build**
```powershell
.\build-and-push.ps1
```

### **Step 2: Run Game in Bot Mode**
- Select Level 1 (Dinosaur Island)
- Bot Mode

### **Step 3: Open Debug Output**
- VS → Debug → Windows → Output
- Or `Ctrl+Alt+O`

### **Step 4: Watch the Logs**

---

## **WHAT TO LOOK FOR**

### **Good Initialization:**
```
╔═══════════════════════════════════════════════════════════════╗
║ BOT INITIALIZATION                                            ║
╚═══════════════════════════════════════════════════════════════╝
Scene: IslandScene
Player: X=100 Y=300 HP=100
[BOT] ✅ ObservableBotAI initialized!
```

**If you DON'T see this:** AI not initializing!

### **Good Detection (Every Frame):**
```
[UPDATE] Time=0.1s | Player X=135 Y=300 HP=100
  🔍 Searching 3 entities...
    ✓ ENEMY #1: X=450 Distance=315px Type=Goomba
  🔍 Found 1 health pickups
    ✓ PICKUP #1: X=200 Distance=100px
  Nearest: Enemy=315px | Pickup=100px
  → DEFAULT: EXPLORE - Move right and jump periodically
  ➜ ACTION: Jump=False | Attack=False | Move=True | State=EXPLORE

[UPDATE] Time=0.2s | Player X=165 Y=300 HP=100
  🔍 Searching 3 entities...
    ✓ ENEMY #1: X=450 Distance=285px Type=Goomba
  🔍 Found 1 health pickups
  → PRIORITY 3: RANGED COMBAT - Attacking distant enemy (285px)
  ➜ ACTION: Jump=False | Attack=True | Move=True | State=COMBAT_RANGED
```

**If you see this:** Detection IS working!

---

## **IF DETECTION ISN'T WORKING**

### **Problem 1: "ERROR: Scene IslandScene has NO _entities field!"**

**This means:** The scene doesn't use `_entities` field

**Solution:** Check the actual scene to see what field it uses
- Open `Scenes/IslandScene.cs`
- Look for a field like `_enemies`, `_entities`, `_actors`, etc.
- Update ObservableBotAI to use the correct field name

**Current code to update in ObservableBotAI.cs:**
```csharp
var entitiesField = _scene.GetType().GetField("_entities", ...);
// Change "_entities" to whatever field name the scene actually uses
```

### **Problem 2: "_entities list is NULL!"**

**This means:** The field exists but is null

**Solution:**
- Entities haven't been added to the list yet
- Or they're in a different container
- Check if entities are actually being created in the scene

### **Problem 3: "(No enemies found in X entities)"**

**This means:** 
- Entities exist but none are `Enemy` type
- They might be a different type

**Solution:** Check `Scenes/IslandScene.cs` to see what entity types are being used

### **Problem 4: Bot still acting dumb despite detection showing enemies**

**Check the decision logic:**

```
  ➜ ACTION: Jump=True | Attack=True | Move=True | State=COMBAT_MELEE
```

If this shows but bot isn't jumping/attacking in game:
- Problem is KEY INJECTION not entity detection
- Check `BotPlayerController.InjectInput()`

---

## **IF DECISION LOGIC ISN'T WORKING**

### **Check what state the bot is in:**

```
State=EXPLORE          → Bot moving forward
State=COMBAT_RANGED    → Bot attacking distant enemy
State=COMBAT_MELEE     → Bot jumping + attacking
State=CRITICAL_HEALTH  → Bot seeking health
State=COLLECT_PICKUP   → Bot collecting items
```

### **Check the actions:**

```
Jump=True    → Space key should be injected
Attack=True  → Z key should be injected
Move=True    → Right + Shift should be injected
```

If actions show TRUE but keys aren't being pressed:
- Problem is in BotPlayerController.InjectInput()
- Or InputManager isn't working

---

## **HOW TO ENABLE/DISABLE LOGGING**

**To log EVERY FRAME:** (currently enabled)
```csharp
// In ObservableBotAI.LogFrame():
foreach (var line in _thisFrameLog)
{
    System.Diagnostics.Debug.WriteLine(line);  // Logs every frame
}
```

**To log ONLY when decision changes:** (if spam is too much)
```csharp
if (_frameCount % 60 == 0)  // Log once per second
{
    foreach (var line in _thisFrameLog)
    {
        System.Diagnostics.Debug.WriteLine(line);
    }
}
```

---

## **QUICK COPY/PASTE TEST**

Run this and paste the output from Debug window:

```
# Tell me what you see:
- Are enemies being detected? (Look for "✓ ENEMY")
- What state is the bot in? (Look for "State=")
- What actions is the bot deciding to take? (Look for "ACTION:")
- Any errors? (Look for "❌ ERROR" or "💥 EXCEPTION")
```

---

## **IF YOU STILL CAN'T FIGURE IT OUT**

Copy this from Debug Output:
1. The initialization message (the box with "BOT INITIALIZATION")
2. 5 frames of updates showing what's being detected
3. Any error messages

Paste it to me and I'll know exactly what's wrong!

---

**NOW GO TEST IT AND TELL ME WHAT THE LOGS SAY!** 🔍
