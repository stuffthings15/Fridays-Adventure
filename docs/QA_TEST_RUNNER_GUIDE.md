# QA Test Runner — Unattended Automation System

## Overview

The QA Test Runner is a fully unattended automation loop that:

1. **Builds** the solution
2. **Launches** the game with `--qa-bot` (auto-starts the QA bot walkthrough)
3. **Monitors** structured JSONL game logs for progress
4. **Detects failures** (bot stuck for 3+ minutes, crash, timeout)
5. **Kills** the game on failure
6. **Generates** a repair prompt with full diagnostic data
7. **Pastes** the prompt into Copilot Chat via Win32 keyboard simulation
8. **Waits** for Copilot to analyze and fix the code
9. **Rebuilds** and relaunches
10. **Repeats** until the bot completes ALL levels or max iterations reached

---

## Activation

**Only** start this system by saying exactly:

> **Start Q&A test now.**

in the Copilot Chat. Copilot will then execute the runner script.

---

## System Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    QATestRunner.ps1                           │
│   (PowerShell orchestrator — Win32 desktop automation)       │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────┐    ┌────────────┐    ┌───────────────────┐     │
│  │  BUILD   │───▶│  LAUNCH    │───▶│  MONITOR LOGS     │     │
│  │ (Ctrl+   │    │ (exe with  │    │ (poll JSONL file   │     │
│  │  Shift+B)│    │  --qa-bot) │    │  every 5 sec)      │     │
│  └──────────┘    └────────────┘    └─────────┬─────────┘     │
│       ▲                                      │               │
│       │                              ┌───────▼───────┐       │
│       │                              │ STUCK / CRASH  │       │
│       │                              │ DETECTED?      │       │
│       │                              └───┬───────┬───┘       │
│       │                         NO ──────┘       │ YES       │
│       │                    (keep monitoring)     │           │
│       │                                  ┌───────▼───────┐   │
│       │                                  │ KILL GAME     │   │
│       │                                  │ COLLECT LOGS  │   │
│       │                                  └───────┬───────┘   │
│       │                                  ┌───────▼───────┐   │
│       │                                  │ GENERATE      │   │
│       │                                  │ REPAIR PROMPT │   │
│       │                                  └───────┬───────┘   │
│       │                                  ┌───────▼───────┐   │
│       │                                  │ PASTE INTO    │   │
│       │                                  │ COPILOT CHAT  │   │
│       │                                  │ (Win32 input) │   │
│       │                                  └───────┬───────┘   │
│       │                                  ┌───────▼───────┐   │
│       │                                  │ WAIT FOR FIX  │   │
│       │                                  │ (2.5 minutes)  │   │
│       │                                  └───────┬───────┘   │
│       └──────────────────────────────────────────┘           │
│                                                              │
│  EXIT CONDITION: QA_COMPLETE event with verdict=ALL_PASSED   │
│  SAFETY LIMIT:   20 iterations max                           │
└──────────────────────────────────────────────────────────────┘
```

---

## Game Code Changes

### 1. `--qa-bot` Command-Line Flag (`Engine/Game.cs`)

```csharp
public static bool AutoQABot { get; set; }

// Checked during Game.Start():
string[] args = Environment.GetCommandLineArgs();
foreach (string a in args)
    if (a.Equals("--qa-bot", StringComparison.OrdinalIgnoreCase))
        AutoQABot = true;
```

### 2. Auto-Launch in `LoadingScene.cs`

When `AutoQABot` is true, `LoadingScene` skips `TitleScene` and goes
directly to `QABotWalkthroughScene` with auto-advance flags on dialogue
and toad house scenes.

### 3. Completion Logging (`QABotWalkthroughScene.cs`)

Logs a `QA_COMPLETE` event with `verdict` field when all levels finish:
- `ALL_PASSED` → every level beaten
- `HAS_FAILURES` → some levels failed

### 4. Auto-Exit on QA Complete

When running in `--qa-bot` mode, the game calls `Application.Exit()`
after the Summary phase displays for 3 seconds. This cleanly exits
the process so the PowerShell monitor detects it.

---

## Failure Detection

The system detects failure when:

| Condition | Detection Method |
|-----------|-----------------|
| Bot stuck for 3 minutes | Log file not updated for 180s |
| Game crash | Process exits unexpectedly |
| Max runtime exceeded | 15 minutes total game time |
| Build failure | Non-zero exit code |
| Levels failed | `QA_COMPLETE` with `verdict=HAS_FAILURES` |

---

## Visual Studio Automation

| Action | Method |
|--------|--------|
| Save all files | `Ctrl+Shift+S` via SendKeys |
| Build solution | `Ctrl+Shift+B` via SendKeys |
| Focus VS window | Win32 `SetForegroundWindow` |
| Open Copilot Chat | `Ctrl+/`, `Ctrl+C` via SendKeys |
| Paste prompt | `Ctrl+V` via SendKeys |
| Submit prompt | `Enter` via SendKeys |

---

## Log Format

Game logs are written to `bin/Debug/Logs/game_events_*.jsonl`:

```jsonl
[GAME] {"timestamp":"2025-01-15T03:22:14","event":"SystemEvent","level":"INFO","action":"BotLevelStart","detail":"Dinosaur Island"}
[GAME] {"timestamp":"2025-01-15T03:23:45","event":"LevelResult","level":"INFO","levelName":"Dinosaur Island","beaten":true,"timeSec":91.2}
[GAME] {"timestamp":"2025-01-15T03:28:00","event":"QA_COMPLETE","level":"INFO","totalLevels":17,"passed":17,"failed":0,"verdict":"ALL_PASSED"}
```

---

## Sample Generated Repair Prompt

```
Analyzed the QA test logs. The bot got stuck. Here is the failure report:

## QA FAILURE REPORT — Iteration 3

**Failure Type:** BOT_STUCK
**Last Active Level:** 5. Sky Island
**Time:** 2025-01-15 03:45:22

### Level Results So Far
  [PASS] 1. Dinosaur Island - 45.2s
  [PASS] 2. Storm Belt - 28.1s
  [PASS] 3. Underwater Caves - 62.5s
  [PASS] 4. Fortress Gate - 55.0s

### Recent Log Output (last 60 lines)
[bot movement, collision, position data...]

### What To Fix
The bot could not progress past "5. Sky Island". Analyze the log above...
```

---

## Validation Checklist

- ✔ Only starts when "Start Q&A test now." is used in Copilot Chat
- ✔ Physically presses buttons / sends input via Win32 SendKeys
- ✔ Runs unattended after activation
- ✔ Detects stuck bot conditions (3 min no-progress)
- ✔ Logs failures to `Tools/qa_failure_report.md`
- ✔ Submits repair prompts automatically to Copilot Chat
- ✔ Rebuilds and reruns automatically (Ctrl+Shift+B → relaunch)
- ✔ Stops only when full game completion is verified (`QA_COMPLETE` + `ALL_PASSED`)
- ✔ Safety limit: 20 iterations max
- ✔ Session log written to `Tools/qa_session_log.md`

---

## Files

| File | Purpose |
|------|---------|
| `Tools/QATestRunner.ps1` | Main automation orchestrator |
| `Tools/qa_session_log.md` | Running session log (auto-generated) |
| `Tools/qa_failure_report.md` | Last failure report (auto-generated) |
| `Engine/Game.cs` | `AutoQABot` flag + CLI arg parsing |
| `Scenes/LoadingScene.cs` | Auto-launch QA bot walkthrough |
| `Scenes/QABotWalkthroughScene.cs` | `QA_COMPLETE` logging + auto-exit |
