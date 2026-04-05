# 📋 Q&A TEST LOG SYSTEM GUIDE

## **COMPLETE LOGGING FOR Q&A TESTING**

All debug information from Q&A tests is now automatically saved to timestamped log files for review and analysis.

---

## **WHERE LOGS ARE SAVED**

### **Main Directory:**
```
Logs/TestSessions/[TIMESTAMP]/
```

Example:
```
Logs/TestSessions/2024-01-15_14-30-45/
```

---

## **WHAT GETS LOGGED**

### **1. SESSION LOG**
**File:** `SESSION_LOG.txt`

Contains:
- Session start time
- All levels tested in order
- Results for each level
- Summary statistics
- Session end time

Example:
```
═══════════════════════════════════════════════════════════════
FRIDAY'S ADVENTURE - AUTOMATED TEST SESSION LOG
═══════════════════════════════════════════════════════════════
Session ID: 2024-01-15_14-30-45
Started: 2024-01-15 14:30:45
───────────────────────────────────────────────────────────────

Starting automated test run of all 18 levels...

[1/18] Testing: 1. Dinosaur Island...
Status: ✅ BEATABLE
Time: 28.5s | Distance: 4200px | Items: 3 | Enemies: 2 | Completed: ✅
```

### **2. LEVEL-SPECIFIC LOGS**
**Files:** `LEVEL_[levelid].txt` (one per level)

Contains:
- Level name and ID
- Test start time
- Frame-by-frame data (every 10 frames)
  - Bot position
  - Bot state
  - Actions taken
  - Progress metrics
- Test result
  - Beatable/Not Beatable
  - Time to complete
  - Distance traveled
  - Items/Enemies
  - Failure reason (if any)

Example:
```
═══════════════════════════════════════════════════════════════
LEVEL TEST LOG: 1. Dinosaur Island
═══════════════════════════════════════════════════════════════
Level ID: dino
Test Started: 2024-01-15 14:30:50
───────────────────────────────────────────────────────────────

[FRAME 000] T=0.00s
  Bot Position: X=100 Y=300
  State: Running
  Actions: Jump=False | Attack=False | Move=True
  Progress: Distance=0px | Items=0 | Enemies=0

[FRAME 010] T=0.16s
  Bot Position: X=102 Y=300
  State: Running
  Actions: Jump=False | Attack=False | Move=True
  Progress: Distance=2px | Items=0 | Enemies=0
```

### **3. RESULTS CSV**
**File:** `TEST_RESULTS.csv`

Contains all results in spreadsheet format for easy analysis:

```
Level ID,Level Name,Beatable,Time (s),Distance (px),Items,Enemies,Failure Reason
dino,1. Dinosaur Island,YES,28.5,4200,3,2,N/A
storm1,2. Storm Belt,NO,60.0,3500,2,1,Timeout - Level took too long
sky,3. Sky Island,YES,45.2,5600,4,3,N/A
```

---

## **HOW TO RUN TESTS WITH LOGGING**

### **Step 1: Start Test**
```csharp
// In your code, call:
LevelAutoTestManager.RunAllTests();
```

### **Step 2: Wait for Completion**
- Tests run for all 18 levels
- Progress shows in console
- Logs are written in real-time

### **Step 3: Check Logs**
```
Look in: Logs/TestSessions/[TIMESTAMP]/
```

---

## **HOW TO ANALYZE THE LOGS**

### **Quick Review:**

1. **Open `SESSION_LOG.txt`** - Overview of all tests
2. **Check summary section** - How many passed/failed
3. **Look for failed levels** - See which ones need attention

### **Detailed Review:**

1. **For each failed level** - Open `LEVEL_[levelid].txt`
2. **Review frame-by-frame data**
3. **Check when bot got stuck or failed**
4. **Understand the progression**

### **Spreadsheet Analysis:**

1. **Open `TEST_RESULTS.csv` in Excel**
2. **Sort by "Beatable" column** - See failures
3. **Sort by "Time" column** - See which took longest
4. **Sort by "Failure Reason"** - Group similar failures

---

