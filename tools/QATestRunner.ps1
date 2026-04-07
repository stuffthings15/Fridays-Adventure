# ════════════════════════════════════════════════════════════════════
# QA TEST RUNNER — Unattended Automated QA Loop
# ════════════════════════════════════════════════════════════════════
# ACTIVATION: Only runs when triggered by "Start Q&A test now." in
#             Copilot Chat.  The Copilot agent calls this script.
#
# PURPOSE:    Build → Launch game with --qa-bot → Monitor logs →
#             Detect stuck/crash → Kill game → Generate repair prompt →
#             Paste into Copilot Chat → Wait for fix → Rebuild → Repeat
#
# USAGE:      .\Tools\QATestRunner.ps1
#             (Launched automatically by Copilot — do NOT run manually)
# ════════════════════════════════════════════════════════════════════

param(
    [int]$MaxIterations      = 20,      # safety limit — max fix/retry cycles
    [int]$StuckTimeoutSec    = 180,     # 3 minutes with no progress = stuck
    [int]$MaxGameTimeSec     = 900,     # 15 min max per full run
    [int]$FixWaitSec         = 150,     # wait 2.5 min for Copilot to apply fixes
    [int]$PollIntervalSec    = 5        # how often to check logs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Paths ──────────────────────────────────────────────────────────
$ProjectDir  = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not (Test-Path "$ProjectDir\Fridays Adventure.sln")) {
    # Fallback: script is in Tools/ inside the project dir
    $ProjectDir = Split-Path -Parent $PSScriptRoot
}
if (-not (Test-Path "$ProjectDir\Fridays Adventure.sln")) {
    $ProjectDir = "C:\Users\stuff\Desktop\Classes\CS-120\PROJECTS\CS-120\Weeks\Week 10\Fridays Adventure II"
}
$SolutionPath = "$ProjectDir\Fridays Adventure.sln"
$ExePath      = "$ProjectDir\bin\Debug\Fridays Adventure.exe"
$LogDir       = "$ProjectDir\bin\Debug\Logs"
$SessionLog   = "$ProjectDir\Tools\qa_session_log.md"
$FailureFile  = "$ProjectDir\Tools\qa_failure_report.md"

# ── Win32 API imports ──────────────────────────────────────────────
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class QAWin32 {
    [DllImport("user32.dll")] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public const int SW_RESTORE  = 9;
    public const int SW_MINIMIZE = 6;
}
"@

# ── Helper: Find window by partial title ───────────────────────────
function Find-WindowByTitle {
    param([string]$TitleFragment)
    $found = $null
    $callback = [QAWin32+EnumWindowsProc]{
        param($hWnd, $lParam)
        $sb = New-Object System.Text.StringBuilder 256
        [QAWin32]::GetWindowText($hWnd, $sb, 256) | Out-Null
        $title = $sb.ToString()
        if ($title -and $title -like "*$TitleFragment*") {
            Set-Variable -Name "found" -Value $hWnd -Scope 2
            return $false  # stop enumerating
        }
        return $true
    }
    [QAWin32]::EnumWindows($callback, [IntPtr]::Zero) | Out-Null
    return $found
}

# ── Helper: Focus a window ─────────────────────────────────────────
function Focus-Window {
    param([IntPtr]$hWnd)
    if ($hWnd -eq [IntPtr]::Zero) { return $false }
    [QAWin32]::ShowWindow($hWnd, [QAWin32]::SW_RESTORE) | Out-Null
    Start-Sleep -Milliseconds 200
    [QAWin32]::SetForegroundWindow($hWnd) | Out-Null
    Start-Sleep -Milliseconds 300
    return $true
}

# ── Helper: Find VS window ────────────────────────────────────────
function Find-VisualStudio {
    # Look for VS window with our solution name
    $hw = Find-WindowByTitle "Fridays Adventure"
    if ($hw -and $hw -ne [IntPtr]::Zero) {
        $sb = New-Object System.Text.StringBuilder 512
        [QAWin32]::GetWindowText($hw, $sb, 512) | Out-Null
        $title = $sb.ToString()
        if ($title -like "*Visual Studio*" -or $title -like "*Microsoft Visual Studio*") {
            return $hw
        }
    }
    # Broader search
    $hw = Find-WindowByTitle "Visual Studio"
    return $hw
}

