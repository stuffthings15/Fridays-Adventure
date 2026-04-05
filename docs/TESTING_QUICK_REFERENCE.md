# Quick Reference - Session 67 Enhanced Testing System

## 🚀 Quick Start

### Run Tests
```
Game → Dev Menu (F2) → [QA] AUTO-TEST: Bot Level Tester → [ENTER]
```

### Check Results
1. **Console** - Shows pass/fail for each level
2. **Logs Directory** - `Logs/bot-tests/`
3. **Summary** - `TEST_ANALYSIS_SUMMARY.txt`

## 📊 What You Get

### For Each Level Tested:
✅ **Result** - Beatable or not beatable  
✅ **Metrics** - Time, items, enemies  
✅ **Timeline** - Complete action log  
✅ **Analysis** - Distance, patterns  
✅ **Fixes** - Specific recommendations  

## 🔍 Reading Logs

### Log File Names
`{levelId}_{levelName}.log`

Examples:
- `dino_1.DinosaurIsland.log`
- `storm1_2.StormBelt.log`
- `wano_5.BladeNation.log`

### What's Inside
```
TEST RESULT:
  Status: ✅/❌
  Time: XX seconds
  Items: X
  Enemies: X
  Reason: (if failed)

DETAILED ACTION LOG:
  [0.02s] INIT: Starting...
  [2.30s] MOVEMENT: Bot at X:250
  [4.10s] ITEM: Item collected
  ...
  [60.0s] TIMEOUT: Timeout reached

ANALYSIS:
  Total Events: XX
  Distance Traveled: XXpx
  Action Types: INIT, MOVEMENT, etc.

RECOMMENDATIONS:
  • Suggestion 1
  • Suggestion 2
  • ...
```

## 🐛 Analyzing Issues

| Issue | Log Shows | Fix |
|-------|-----------|-----|
| **Too Hard** | Timeout at 60s, low progress | Reduce difficulty, add platforms |
| **Stuck** | Movement plateaus | Check for gaps, add platforms |
| **Can't Find Path** | Low distance traveled | Add shortcuts, mark paths |
| **Enemy Spam** | Quick failure | Space enemies wider |
| **Too Easy** | Very fast completion | Add challenge, more enemies |

## 📈 Workflow

```
Run Tests
    ↓
Review Console
    ↓
Check Failed Levels
    ↓
Open Failed Level Log
    ↓
Find Problem Point
    ↓
Use Recommendation
    ↓
Fix Level
    ↓
Run Tests Again
    ↓
Verify Pass
    ↓
Repeat for Next Level
```

## 📁 Important Files

| File | Contains |
|------|----------|
| `Logs/bot-tests/{id}.log` | Individual level details |
| `TEST_ANALYSIS_SUMMARY.txt` | Overview of all results |
| `Tests/AutoTestBot.cs` | Bot implementation |
| `Tests/TestLogSystem.cs` | Logging system |
| `docs/TEST_LOGGING_GUIDE.md` | Full guide |

## 💡 Tips

1. **First Run** - Run all 18 levels to see overall picture
2. **Prioritize** - Fix most-failed levels first
3. **Pattern Fix** - If 3+ levels have same issue, fix root cause
4. **Incremental** - Test, fix, test again
5. **Save Logs** - Keep old logs for before/after comparison
6. **Git Commit** - Commit after each fix batch

## 🎯 Success Criteria

All 18 levels:
- ✅ Beatable within 60 seconds
- ✅ Bot can traverse geometry
- ✅ Difficulty is appropriate
- ✅ No unpassable obstacles

## 🔗 Integration

Share logs with Copilot:
```
"Here's the log from Blade Nation. 
Bot reaches X:1200 then timeout. 
What's the issue?"
```

## 📞 Support

**Q: Logs not appearing?**
A: Run test once to create `Logs/bot-tests/` directory

**Q: Log file won't open?**
A: Use any text editor (Notepad, VS Code, Sublime)

**Q: Need help analyzing?**
A: Copy relevant section and ask Copilot

## ⚙️ Technical

### Logging Events
- `INIT` - Start
- `MOVEMENT` - Progress
- `JUMP` - Action
- `ITEM` - Collection
- `ENEMY` - Combat
- `VICTORY` - Win
- `TIMEOUT` - Fail (too long)
- `FAILED` - Fail (other)

### Format
```
[Time] EventType: Message
       @ (X, Y)
```

---

**Save this for quick reference while testing!**