## **LOG FILE STRUCTURE**

```
Logs/
└── TestSessions/
    └── 2024-01-15_14-30-45/
        ├── SESSION_LOG.txt          ← Main session log
        ├── LEVEL_dino.txt           ← Island level details
        ├── LEVEL_storm1.txt         ← Storm Belt level details
        ├── LEVEL_sky.txt            ← Sky Island level details
        ├── ...more levels...
        └── TEST_RESULTS.csv         ← Spreadsheet data
```

---

## **WHAT INFORMATION IS AVAILABLE**

### **Per Test Session:**
- ✅ Start/end timestamps
- ✅ Total tests run
- ✅ Pass/fail count
- ✅ Success percentage

### **Per Level:**
- ✅ Frame-by-frame bot position
- ✅ Bot state each frame
- ✅ Actions taken (jump, attack, move)
- ✅ Progress metrics
- ✅ Time to complete
- ✅ Distance traveled
- ✅ Items collected
- ✅ Enemies defeated
- ✅ Failure reasons

### **Summary Data:**
- ✅ CSV for spreadsheet analysis
- ✅ Summary statistics
- ✅ Levels needing attention

---

## **MAKING CHANGES BASED ON LOGS**

### **Step 1: Run Tests**
```
LevelAutoTestManager.RunAllTests();
```

### **Step 2: Review Logs**
- Check `TEST_RESULTS.csv` for failures
- Review frame data for problem levels

### **Step 3: Identify Issues**
- Did bot get stuck?
- Did bot timeout?
- Did bot not move?
- What was the state?

### **Step 4: Make Code Changes**
- Update `AutoTestBot.cs` logic
- Adjust detection thresholds
- Fix decision making

### **Step 5: Run Tests Again**
- Re-run full test suite
- Compare with previous logs
- Verify improvements

---

## **LOG FILE SIZES**

- **SESSION_LOG.txt:** ~5-10 KB (all test results)
- **LEVEL_[id].txt:** ~1-2 KB each (frame data)
- **TEST_RESULTS.csv:** ~2-3 KB (all results)

**Total per session:** ~30-50 KB (very manageable)

---

## **ACCESSING OLD TEST LOGS**

All test sessions are timestamped and saved, so you can:

1. **Compare results over time**
   - Session 1: 15/18 passing
   - Session 2: 16/18 passing
   - Session 3: 18/18 passing ✅

2. **Trace when issues appeared**
   - Level X passed in Session 1
   - Level X failed in Session 2
   - What changed?

3. **Analyze trends**
   - Which levels are consistently failing?
   - Are performance times improving?
   - Are more items being collected?

---

## **CONSOLE OUTPUT EXAMPLE**

```
╔════════════════════════════════════════════════════════════╗
║                                                            ║
║     FRIDAYS ADVENTURE - AUTOMATED LEVEL BEATABILITY TEST   ║
║                                                            ║
║        AI Bot Testing All 18 Levels                        ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝

[1/18] Testing: 1. Dinosaur Island...
        Status: ✅ BEATABLE
        Time: 28.5s | Distance: 4200px | Items: 3 | Enemies: 2 | Completed: ✅

[2/18] Testing: 2. Storm Belt...
        Status: ❌ NOT BEATABLE
        Time: 60.0s | Distance: 3500px | Items: 2 | Enemies: 1 | Completed: ❌
        Issue: Timeout - Level took too long

...

📁 Test logs saved to: Logs/TestSessions/2024-01-15_14-30-45/
```

---

## **QUICK REFERENCE**

| Need | File | Location |
|------|------|----------|
| Overall summary | SESSION_LOG.txt | Top-level |
| Specific level details | LEVEL_[id].txt | Level directory |
| Spreadsheet analysis | TEST_RESULTS.csv | Top-level |
| Frame-by-frame data | LEVEL_[id].txt | Detailed section |
| Time comparison | TEST_RESULTS.csv | "Time" column |
| Failure reasons | TEST_RESULTS.csv | "Failure Reason" column |

---

**All your test data is now permanently saved for QA review and analysis!** 📊