# ── Helper: Build the project ──────────────────────────────────────
function Build-Project {
    Write-Host "  [BUILD] Building solution..." -ForegroundColor Yellow
    $vsHwnd = Find-VisualStudio
    if ($vsHwnd -and $vsHwnd -ne [IntPtr]::Zero) {
        Focus-Window $vsHwnd | Out-Null
        Start-Sleep -Milliseconds 500
        # Ctrl+Shift+S to save all
        [System.Windows.Forms.SendKeys]::SendWait("^+s")
        Start-Sleep -Seconds 1
        # Ctrl+Shift+B to build
        [System.Windows.Forms.SendKeys]::SendWait("^+b")
        Start-Sleep -Seconds 8  # wait for build
        Write-Host "  [BUILD] Build command sent via VS" -ForegroundColor Green
        return $true
    }
    # Fallback: use MSBuild directly
    Write-Host "  [BUILD] VS not found — using dotnet build fallback" -ForegroundColor DarkYellow
    $msbuild = & where.exe msbuild.exe 2>$null | Select-Object -First 1
    if ($msbuild) {
        & $msbuild $SolutionPath /p:Configuration=Debug /v:q /nologo
        return ($LASTEXITCODE -eq 0)
    }
    Write-Host "  [BUILD] ERROR: No build tool found!" -ForegroundColor Red
    return $false
}

# ── Helper: Launch the game with --qa-bot ──────────────────────────
function Start-Game {
    Write-Host "  [GAME] Launching game with --qa-bot..." -ForegroundColor Cyan
    if (-not (Test-Path $ExePath)) {
        Write-Host "  [GAME] ERROR: Exe not found at $ExePath" -ForegroundColor Red
        return $null
    }
    # Clean old logs
    if (Test-Path $LogDir) {
        Get-ChildItem "$LogDir\game_events_*.jsonl" | Remove-Item -Force -ErrorAction SilentlyContinue
    }
    $proc = Start-Process -FilePath $ExePath -ArgumentList "--qa-bot" -PassThru
    Start-Sleep -Seconds 5  # let the game initialize
    Write-Host "  [GAME] PID=$($proc.Id)" -ForegroundColor Green
    return $proc
}

# ── Helper: Get the latest log file ───────────────────────────────
function Get-LatestLogFile {
    if (-not (Test-Path $LogDir)) { return $null }
    $files = Get-ChildItem "$LogDir\game_events_*.jsonl" -ErrorAction SilentlyContinue |
             Sort-Object LastWriteTime -Descending
    if ($files.Count -gt 0) { return $files[0].FullName }
    return $null
}

# ── Helper: Parse log file for events ──────────────────────────────
function Read-GameLogs {
    param([string]$LogPath)
    if (-not $LogPath -or -not (Test-Path $LogPath)) { return @() }
    $lines = Get-Content $LogPath -ErrorAction SilentlyContinue
    $events = @()
    foreach ($line in $lines) {
        # Each line starts with [GAME] followed by JSON
        if ($line -match '^\[GAME\]\s*(\{.+\})') {
            try {
                $json = $Matches[1] | ConvertFrom-Json
                $events += $json
            } catch { }
        }
    }
    return $events
}

# ── Helper: Check if QA completed successfully ─────────────────────
function Test-QAComplete {
    param([array]$Events)
    foreach ($e in $Events) {
        if ($e.event -eq "QA_COMPLETE") {
            return $e
        }
    }
    return $null
}

# ── Helper: Detect stuck condition ─────────────────────────────────
function Test-BotStuck {
    param(
        [string]$LogPath,
        [int]$TimeoutSec
    )
    if (-not $LogPath -or -not (Test-Path $LogPath)) { return $false }
    $lastWrite = (Get-Item $LogPath).LastWriteTime
    $elapsed = (Get-Date) - $lastWrite
    return ($elapsed.TotalSeconds -ge $TimeoutSec)
}

# ── Helper: Find last active level from logs ───────────────────────
function Get-LastActiveLevel {
    param([array]$Events)
    $lastLevel = "Unknown"
    foreach ($e in $Events) {
        if ($e.event -eq "SystemEvent" -and $e.action -eq "BotLevelStart") {
            $lastLevel = $e.detail
        }
        if ($e.event -eq "LevelResult") {
            $lastLevel = "$($e.levelName) ($(if($e.beaten){'PASSED'}else{'FAILED'}))"
        }
    }
    return $lastLevel
}

