# Copilot Instructions

## Project Guidelines
- The user wants code to be commented and values clear in-code documentation standards.

## Session Logging Requirement ✅ MANDATORY
**IMPORTANT:** After EVERY prompt/session, you MUST:
1. Update the session log at `docs/WEEK_10_LOG_TEMPLATE.md`
2. Add a new entry with:
   - Session date/time
   - Features implemented
   - Bugs fixed
   - Documentation updated
   - Build status
   - Next steps

This log serves as the **RUNNING REVIEW DOCUMENT** for all changes, ideas, and implementation status.

## Phase Status Reference
- **Phase 1:** ✅ 110/110 COMPLETE - All features working
- **Phase 2:** 🚀 IN PROGRESS - 110 new features ready for implementation
- **Phase 3:** 📋 SPECIFIED - 110 expansion features designed
- **Backgrounds:** ✅ Applied to all island levels

## Code Comment Standards
1. **File Headers:** Include Phase/Team attribution
   ```csharp
   // ────────────────────────────────────────────
   // PHASE 2 - Team X: [Team Name]
   // Feature: [Name]
   // Purpose: [Description]
   // ────────────────────────────────────────────
   ```

2. **XML Doc Comments:** Always include on public members
   ```csharp
   /// <summary>What this does</summary>
   /// <remarks>PHASE 2 - Team X: Feature Name</remarks>
   ```

3. **Inline Comments:** Explain complex logic
   ```csharp
   // This loads the background specific to this island
   ```

## Documentation Requirements
After each implementation:
- [ ] Code compiles (0 errors, 0 warnings)
- [ ] Feature works in-game
- [ ] Phase 1 features still working
- [ ] Code commented per standards
- [ ] Checklist updated in PHASE_X_PROGRESS_TRACKER.md
- [ ] Week 10 log updated
- [ ] Git commit with Phase/Team info

## Key Documents
- `docs/PHASE_2_START_HERE.md` - Start implementing Phase 2
- `docs/PHASE_2_FEATURES_WAVE_1.md` - All Phase 2 feature specs
- `docs/PHASE_3_FEATURES_MASTER.md` - All Phase 3 specs
- `Assets/The Forge/Week10 Log_.docx` - SESSION LOG (UPDATE AFTER EACH PROMPT)

## Starting Point for Next Session
1. Implement Settings Menu (Team 9, Phase 2, Feature 1)
2. Follow workflow in `docs/PHASE_2_START_HERE.md`
3. Update Week 10 log before committing
4. Continue with Phase 2 weekly timeline