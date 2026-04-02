# COMPREHENSIVE IMPLEMENTATION GUIDE - PHASES 2 & 3 + BACKGROUNDS

**Status:** Ready for Full Implementation  
**Backgrounds Found:** 5 in Assets/Sprites (bg_*.png)  
**Phase 2 Features:** 110 ready  
**Phase 3 Features:** 110 ready  

---

## PART 1: BACKGROUND ASSET MAPPING

### Backgrounds Located:
```
Assets/Sprites/
├── bg_dinoIsland.png      → Dinosaur Island scene
├── bg_skyisland.png       → Sky Island scene  
├── bg_bladenation.png     → Blade Nation (Wano) scene
├── bg_island.png          → Generic island (Harbor, Coral, Tundra, etc)
├── bg_overworld.png       → Overworld map (already used)
└── bg_title.png           → Title screen (already used)
```

### Implementation: Apply Backgrounds to Levels

**File to Modify:** `Scenes\IslandScene.cs`

**Current State:** Generic island backgrounds
**Target State:** Level-specific backgrounds

**Mapping:**
- `"dino"` island ID → `bg_dinoIsland.png`
- `"sky"` island ID → `bg_skyisland.png`
- `"wano"` island ID → `bg_bladenation.png`
- `"harbor"`, `"coral"`, `"tundra"`, underwater islands → `bg_island.png`

**Implementation:**
```csharp
// In IslandScene.cs, add to LoadBackgroundForIsland() method:
private Bitmap LoadBackgroundForIsland(string islandId)
{
    string bgFile = islandId switch
    {
        "dino" => "bg_dinoIsland.png",
        "sky" => "bg_skyisland.png",
        "wano" => "bg_bladenation.png",
        // All other islands use generic
        _ => "bg_island.png"
    };
    
    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
        "Assets", "Sprites", bgFile);
    
    return File.Exists(path) ? new Bitmap(path) : null;
}
```

---

## PART 2: PHASE 2 IMPLEMENTATION (110 FEATURES)

### Quick Start Priority Order

**Week 1 - Foundation (15 features)**
1. Team 2 (Producer) - Milestone Tracker
2. Team 3 (Tech Lead) - Hot-reload config
3. Team 9 (UI) - Settings Menu (TOP PRIORITY)
4. Team 10 (Engine) - Frame time histogram
5. Team 11 (Build) - Error log rotation

**Week 2 - Gameplay (25 features)**
6. Team 1 (Director) - Difficulty Modifiers
7. Team 4 (Designer) - Energy Meter System
8. Team 7 (Gameplay) - Wall Slide Mechanic
9. Team 8 (Systems) - Localization System
10. Team 6 (Narrative) - Branch Dialogue Trees

**Week 3 - Content (20 features)**
11. Team 5 (Level Designer) - Casino Level
12. Team 12 (Art) - Neon Aesthetic Mode
13. Team 13 (Character) - Cosmetic Skin Pack
14. Team 16 (Animator) - Falling Animation
15. Team 18 (Sound) - Footstep Variations

**Weeks 4+ - Polish & Remaining (50 features)**

### Implementation Template for Each Phase 2 Feature:

```csharp
/// <summary>
/// PHASE 2 - Team [X]: [Team Name]
/// Feature: [Feature Name]
/// Complexity: [Simple/Medium/Complex]
/// Dependencies: [List any]
/// Estimated Time: [X hours]
/// </summary>
public class [FeatureName]System
{
    // Implementation goes here
}
```

---

## PART 3: PHASE 3 IMPLEMENTATION (110 FEATURES)

### Critical Path for Phase 3

**Phase 3 Pillar 1: Cosmetics System (30 features)**
- Cosmetic shop UI (Team 9)
- Character skins (Team 7, 13)
- Cosmetic trails (Team 13)
- Cosmetic shop layout (Team 15)
- Cosmetic themes (Team 12)
- ... + 25 more

**Phase 3 Pillar 2: Community & Modding (30 features)**
- Modding framework (Team 3, 8)
- Community voting (Team 19)
- Bug bounty program (Team 19)
- Content creator tools (Team 2, 9)
- Leaderboards (Team 2, 9)
- ... + 25 more

**Phase 3 Pillar 3: New Content (30 features)**
- 5 new islands (Team 5)
- Mega bosses (Team 4, 7)
- New Game+ mode (Team 1)
- Endless mode (Team 1, 10)
- Challenge system (Team 1, 2)
- ... + 25 more