# ── Helper: Get recent bot log lines ──────────────────────────────
function Get-RecentBotLines {
    param([string]$LogPath, [int]$Count = 60)
    if (-not $LogPath -or -not (Test-Path $LogPath)) { return "" }
    $lines = Get-Content $LogPath -Tail $Count -ErrorAction SilentlyContinue
    return ($lines -join "`n")
}

# ── Helper: Generate the failure repair prompt ─────────────────────
function New-RepairPrompt {
    param(
        [string]$LastLevel,
        [string]$FailureType,
        [string]$RecentLogs,
        [array]$LevelResults,
        [int]$Iteration
    )

    $resultSummary = ""
    foreach ($r in $LevelResults) {
        if ($r.event -eq "LevelResult") {
            $icon = if ($r.beaten) { "PASS" } else { "FAIL" }
            $resultSummary += "  [$icon] $($r.levelName) - $($r.timeSec)s`n"
        }
    }

    $prompt = @"
Analyzed the QA test logs. The bot got stuck. Here is the failure report:

## QA FAILURE REPORT — Iteration $Iteration

**Failure Type:** $FailureType
**Last Active Level:** $LastLevel
**Time:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

### Level Results So Far
$resultSummary

### Recent Log Output (last 60 lines)
``````
$RecentLogs
``````

### What To Fix
The bot could not progress past "$LastLevel". Analyze the log output above to determine:
1. What the bot was trying to do when it got stuck
2. Why it failed (collision, missing detection, scene transition issue, etc.)
3. Fix the code so the bot can progress through this section

**Rules:**
- Must compile under .NET Framework 4.7.2
- Do NOT break existing gameplay
- Make minimal targeted fixes
- Build and verify after changes
"@

    return $prompt
}

# ── Helper: Write the failure report to disk ───────────────────────
function Write-FailureReport {
    param([string]$Prompt)
    $Prompt | Out-File -FilePath $FailureFile -Encoding utf8 -Force
    Write-Host "  [REPORT] Written to $FailureFile" -ForegroundColor Yellow
}

