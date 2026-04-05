# 🚀 Q&A TEST LOGGING - QUICK START

## **In 30 Seconds**

1. **Run tests:** `LevelAutoTestManager.RunAllTests();`
2. **Wait for completion**
3. **Open logs:** `Logs/TestSessions/[TIMESTAMP]/`
4. **Review results:** `SESSION_LOG.txt` or `TEST_RESULTS.csv`

---

## **WHAT YOU GET**

✅ Timestamped test session folder  
✅ Session log with all results  
✅ Per-level logs with frame-by-frame data  
✅ CSV spreadsheet for analysis  
✅ All debug info saved permanently  

---

## **LOG FILES**

| File | Purpose |
|------|---------|
| `SESSION_LOG.txt` | Overview of entire test run |
| `LEVEL_[id].txt` | Detailed data for each level |
| `TEST_RESULTS.csv` | Spreadsheet format (open in Excel) |

---

## **EXAMPLE LOG LOCATION**

```
Logs/TestSessions/2024-01-15_14-30-45/
├── SESSION_LOG.txt
├── LEVEL_dino.txt
├── LEVEL_storm1.txt
└── TEST_RESULTS.csv
```

---

## **HOW TO USE THE LOGS**

### **Quick Check:**
```
1. Open SESSION_LOG.txt
2. Scroll to "TEST SESSION SUMMARY"
3. See which levels failed
```

### **Detailed Analysis:**
```
1. Open TEST_RESULTS.csv in Excel
2. Sort by "Beatable" to see failures
3. Sort by "Failure Reason" to group issues
```

### **Debug Specific Level:**
```
1. Open LEVEL_[id].txt
2. Review frame data
3. See where bot got stuck
```

---

## **MAKING IMPROVEMENTS**

1. **Run tests** → Get logs
2. **Analyze logs** → Find issues
3. **Update `AutoTestBot.cs`** → Fix logic
4. **Run tests again** → Compare results
5. **Compare old vs new logs** → See improvements

---

## **FULL DOCUMENTATION**

See `docs/QA_TEST_LOGGING_SYSTEM_GUIDE.md` for complete details

---

**Your test data is now saved and ready for review!** 📊
