# ────────────────────────────────────────────────────────────────
# PHASE 2 - Team 8: Systems Programmer
# Feature: Self-Healing Asset Pipeline — Automated Loop Orchestrator
# Purpose: Builds the game, launches it in --heal-assets mode,
#          reads the healing report, and re-launches until all
#          asset gaps are resolved or max cycles are reached.
#
# Usage:
#   .\Tools\AssetHealingLoop.ps1                    # default 5 cycles
#   .\Tools\AssetHealingLoop.ps1 -MaxCycles 10      # up to 10 cycles
#   .\Tools\AssetHealingLoop.ps1 -SkipBuild         # skip MSBuild step
# ────────────────────────────────────────────────────────────────

param(
    [int]$MaxCycles = 5,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# ── Locate project root ──────────────────────────────────────────
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$csproj = Join-Path $projectRoot "Fridays Adventure.csproj"
$exePath = Join-Path $projectRoot "bin\Debug\Fridays Adventure.exe"
$reportPath = Join-Path $projectRoot "bin\Debug\Logs\asset-healing\latest_healing_report.txt"

if (-not (Test-Path $csproj)) {
    Write-Error "Cannot find project file at: $csproj"
    exit 1
}

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  SELF-HEALING ASSET PIPELINE — Automated Loop" -ForegroundColor Cyan
Write-Host "  Max Cycles: $MaxCycles" -ForegroundColor Cyan
Write-Host "  Project: $csproj" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ── Locate MSBuild ───────────────────────────────────────────────
$msbuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
)
$msbuild = $null
foreach ($p in $msbuildPaths) {
    if (Test-Path $p) { $msbuild = $p; break }
}
if (-not $msbuild -and -not $SkipBuild) {
    Write-Error "MSBuild not found. Install Visual Studio or use -SkipBuild."
    exit 1
}

# ── Helper: Parse report for unresolved count ────────────────────
function Get-UnresolvedCount {
    if (-not (Test-Path $reportPath)) { return -1 }
    $content = Get-Content $reportPath -Raw
    # Look for the "Resolved: X / Y" line
    if ($content -match "Resolved:\s+(\d+)\s*/\s*(\d+)") {
        $resolved = [int]$Matches[1]
        $total    = [int]$Matches[2]
        return ($total - $resolved)
    }
    # Look for UNRESOLVED markers
    $unresolvedLines = ($content | Select-String "UNRESOLVED" -AllMatches).Matches.Count
    return $unresolvedLines
}

# ── Main Loop ────────────────────────────────────────────────────
$cycle = 0
$allResolved = $false

while ($cycle -lt $MaxCycles -and -not $allResolved) {
    $cycle++
    Write-Host ""
    Write-Host "── Cycle $cycle / $MaxCycles ──────────────────────────────────" -ForegroundColor Yellow
    
    # ── Step 1: Build ─────────────────────────────────────────────
    if (-not $SkipBuild) {
        Write-Host "[BUILD] Compiling project..." -ForegroundColor White
        $buildResult = & $msbuild $csproj /t:Build /p:Configuration=Debug /verbosity:minimal 2>&1
        $buildExitCode = $LASTEXITCODE
        
        if ($buildExitCode -ne 0) {
            Write-Host "[BUILD] ❌ Build failed!" -ForegroundColor Red
            $buildResult | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkRed }
            Write-Host ""
            Write-Host "Fix build errors and re-run." -ForegroundColor Red
            exit 1
        }
        Write-Host "[BUILD] ✅ Build succeeded." -ForegroundColor Green
    } else {
        Write-Host "[BUILD] Skipped (-SkipBuild)." -ForegroundColor DarkGray
    }
    
    # ── Step 2: Clear previous report ─────────────────────────────
    if (Test-Path $reportPath) { Remove-Item $reportPath -Force }
    
    # ── Step 3: Launch game in heal mode ──────────────────────────
    Write-Host "[LAUNCH] Starting game with --heal-assets..." -ForegroundColor White
    $gameProcess = Start-Process -FilePath $exePath -ArgumentList "--heal-assets" -PassThru
    
    # Wait for game to complete (timeout 90 seconds)
    $timeout = 90
    $waited = 0
    while (-not $gameProcess.HasExited -and $waited -lt $timeout) {
        Start-Sleep -Seconds 1
        $waited++
        if ($waited % 10 -eq 0) {
            Write-Host "  Waiting... ($waited s)" -ForegroundColor DarkGray
        }
    }
    
    if (-not $gameProcess.HasExited) {
        Write-Host "[LAUNCH] ⚠ Game didn't exit within $timeout s — killing process." -ForegroundColor Yellow
        $gameProcess | Stop-Process -Force
        Start-Sleep -Seconds 2
    } else {
        Write-Host "[LAUNCH] Game exited normally (${waited}s)." -ForegroundColor Green
    }
    
    # ── Step 4: Read healing report ───────────────────────────────
    Start-Sleep -Seconds 1  # brief pause for file flush
    
    if (-not (Test-Path $reportPath)) {
        Write-Host "[REPORT] ⚠ No report file found. Pipeline may not have run." -ForegroundColor Yellow
        continue
    }
    
    $unresolvedCount = Get-UnresolvedCount
    Write-Host "[REPORT] Healing report found." -ForegroundColor White
    
    # Show report summary
    $content = Get-Content $reportPath -Raw
    $summaryLines = $content -split "`n" | Select-Object -First 8
    foreach ($line in $summaryLines) {
        Write-Host "  $line" -ForegroundColor DarkCyan
    }
    
    if ($unresolvedCount -le 0) {
        Write-Host ""
        Write-Host "[RESULT] ✅ All asset gaps resolved!" -ForegroundColor Green
        $allResolved = $true
    } else {
        Write-Host ""
        Write-Host "[RESULT] ⚠ $unresolvedCount unresolved gap(s) remain." -ForegroundColor Yellow
        
        if ($cycle -lt $MaxCycles) {
            Write-Host "  Re-running cycle to resolve remaining gaps..." -ForegroundColor White
        }
    }
}

# ── Final Summary ────────────────────────────────────────────────
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ASSET HEALING LOOP COMPLETE" -ForegroundColor Cyan
Write-Host "  Cycles run: $cycle / $MaxCycles" -ForegroundColor Cyan
if ($allResolved) {
    Write-Host "  Status: ✅ ALL GAPS RESOLVED" -ForegroundColor Green
} else {
    Write-Host "  Status: ⚠ SOME GAPS REMAIN (check report)" -ForegroundColor Yellow
}
Write-Host "  Report: $reportPath" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