# ── Helper: Submit prompt to Copilot Chat via VS ──────────────────
function Submit-ToCopilot {
    param([string]$Prompt)

    Write-Host "  [COPILOT] Focusing Visual Studio..." -ForegroundColor Magenta
    $vsHwnd = Find-VisualStudio
    if (-not $vsHwnd -or $vsHwnd -eq [IntPtr]::Zero) {
        Write-Host "  [COPILOT] ERROR: Cannot find Visual Studio window!" -ForegroundColor Red
        return $false
    }

    Focus-Window $vsHwnd | Out-Null
    Start-Sleep -Seconds 1

    # Copy prompt to clipboard
    Set-Clipboard -Value $Prompt
    Start-Sleep -Milliseconds 300

    # Open Copilot Chat: Ctrl+/, Ctrl+C  (VS 2022+ shortcut)
    # First try the standard shortcut
    [System.Windows.Forms.SendKeys]::SendWait("^/")
    Start-Sleep -Milliseconds 200
    [System.Windows.Forms.SendKeys]::SendWait("^c")
    Start-Sleep -Seconds 2

    # Paste the prompt from clipboard
    [System.Windows.Forms.SendKeys]::SendWait("^v")
    Start-Sleep -Seconds 1

    # Submit with Enter
    [System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
    Start-Sleep -Milliseconds 500

    Write-Host "  [COPILOT] Prompt submitted!" -ForegroundColor Green
    return $true
}

# ── Helper: Stop the game process ──────────────────────────────────
function Stop-Game {
    param([System.Diagnostics.Process]$Proc)
    if ($Proc -and -not $Proc.HasExited) {
        Write-Host "  [GAME] Stopping game (PID=$($Proc.Id))..." -ForegroundColor Yellow
        try { $Proc.Kill() } catch { }
        Start-Sleep -Seconds 2
    }
}

# ── Helper: Append to session log ──────────────────────────────────
function Write-SessionLog {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $entry = "[$timestamp] $Message"
    Write-Host $entry
    Add-Content -Path $SessionLog -Value $entry -ErrorAction SilentlyContinue
}

# ════════════════════════════════════════════════════════════════════
# MAIN QA LOOP
# ════════════════════════════════════════════════════════════════════

Add-Type -AssemblyName System.Windows.Forms

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  QA TEST RUNNER — Unattended Automated Loop" -ForegroundColor Cyan
Write-Host "  Max iterations: $MaxIterations" -ForegroundColor DarkCyan
Write-Host "  Stuck timeout:  ${StuckTimeoutSec}s ($(${StuckTimeoutSec}/60) min)" -ForegroundColor DarkCyan
Write-Host "  Project:        $ProjectDir" -ForegroundColor DarkCyan
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Initialize session log
"# QA Test Runner Session Log`n# Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" |
    Out-File -FilePath $SessionLog -Encoding utf8 -Force

$qaPassedFull = $false

for ($iter = 1; $iter -le $MaxIterations; $iter++) {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════╗" -ForegroundColor Yellow
    Write-Host "║  QA ITERATION $iter / $MaxIterations                              ║" -ForegroundColor Yellow
    Write-Host "╚═══════════════════════════════════════════════════════╝" -ForegroundColor Yellow
    Write-SessionLog "=== ITERATION $iter START ==="

    # ── Step 1: Build ──────────────────────────────────────────────
    $buildOk = Build-Project
    if (-not $buildOk) {
        Write-SessionLog "BUILD FAILED — waiting for fix"
        $failPrompt = New-RepairPrompt -LastLevel "Build" -FailureType "BUILD_ERROR" `
            -RecentLogs "Build failed. Check build output for errors." `
            -LevelResults @() -Iteration $iter
        Write-FailureReport $failPrompt
        Submit-ToCopilot $failPrompt
        Write-Host "  [WAIT] Waiting ${FixWaitSec}s for Copilot to fix build..." -ForegroundColor DarkYellow
        Start-Sleep -Seconds $FixWaitSec
        continue
    }

    # ── Step 2: Launch game ────────────────────────────────────────
    $gameProc = Start-Game
    if (-not $gameProc) {
        Write-SessionLog "GAME LAUNCH FAILED"
        Start-Sleep -Seconds 5
        continue
    }

    # ── Step 3: Monitor game progress ──────────────────────────────
    Write-Host "  [MONITOR] Watching game logs..." -ForegroundColor Green
    $startTime = Get-Date
    $lastProgressTime = Get-Date
    $lastLogSize = 0
    $gameRunning = $true
    $failureType = $null

    while ($gameRunning) {
        Start-Sleep -Seconds $PollIntervalSec

        # Check if game process exited
        if ($gameProc.HasExited) {
            Write-Host "  [MONITOR] Game process exited (code=$($gameProc.ExitCode))" -ForegroundColor Yellow
            $gameRunning = $false
            break
        }

        # Check total runtime
        $runElapsed = ((Get-Date) - $startTime).TotalSeconds
        if ($runElapsed -ge $MaxGameTimeSec) {
            Write-Host "  [MONITOR] Max game time exceeded (${MaxGameTimeSec}s)" -ForegroundColor Red
            $failureType = "TIMEOUT_MAX_RUNTIME"
            $gameRunning = $false
            break
        }

        # Check log file for progress
        $logFile = Get-LatestLogFile
        if ($logFile) {
            $currentSize = (Get-Item $logFile).Length
            if ($currentSize -gt $lastLogSize) {
                $lastProgressTime = Get-Date
                $lastLogSize = $currentSize
            }

            # Check for QA_COMPLETE event
            $events = Read-GameLogs $logFile
            $complete = Test-QAComplete $events
            if ($complete) {
                Write-Host "  [MONITOR] QA_COMPLETE detected!" -ForegroundColor Green
                if ($complete.verdict -eq "ALL_PASSED") {
                    $qaPassedFull = $true
                }
                $gameRunning = $false
                break
            }
        }

        # Check stuck condition — no new log data for StuckTimeoutSec
        $stuckElapsed = ((Get-Date) - $lastProgressTime).TotalSeconds
        if ($stuckElapsed -ge $StuckTimeoutSec) {
            Write-Host "  [MONITOR] Bot appears STUCK (no progress for ${StuckTimeoutSec}s)" -ForegroundColor Red
            $failureType = "BOT_STUCK"
            $gameRunning = $false
            break
        }

        # Status update every 30s
        if ([int]$runElapsed % 30 -lt $PollIntervalSec) {
            $logFile2 = Get-LatestLogFile
            $ev2 = if ($logFile2) { Read-GameLogs $logFile2 } else { @() }
            $lvl = Get-LastActiveLevel $ev2
            Write-Host "  [STATUS] ${runElapsed}s elapsed | Level: $lvl | Log: $([int]$lastLogSize) bytes" -ForegroundColor DarkGray
        }
    }

    # ── Step 4: Evaluate result ────────────────────────────────────
    Start-Sleep -Seconds 2  # let final logs flush

    if ($qaPassedFull) {
        Write-Host ""
        Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║  QA PASSED — FULL GAME COMPLETION VERIFIED                   ║" -ForegroundColor Green
        Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
        Write-SessionLog "QA PASSED — FULL GAME COMPLETION VERIFIED"
        Stop-Game $gameProc
        break
    }

    # Check if all levels passed but some failed
    $logFile = Get-LatestLogFile
    $events = if ($logFile) { Read-GameLogs $logFile } else { @() }
    $complete = Test-QAComplete $events

    if ($complete -and $complete.verdict -eq "HAS_FAILURES") {
        # QA finished but some levels failed — need fixes
        $failureType = "LEVELS_FAILED"
        Write-SessionLog "QA complete but $($complete.failed) level(s) failed"
    }

    # ── Step 5: On failure — generate and submit repair prompt ─────
    if ($failureType) {
        Stop-Game $gameProc
        Write-SessionLog "FAILURE: $failureType"

        $levelResults = $events | Where-Object { $_.event -eq "LevelResult" }
        $lastLevel = Get-LastActiveLevel $events
        $recentLogs = Get-RecentBotLines $logFile 80

        $repairPrompt = New-RepairPrompt `
            -LastLevel $lastLevel `
            -FailureType $failureType `
            -RecentLogs $recentLogs `
            -LevelResults $levelResults `
            -Iteration $iter

        Write-FailureReport $repairPrompt

        # Submit to Copilot Chat
        $submitted = Submit-ToCopilot $repairPrompt
        if ($submitted) {
            Write-Host "  [WAIT] Waiting ${FixWaitSec}s for Copilot to analyze and fix..." -ForegroundColor DarkYellow
            Start-Sleep -Seconds $FixWaitSec
        } else {
            Write-Host "  [WAIT] Could not submit to Copilot. Report saved to $FailureFile" -ForegroundColor Red
            Write-Host "  [WAIT] Please manually paste the report. Waiting ${FixWaitSec}s..." -ForegroundColor Red
            Start-Sleep -Seconds $FixWaitSec
        }
    } elseif (-not $gameProc.HasExited) {
        # Game exited cleanly but no QA_COMPLETE — might be crash or user close
        Stop-Game $gameProc
        Write-SessionLog "Game exited without QA_COMPLETE — possible crash"
    } else {
        Write-SessionLog "Game exited normally — checking if complete"
        # Game exited on its own — might be the auto-exit after Summary phase
        $logFile = Get-LatestLogFile
        $events = if ($logFile) { Read-GameLogs $logFile } else { @() }
        $complete = Test-QAComplete $events
        if ($complete -and $complete.verdict -eq "ALL_PASSED") {
            $qaPassedFull = $true
            Write-Host ""
            Write-Host "╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Green
            Write-Host "║  QA PASSED — FULL GAME COMPLETION VERIFIED                   ║" -ForegroundColor Green
            Write-Host "╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Green
            Write-SessionLog "QA PASSED — FULL GAME COMPLETION VERIFIED"
            break
        }
    }
}

# ── Final summary ──────────────────────────────────────────────────
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($qaPassedFull) {
    Write-Host "  RESULT: QA PASSED after $iter iteration(s)" -ForegroundColor Green
} else {
    Write-Host "  RESULT: QA DID NOT PASS after $MaxIterations iterations" -ForegroundColor Red
    Write-Host "  Check $SessionLog for details" -ForegroundColor Yellow
}
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-SessionLog "=== SESSION COMPLETE === Result: $(if($qaPassedFull){'PASSED'}else{'NOT PASSED'})"