**Phase 3 Pillar 4: Quality Polish (20 features)**
- Advanced animations (Team 16)
- New VFX (Team 17)
- Sound enhancements (Team 18)
- Art polish (Team 12-15)
- QA improvements (Team 19)

---

## STEP-BY-STEP IMPLEMENTATION PROCESS

### For Each Feature:

1. **Read Specification**
   - Source: `PHASE_2_FEATURES_WAVE_1.md` or `PHASE_3_FEATURES_MASTER.md`
   - Understand requirements

2. **Create System Class**
   - Location: `Systems/` or `Entities/` or `Scenes/`
   - Template: Use format above
   - Add XML comments

3. **Wire into Game Loop**
   - Update() method - logic
   - Draw() method - rendering
   - Input handling - if needed

4. **Test in-Game**
   - Run game
   - Verify feature works
   - Check for errors

5. **Update Documentation**
   - Update checklist in roadmap
   - Add to verification
   - Note any issues

6. **Build Verification**
   - Run build
   - Zero errors/warnings
   - All Phase 1 features still work

7. **Commit to Git**
   - Meaningful commit message
   - Include Phase and feature name

---

## FILE ORGANIZATION PLAN

### New Directories to Create:

```
Systems/
├── Phase2/          (new)
│   ├── DifficultyModifiers.cs
│   ├── SettingsMenu.cs
│   ├── EnergyMeterSystem.cs
│   └── ... (Phase 2 systems)
└── Phase3/          (new)
    ├── CosmeticShop.cs
    ├── ModdingFramework.cs
    ├── LeaderboardSystem.cs
    └── ... (Phase 3 systems)

Scenes/
├── Phase2/          (new)
│   ├── CasinoLevel.cs
│   ├── DreamIsland.cs
│   └── ... (new scenes)
└── Phase3/          (new)
    ├── NeonCity.cs
    ├── HauntedMansion.cs
    └── ... (Phase 3 scenes)
```

---

## QUICK CHECKLIST: STARTING IMPLEMENTATION

### Before You Begin:
- [ ] All Phase 1 features verified working ✅
- [ ] Build is passing ✅
- [ ] All documentation open and reviewed
- [ ] Git is ready (branch is clean)
- [ ] Backgrounds mapped above

### Starting Point #1: Apply Backgrounds
1. Open `Scenes\IslandScene.cs`
2. Add `LoadBackgroundForIsland()` method (see above)
3. Call it in `OnEnter()` method
4. Test each island in-game
5. Commit: "Apply level-specific backgrounds"

### Starting Point #2: Phase 2 Feature #1 - Settings Menu
1. Read: `PHASE_2_FEATURES_WAVE_1.md` (Team 9, Feature 1)
2. Create: `Systems/Phase2/SettingsMenu.cs`
3. Create: `Scenes/SettingsScene.cs`
4. Wire input: TitleScene.cs → SettingsScene
5. Test in-game
6. Commit: "Phase 2 - Team 9: Settings Menu"

### Starting Point #3: Phase 3 Feature #1 - Cosmetic Skins
1. Read: `PHASE_3_FEATURES_MASTER.md` (Team 7, Feature 1)
2. Create: `Systems/Phase3/CosmeticSkinSystem.cs`
3. Integrate with `Player.cs`
4. Add to character select
5. Test in-game
6. Commit: "Phase 3 - Team 7: Character Skins"

---

## VERIFICATION AFTER EACH COMMIT

```
✓ Code compiles (0 errors, 0 warnings)
✓ Phase 1 features still work
✓ New feature functional in-game
✓ No breaking changes
✓ Documentation updated
✓ Git commit clean
```

---

## SUCCESS CRITERIA

**Phase 2 Complete:**
- [ ] 110 features implemented
- [ ] Build passing
- [ ] All features work in-game
- [ ] 100% documentation
- [ ] Git history clean

**Phase 3 Complete:**
- [ ] 110 features implemented
- [ ] Build passing
- [ ] All features work in-game
- [ ] 100% documentation
- [ ] Git history clean

**Total Project:**
- [ ] 330 features implemented
- [ ] All working
- [ ] Fully documented
- [ ] Ready for release

---

## REFERENCES

- Phase 2 specs: `docs/PHASE_2_FEATURES_WAVE_1.md` + `WAVE_2.md`
- Phase 3 specs: `docs/PHASE_3_FEATURES_MASTER.md`
- Code standards: `.github/copilot-instructions.md`
- Current architecture: `docs/AI_DOCS.md`

---

**Ready to Begin Implementation** ✅

Start with backgrounds, then Phase 2 Week 1, then proceed systematically.

