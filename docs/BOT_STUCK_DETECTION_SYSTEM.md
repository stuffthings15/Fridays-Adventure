# 🔴 BOT STUCK DETECTION SYSTEM

## **THE PROBLEM YOU REPORTED**

Bot got stuck for 30+ minutes and just kept running without progress being logged.

## **THE SOLUTION**

A comprehensive stuck detection system that:
1. ✅ Detects when bot stops moving (< 5px/sec for > 3 seconds)
2. ✅ Automatically logs stuck events with exact position, time, and reason
3. ✅ Continues monitoring even after stuck
4. ✅ Fails level if stuck for more than 13 seconds
5. ✅ Logs stuck diagnostics to the test log files

---

## **HOW IT WORKS**

### **Stuck Detection Logic:**

```
Every 0.1 seconds:
  1. Calculate distance moved since last check
  2. Calculate speed: distance / time
  3. If speed < 5 pixels/sec:
     → Start counting stuck time
     → After 3 seconds: Mark as STUCK
     → Log stuck event with diagnostics
     → After 13 seconds: FAIL level automatically
  4. If speed > 5 pixels/sec:
     → Reset stuck timer
     → If was stuck: Log "unstuck" message
```

---

## **STUCK EVENT LOG INFORMATION**

When bot gets stuck, the log captures:

```
╔═══════════════════════════════════════════════════════════════╗
║ BOT STUCK DETECTED!                                           ║
╚═══════════════════════════════════════════════════════════════╝
Time in Level: 15.2s
Position: X=450 Y=300
State: Running
Distance Traveled: 1200px
Items Collected: 1
Enemies Defeated: 0
Likely Reason: Unknown - not moving but no obvious reason
```

---

## **STUCK DETECTION THRESHOLDS**

| Threshold | Value | Meaning |
|-----------|-------|---------|
| No Progress | 5 px/sec | Bot moving slower than this is stuck |
| Stuck Time | 3 seconds | Time before marking stuck |
| Failure Time | 13 seconds | Time before auto-failing level |
| Check Interval | 0.1 sec | How often to check position |

---

## **AUTOMATIC FAILURE BEHAVIOR**

```
Timeline:
Time 0.0s  → Bot starts moving
Time 3.0s  → No progress detected, start counting
Time 6.0s  → Still stuck (3 sec stuck duration)
           → Log stuck event
Time 10.0s → Still stuck (7 sec stuck duration)  
           → Log warning every 10 sec
Time 13.0s → Still stuck (10 sec stuck duration)
           → AUTO-FAIL LEVEL
           → Return failure reason: "Bot stuck"
           → Do NOT wait for 60 second timeout
```

---

## **STUCK REASONS DETECTED**

The system analyzes WHY the bot is stuck:

```
"No initial progress - stuck at level start"
  → Bot hasn't moved since initialization

"Fell below level (Y > 600) - likely fell into pit"  
  → Y position indicates fell off level

"Position above top of level - physics error?"
  → Y < 0, something is wrong

"Bounced back or level wraps - unexpected position"
  → Large distance but X position regressed

"Bot reached failed state"
  → State is explicitly Failed

"Unknown - not moving but no obvious reason"
  → Stuck but no identifiable cause
```

---

## **LOG FILES WITH STUCK DATA**

When you run tests with stuck detection:

```
Logs/TestSessions/[TIMESTAMP]/
├── SESSION_LOG.txt           ← Overall summary
├── LEVEL_[id].txt            ← Includes stuck events
└── TEST_RESULTS.csv          ← Shows "Bot got stuck" as failure reason
```

### **Example log entry:**

```
[BOT] ✓ Found ObservableBotAI initialization successful

[UPDATE] Time=15.2s | Player X=450 Y=300 HP=100
  Searching entities...

╔═══════════════════════════════════════════════════════════════╗
║ BOT STUCK DETECTED!                                           ║
╚═══════════════════════════════════════════════════════════════╝
Time in Level: 15.2s
Position: X=450 Y=300
State: Running
Distance Traveled: 1200px
Items Collected: 1
Enemies Defeated: 0
Likely Reason: Unknown - not moving but no obvious reason

[BOT_STUCK] Still stuck for 5.1s at X=450 Y=300 State=Running
[BOT_STUCK] Still stuck for 10.2s at X=450 Y=300 State=Running

[BOT] Level failed - bot stuck for 13.5s
```

---

## **ADVANTAGES OVER WAITING 60 SECONDS**

Before:
- ❌ Bot stuck at 0s, test waits until 60s timeout
- ❌ 60 seconds of useless waiting per stuck level
- ❌ 18 levels × 60 sec = 18 minutes wasted

After:
- ✅ Bot stuck at 0s, detected at 3s, failed at 13s
- ✅ Only 13 seconds wasted per stuck level
- ✅ 18 levels × 13 sec = 3.9 minutes total
- ✅ Identifies stuck levels 4.5x faster!

---

## **DEBUGGING WITH STUCK DATA**

When a level fails due to being stuck:

1. **Check the log** for exact stuck position
2. **See the reason** why it got stuck
3. **Look at frame data** showing the last good position
4. **Identify pattern** (pit, obstacle, physics bug, etc.)
5. **Fix the issue** in level design or AI

---

## **USING THE API**

```csharp
// In your test code:
var bot = new AutoTestBot();
bot.Initialize(100f, 300f);

// During testing:
bot.Update(0.016f, logger);  // Logger gets stuck notifications

// Check results:
if (bot.DidBotGetStuck())
{
    Console.WriteLine(bot.GetStuckReport());
}

// Get stuck events:
foreach (var evt in bot._stuckDetector.AllStuckEvents)
{
    Console.WriteLine($"Stuck at ({evt.X}, {evt.Y}): {evt.Reason}");
}
```

---

## **WHAT'S LOGGED**

For each stuck event:
- ✅ Exact time in level
- ✅ X and Y position
- ✅ Current bot state
- ✅ Distance traveled so far
- ✅ Items collected
- ✅ Enemies defeated
- ✅ Reason why stuck
- ✅ Duration of stuck period

---

## **FILES CREATED/MODIFIED**

- ✅ `Tests/BotStuckDetector.cs` - NEW detection system
- ✅ `Tests/AutoTestBot.cs` - Integrated stuck detection

---

## **BUILD STATUS**

✅ Compilation: 0 errors  
✅ Stuck detection: Active  
✅ Auto-failure: Enabled  
✅ Logging: Integrated  

---

## **KEY METRICS**

| Metric | Value |
|--------|-------|
| Min speed to be unstuck | 5 px/sec |
| Time before marking stuck | 3 seconds |
| Time before auto-failing | 13 seconds |
| Check frequency | Every 0.1 sec |
| Time saved per stuck level | 47 seconds |

---

**The bot will never silently hang again - stuck levels are detected and logged in detail!** 🔴➜✅
