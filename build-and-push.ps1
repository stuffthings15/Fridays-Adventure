#!/usr/bin/env pwsh
# ════════════════════════════════════════════════════════════════════════════
# Fridays Adventure - Automated Build & Push System
# Purpose: Build executable and push to Git with comprehensive logging
# Usage: .\build-and-push.ps1
# ════════════════════════════════════════════════════════════════════════════

param(
    [string]$CommitMessage = "Auto-build: Latest features and fixes",
    [bool]$PushToGit = $true,
    [bool]$CreateRelease = $false
)

# ────────────────────────────────────────────────────────────────────────────
# Configuration
# ────────────────────────────────────────────────────────────────────────────

$ProjectRoot = $PSScriptRoot
$SolutionFile = Join-Path $ProjectRoot "Fridays Adventure II.sln"
$OutputExe = Join-Path $ProjectRoot "bin\Release\Fridays_Adventure.exe"
$BuildLog = Join-Path $ProjectRoot "build-log-$(Get-Date -Format 'yyyy-MM-dd-HHmmss').txt"

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Fridays Adventure - Automated Build & Push System" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# ────────────────────────────────────────────────────────────────────────────
# Step 1: Verify Solution File
# ────────────────────────────────────────────────────────────────────────────

Write-Host "[1/5] Verifying project structure..." -ForegroundColor Yellow
if (-not (Test-Path $SolutionFile)) {
    Write-Host "ERROR: Solution file not found: $SolutionFile" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Solution found: $SolutionFile" -ForegroundColor Green

# ────────────────────────────────────────────────────────────────────────────
# Step 2: Clean Previous Build
# ────────────────────────────────────────────────────────────────────────────

Write-Host "[2/5] Cleaning previous build..." -ForegroundColor Yellow
$BinPath = Join-Path $ProjectRoot "bin"
$ObjPath = Join-Path $ProjectRoot "obj"

if (Test-Path $BinPath) {
    Remove-Item $BinPath -Recurse -Force
    Write-Host "✓ Cleaned bin directory" -ForegroundColor Green
}

if (Test-Path $ObjPath) {
    Remove-Item $ObjPath -Recurse -Force
    Write-Host "✓ Cleaned obj directory" -ForegroundColor Green
}

# ────────────────────────────────────────────────────────────────────────────
# Step 3: Build Solution
# ────────────────────────────────────────────────────────────────────────────

Write-Host "[3/5] Building solution with MSBuild..." -ForegroundColor Yellow

$MSBuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $MSBuildPath)) {
    $MSBuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
}

if (-not (Test-Path $MSBuildPath)) {
    Write-Host "ERROR: MSBuild not found" -ForegroundColor Red
    exit 1
}

$BuildCommand = @(
    $MSBuildPath,
    $SolutionFile,
    "/p:Configuration=Release",
    "/p:Platform=AnyCPU",
    "/verbosity:minimal",
    "/nologo"
)

& $BuildCommand 2>&1 | Tee-Object -FilePath $BuildLog

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed! See $BuildLog" -ForegroundColor Red
    Write-Host "Build log saved to: $BuildLog" -ForegroundColor Yellow
    exit 1
}

Write-Host "✓ Build completed successfully" -ForegroundColor Green

# ────────────────────────────────────────────────────────────────────────────
# Step 4: Verify Executable
# ────────────────────────────────────────────────────────────────────────────

Write-Host "[4/5] Verifying executable..." -ForegroundColor Yellow

if (-not (Test-Path $OutputExe)) {
    Write-Host "ERROR: Executable not found: $OutputExe" -ForegroundColor Red
    exit 1
}

$ExeSize = (Get-Item $OutputExe).Length / 1MB
Write-Host "✓ Executable created: $OutputExe ($ExeSize MB)" -ForegroundColor Green

# ────────────────────────────────────────────────────────────────────────────
# Step 5: Git Commit & Push
# ────────────────────────────────────────────────────────────────────────────

if ($PushToGit) {
    Write-Host "[5/5] Pushing to Git repository..." -ForegroundColor Yellow
    
    Push-Location $ProjectRoot
    
    try {
        # Stage all changes
        & git add -A 2>&1 | Out-Null
        
        # Get current timestamp
        $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $FullMessage = "$CommitMessage - Build $Timestamp"
        
        # Commit
        & git commit -m $FullMessage 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Git commit created" -ForegroundColor Green
            
            # Push
            & git push origin master 2>&1 | Out-Null
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Pushed to origin/master" -ForegroundColor Green
            } else {
                Write-Host "⚠ Git push had issues (may already be up to date)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "⚠ No changes to commit" -ForegroundColor Yellow
        }
        
        # Show commit log
        Write-Host "" -ForegroundColor Gray
        Write-Host "Recent commits:" -ForegroundColor Gray
        & git log --oneline -5
        
    } finally {
        Pop-Location
    }
}

# ────────────────────────────────────────────────────────────────────────────
# Summary
# ────────────────────────────────────────────────────────────────────────────

Write-Host "" -ForegroundColor Gray
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "✓ BUILD & PUSH COMPLETE" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Executable: $OutputExe" -ForegroundColor Cyan
Write-Host "Build Log: $BuildLog" -ForegroundColor Cyan
Write-Host "Size: $ExeSize MB" -ForegroundColor Cyan
Write-Host ""
Write-Host "Status: ✓ READY TO RUN" -ForegroundColor Green
Write-Host ""

exit 0
