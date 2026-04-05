# 🎮 QUICK START: Automated Development with Vibe Coding Tools

## **What's New?**

✅ Bot auto-skips all dialogue  
✅ Supports all major controllers (Xbox, PS4, Switch, Generic)  
✅ Automatic build & push to Git  
✅ Vibe Coding Tools integration  

---

## **Setup (One-time)**

### **Step 1: Install Vibe Coding Tools**
- Download from: [Vibe Coding Tools](https://vibecoding.com)
- Install to your development environment
- Open Fridays Adventure project with Vibe enabled

### **Step 2: Review Configuration**
- Open `vibe.config.json` in project root
- Customize automation settings:
  ```json
  {
    "autoBuildonSave": true,
    "autoPushToGit": true,
    "buildOnCompletion": true
  }
  ```

### **Step 3: Manual Test**
```powershell
# Test build script
cd "C:\Users\stuff\Desktop\Classes\CS-120\PROJECTS\CS-120\Weeks\Week 10\Fridays Adventure II"
.\build-and-push.ps1 -PushToGit $false

# Check output
ls bin\Release\Fridays_Adventure.exe
```

---

## **Daily Workflow**

### **With Automation ENABLED:**
1. ✏️ **Make code changes**
2. 💾 **Save file** (`Ctrl+S`)
3. 🔨 **Automatic build starts** (wait ~30-60 seconds)
4. ✅ **Build completes**
5. 📤 **Auto-commit to Git** (if changes detected)
6. 🚀 **Auto-push to master** (if configured)
7. 📋 **Build log saved**

→ **No manual steps needed!**

### **Without Automation (Manual):**
```powershell
# Build and push manually
.\build-and-push.ps1 -CommitMessage "Feature: Your description"

# Or just build
.\build-and-push.ps1 -PushToGit $false
```

---

## **Configuration Options**

### **Enable Auto-Build on Save:**
```json
"autoBuildonSave": true,
"buildIntervalSeconds": 300    # Wait 5 min between builds
```

### **Enable Auto-Push to Git:**
```json
"autoPushToGit": true,
"commitTemplate": "Auto-build: Latest features and fixes - {timestamp}",
"pushAfterCommit": true,
"createTag": true
```

### **Custom Commit Messages:**
```powershell
.\build-and-push.ps1 -CommitMessage "Feature: Added dialogue skipping"
```

---

## **Game Features Available**

### **Bot Features**
- ✅ Auto-skip dialogue boxes
- ✅ Auto-progress narrative
- ✅ Handle all prompts automatically
- ✅ Play complete levels without intervention

### **Controller Support**
| Controller | Status | Notes |
|-----------|--------|-------|
| Keyboard | ✅ Full | WASD, Space, Z, X |
| Xbox One/Series | ✅ Full | 4 controllers, vibration |
| PlayStation 4/5 | ✅ Full | XInput compatible |
| Nintendo Switch | ✅ Full | Pro Controller + Joy-Cons |
| Generic HID | ✅ Full | Any USB gamepad |
| Touch Screen | ✅ Full | Virtual buttons |

### **Game Features**
- ✅ 18 levels fully playable
- ✅ All enemies & bosses
- ✅ All abilities & power-ups
- ✅ CardRoulette auto-play
- ✅ Difficulty settings
- ✅ Settings menu
- ✅ Inventory system

---

## **Troubleshooting**

### **Build fails?**
```powershell
# Check build log
Get-ChildItem build-log-*.txt | Select -Last 1 | Get-Content
```

### **Build succeeds but exe not found?**
```powershell
# Verify build output
ls bin\Release\
```

### **Git push fails?**
```powershell
# Check Git status
git status
git log --oneline -3

# Retry push manually
git push origin master
```

### **Vibe not detecting changes?**
- Restart Vibe Coding Tools
- Ensure file is saved (`Ctrl+S`)
- Check `vibe.config.json` for enabled status

---

## **Build Output**

Each build creates:
- ✅ `Fridays_Adventure.exe` - The game executable
- ✅ `build-log-YYYY-MM-DD-HHMMSS.txt` - Build log
- ✅ Git commit - Auto-committed with timestamp
- ✅ Git tag - Release tag (if enabled)
- ✅ Release notes - Generated summary

---

## **Advanced: Custom Build Script**

To customize the build process, edit `build-and-push.ps1`:

```powershell
# Change output directory
$OutputExe = "C:\MyGames\Fridays_Adventure.exe"

# Change build configuration
$BuildCommand += "/p:Configuration=Debug"

# Add custom pre-build step
& "./scripts/pre-build.ps1"
```

---

## **Key Commands**

```powershell
# Full build & push
.\build-and-push.ps1

# Build only (no push)
.\build-and-push.ps1 -PushToGit $false

# Custom commit message
.\build-and-push.ps1 -CommitMessage "Feature: Added new level"

# Check recent commits
git log --oneline -10

# View automation config
cat vibe.config.json | ConvertFrom-Json | Format-List
```

---

## **Monitoring Builds**

### **Check Build Status:**
```powershell
Get-ChildItem build-log-*.txt | Sort -Descending | Select -First 1
```

### **Monitor Real-Time:**
```powershell
Get-Content build-log-*.txt -Tail 20 -Wait
```

### **Review Git History:**
```powershell
git log --oneline -20 --grep="Auto-build"
```

---

## **Next Steps**

1. ✅ Configure `vibe.config.json`
2. ✅ Test manual build: `.\build-and-push.ps1`
3. ✅ Enable automation in Vibe
4. ✅ Start developing → automatic build & push!
5. ✅ Monitor build logs in `build-logs/` directory

---

## **Support**

- Build issues? Check `build-log-*.txt`
- Git issues? Run `git status`
- Vibe issues? Check `vibe.config.json` formatting
- Controller issues? Plug in controller, restart game

---

**Happy coding! 🚀**
