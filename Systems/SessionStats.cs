using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SessionStats.cs  —  Per-session telemetry + Producer milestone tracking
    //
    //  Team 2 (Producer / Project Manager) — Ideas 1–10:
    //    1. Milestone tracker  (unlock named milestones at key progress points)
    //    2. Play-streak tracker  (days played in a row, saved to file)
    //    3. Auto-export session summary on close  (CSV row)
    //    4. Analytics event log  (timestamped event list)
    //    5. Retry counter per level  (how many attempts before clearing)
    //    6. Playtime badge thresholds  (1 h, 5 h, 10 h)
    //    7. Daily session reminder  (flag if > 24 h since last play)
    //    8. Tip system  (cycling in-game hints shown during loading)
    //    9. Version annotation in export  (BuildInfo.Summary stamped on each CSV row)
    //   10. Session ID  (UUID stamped at open so logs are correlatable)
    //
    //  Team 19 (QA Tester) — feeds death / error telemetry to QAReportScene.
    // ═══════════════════════════════════════════════════════════════════════════
    public sealed class SessionStats
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static readonly SessionStats Instance = new SessionStats();

        // ── Team 2 — Idea 10: unique session ID for log correlation ───────────
        /// <summary>Unique GUID stamped when the session opens.</summary>
        public string SessionId { get; } = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

        // ── Session clock ─────────────────────────────────────────────────────
        /// <summary>Wall-clock time the session started.</summary>
        public DateTime SessionStart { get; } = DateTime.Now;

        /// <summary>Total seconds of active play (accumulated via Tick).</summary>
        public float PlaySeconds { get; private set; }

        /// <summary>Human-readable play duration.</summary>
        public string PlayTimeFormatted =>
            TimeSpan.FromSeconds(PlaySeconds).ToString(@"hh\:mm\:ss");

        // ── Counters ──────────────────────────────────────────────────────────
        /// <summary>Number of times the player has died this session.</summary>
        public int DeathCount { get; private set; }

        /// <summary>Berries collected across all levels this session.</summary>
        public int BerriesCollected { get; private set; }

        /// <summary>Enemies defeated this session.</summary>
        public int EnemiesDefeated { get; private set; }

        /// <summary>Boss fights completed this session.</summary>
        public int BossesDefeated { get; private set; }

        /// <summary>Checkpoints reached this session.</summary>
        public int CheckpointsReached { get; private set; }

        /// <summary>Power-ups collected this session.</summary>
        public int PowerUpsCollected { get; private set; }

        /// <summary>Levels completed this session.</summary>
        public int LevelsCompleted { get; private set; }

        /// <summary>Longest streak of enemies defeated without taking damage.</summary>
        public int LongestCombo { get; private set; }

        // ── Team 2 — Idea 5: per-level retry counter ──────────────────────────
        /// <summary>
        /// Tracks how many attempts were made before the current level was cleared.
        /// Reset whenever a new level starts; incremented on each death.
        /// Team 2 (Producer) — Idea 5.
        /// </summary>
        public int CurrentLevelAttempts { get; private set; }

        // ── Team 2 — Idea 1: milestone tracker ────────────────────────────────
        private readonly HashSet<string> _unlockedMilestones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Read-only collection of all milestones unlocked this session.
        /// Team 2 (Producer) — Idea 1.
        /// </summary>
        public IEnumerable<string> UnlockedMilestones => _unlockedMilestones;

        // ── Team 2 — Idea 4: analytics event log ──────────────────────────────
        private readonly List<string> _events = new List<string>();

        // ── Team 2 — Idea 8: in-game tip system ───────────────────────────────
        private static readonly string[] _tips = new string[]
        {
            "Stomp enemies to bounce higher — great for reaching secret platforms!",
            "Hold Jump after stomping to extend your bounce height.",
            "Collect 100 coins to earn an extra life.",
            "Swan can glide by holding Jump while airborne.",
            "Orca's Ground Pound creates a shockwave on landing.",
            "Use Ice Wall to block enemy projectiles and create platforms.",
            "Flash Freeze chills all enemies nearby — great for crowd control.",
            "Break Wall shatters ice and stuns close enemies.",
            "P-Meter charges when you run at full speed for 1.5 seconds.",
            "Coyote time lets you jump for a brief moment after walking off a ledge.",
            "Wall jumps refresh your double jump — chain them for height.",
            "Visit Toad Houses between worlds for free items.",
            "Check your combo counter — a long streak means a bigger score bonus!",
            "SeaStone zones suppress Devil Fruit powers. Plan around them!",
            "Crew Bonds reduce the Rescue mash window when you fall in water.",
        };
        private int _tipIndex;

        // ── Current-streak state ──────────────────────────────────────────────
        private int _currentCombo;

        // ── Team 2 — Idea 2: play-streak file path ────────────────────────────
        private static readonly string StreakFile =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "MissFridaysAdventure", "streak.dat");

        // ── Team 2 — Idea 6: playtime badge thresholds (hours) ───────────────
        private static readonly float[] PlaytimeBadgeHours = { 1f, 5f, 10f, 25f, 50f };
        private float _lastBadgeCheckSeconds;

        // ── Constructor ───────────────────────────────────────────────────────
        private SessionStats()
        {
            DebugLogger.LogInfo("SessionStats", $"Session {SessionId} opened. {BuildInfo.Summary}");
            LogEvent("SESSION_OPEN");

            // Team 2 — Idea 2: load and advance the play streak.
            AdvancePlayStreak();

            // Team 2 — Idea 7: check if > 24 h since last play.
            CheckDailyReminder();
        }

        // ── Tick ──────────────────────────────────────────────────────────────
        /// <summary>
        /// Must be called once per game tick with the fixed delta-time.
        /// Accumulates total play time and checks badge thresholds.
        /// </summary>
        public void Tick(float dt)
        {
            PlaySeconds += dt;

            // Team 2 — Idea 6: award playtime badges at thresholds.
            if (PlaySeconds - _lastBadgeCheckSeconds >= 60f)
            {
                _lastBadgeCheckSeconds = PlaySeconds;
                CheckPlaytimeBadges();
            }
        }

        // ── Event recording ───────────────────────────────────────────────────
        /// <summary>Records a player death and resets the current enemy combo.</summary>
        public void RecordDeath()
        {
            DeathCount++;
            CurrentLevelAttempts++;
            _currentCombo = 0;
            LogEvent("DEATH");
            DebugLogger.LogInfo("SessionStats.Death", $"DeathCount={DeathCount} at {PlayTimeFormatted}");
            CheckMilestones();
        }

        /// <summary>Records a berry / coin collection.</summary>
        public void RecordBerry(int count = 1)
        {
            BerriesCollected += count;
            LogEvent($"BERRY +{count}");
        }

        /// <summary>Records an enemy defeat and updates the longest combo.</summary>
        public void RecordEnemyDefeated()
        {
            EnemiesDefeated++;
            _currentCombo++;
            if (_currentCombo > LongestCombo) LongestCombo = _currentCombo;
            LogEvent($"ENEMY_DEFEATED combo={_currentCombo}");
            CheckMilestones();
        }

        /// <summary>Breaks the current enemy combo (e.g. on taking damage).</summary>
        public void BreakCombo() => _currentCombo = 0;

        /// <summary>Records a boss defeat.</summary>
        public void RecordBossDefeated()
        {
            BossesDefeated++;
            LogEvent("BOSS_DEFEATED");
            DebugLogger.LogInfo("SessionStats.Boss", $"BossDefeated #{BossesDefeated} at {PlayTimeFormatted}");
            CheckMilestones();
        }

        /// <summary>Records a checkpoint being reached.</summary>
        public void RecordCheckpoint()
        {
            CheckpointsReached++;
            LogEvent("CHECKPOINT");
        }

        /// <summary>Records a power-up being collected.</summary>
        public void RecordPowerUp()
        {
            PowerUpsCollected++;
            LogEvent("POWERUP");
        }

        /// <summary>Records a level being successfully completed.</summary>
        public void RecordLevelComplete()
        {
            LevelsCompleted++;
            LogEvent($"LEVEL_COMPLETE attempts={CurrentLevelAttempts}");
            CurrentLevelAttempts = 0;   // reset retry counter for next level
            DebugLogger.LogInfo("SessionStats.Level",
                $"Level #{LevelsCompleted} completed. Deaths={DeathCount} Time={PlayTimeFormatted}");
            CheckMilestones();
        }

        /// <summary>
        /// Alias for <see cref="RecordLevelComplete"/> — called by CourseClearScene.
        /// Both names are kept so existing call-sites continue to compile.
        /// </summary>
        public void RecordLevelCompleted() => RecordLevelComplete();

        // ── Team 2 — Idea 1: milestone evaluation ─────────────────────────────
        /// <summary>
        /// Checks and unlocks any milestones the player has just reached.
        /// Called after each recorded event so milestones are awarded immediately.
        /// Team 2 (Producer) — Idea 1.
        /// </summary>
        private void CheckMilestones()
        {
            TryUnlockMilestone("First Steps",         DeathCount   == 0  && LevelsCompleted >= 1);
            TryUnlockMilestone("Adventurer",          LevelsCompleted >= 3);
            TryUnlockMilestone("Boss Hunter",         BossesDefeated  >= 1);
            TryUnlockMilestone("Warlord Slayer",      BossesDefeated  >= 4);
            TryUnlockMilestone("Combo Apprentice",    LongestCombo    >= 5);
            TryUnlockMilestone("Combo Master",        LongestCombo    >= 15);
            TryUnlockMilestone("Berry Collector",     BerriesCollected >= 100);
            TryUnlockMilestone("Enemy of the State",  EnemiesDefeated >= 50);
            TryUnlockMilestone("Persistent",          DeathCount      >= 10);
            TryUnlockMilestone("Unstoppable",         DeathCount      == 0  && BossesDefeated >= 2);
        }

        private void TryUnlockMilestone(string name, bool condition)
        {
            if (!condition || _unlockedMilestones.Contains(name)) return;
            _unlockedMilestones.Add(name);
            LogEvent($"MILESTONE:{name}");
            DebugLogger.LogInfo("SessionStats.Milestone", $"Milestone unlocked: {name}");
            EventBus.Publish(new MilestoneEarnedEvent { Name = name });
        }

        // -- Team 2 -- Idea 6: playtime badges
        /// <summary>
        /// Awards a badge notification when the player crosses a cumulative
        /// playtime threshold for the first time this session.
        /// Team 2 (Producer) -- Idea 6.
        /// </summary>
        private void CheckPlaytimeBadges()
        {
            float playHours = PlaySeconds / 3600f;
            foreach (float threshold in PlaytimeBadgeHours)
            {
                string badge = string.Format("Played {0:0}h", threshold);
                if (playHours >= threshold && !_unlockedMilestones.Contains(badge))
                    TryUnlockMilestone(badge, true);
            }
        }

        // -- Team 2 -- Idea 2: play-streak tracker
        /// <summary>Number of consecutive days the player has opened the game.</summary>
        public int PlayStreak { get; private set; }

        /// <summary>
        /// Reads the streak file, increments if today is a new day, and saves.
        /// Team 2 (Producer) -- Idea 2.
        /// </summary>
        private void AdvancePlayStreak()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StreakFile));
                DateTime lastPlay = DateTime.MinValue;
                int      streak   = 0;

                if (File.Exists(StreakFile))
                {
                    string[] lines = File.ReadAllLines(StreakFile);
                    if (lines.Length >= 2)
                    {
                        DateTime.TryParse(lines[0], out lastPlay);
                        int.TryParse(lines[1], out streak);
                    }
                }

                DateTime today = DateTime.Today;
                if (lastPlay.Date == today)
                {
                    // Already played today -- keep streak as-is.
                }
                else if (lastPlay.Date == today.AddDays(-1))
                {
                    // Consecutive day -- extend streak.
                    streak++;
                }
                else
                {
                    // Streak broken.
                    streak = 1;
                }

                PlayStreak = streak;
                File.WriteAllText(StreakFile, string.Format("{0:yyyy-MM-dd}\n{1}", today, streak));
            }
            catch { PlayStreak = 1; }
        }

        // -- Team 2 -- Idea 7: daily reminder
        /// <summary>
        /// True if the player has not played in more than 24 hours.
        /// Shown as a "Welcome back!" note on the title screen.
        /// Team 2 (Producer) -- Idea 7.
        /// </summary>
        public bool IsReturnPlayer { get; private set; }

        private void CheckDailyReminder()
        {
            try
            {
                if (!File.Exists(StreakFile)) return;
                string[] lines = File.ReadAllLines(StreakFile);
                if (lines.Length < 1) return;
                DateTime last;
                if (DateTime.TryParse(lines[0], out last))
                    IsReturnPlayer = (DateTime.Today - last.Date).TotalDays > 1;
            }
            catch { }
        }

        // -- Team 2 -- Idea 4: analytics event log
        /// <summary>
        /// Appends a timestamped analytics event to the in-memory event list.
        /// Team 2 (Producer) -- Idea 4.
        /// </summary>
        private void LogEvent(string evt)
        {
            _events.Add(string.Format("{0:HH:mm:ss.fff}|{1}", DateTime.Now, evt));
        }

        // -- Team 2 -- Idea 3: analytics CSV export
        /// <summary>
        /// Appends a single-row CSV summary of this session to a running telemetry file.
        /// Called at session close so every session has a permanent audit trail.
        /// Team 2 (Producer) -- Ideas 3 and 9 (version annotation).
        /// </summary>
        public void ExportAnalytics()
        {
            try
            {
                string dir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                string path = Path.Combine(dir, "analytics.csv");
                Directory.CreateDirectory(dir);

                bool isNew = !File.Exists(path) || new FileInfo(path).Length == 0;
                using (var sw = new StreamWriter(path, append: true))
                {
                    if (isNew)
                        sw.WriteLine("SessionId,Date,Duration,Deaths,Berries,Enemies,Bosses,Levels,LongestCombo,PlayStreak,Version");
                    sw.WriteLine(string.Format("{0},{1:yyyy-MM-dd HH:mm:ss},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                        SessionId, SessionStart, PlayTimeFormatted,
                        DeathCount, BerriesCollected, EnemiesDefeated, BossesDefeated,
                        LevelsCompleted, LongestCombo, PlayStreak, BuildInfo.Version));
                }
                DebugLogger.LogInfo("SessionStats.Export", "Analytics exported to " + path);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("SessionStats.ExportAnalytics", ex);
            }
        }

        // -- Team 2 -- Idea 8: in-game tip system
        /// <summary>
        /// Returns the next cycling tip string.
        /// Called by LoadingScene and transition screens to fill dead air.
        /// Team 2 (Producer) -- Idea 8.
        /// </summary>
        public string NextTip()
        {
            string tip = _tips[_tipIndex % _tips.Length];
            _tipIndex++;
            return tip;
        }

        // -- Summary
        /// <summary>
        /// Writes a full session summary to the debug log.
        /// Call at application exit or scene transition for QA records.
        /// </summary>
        public void WriteSummaryToLog()
        {
            string summary = string.Format(
                "[Session {0}] PlayTime={1} | Deaths={2} | Berries={3} | Enemies={4} | " +
                "Bosses={5} | Levels={6} | Checkpoints={7} | PowerUps={8} | LongestCombo={9} | Streak={10}d",
                SessionId, PlayTimeFormatted, DeathCount, BerriesCollected, EnemiesDefeated,
                BossesDefeated, LevelsCompleted, CheckpointsReached, PowerUpsCollected, LongestCombo, PlayStreak);
            DebugLogger.LogInfo("SessionStats.Summary", summary);
            ExportAnalytics();
        }

        /// <summary>Returns a compact multi-line string for the HUD / QA scene.</summary>
        public string ToDisplayString()
        {
            return
                "Play Time  : " + PlayTimeFormatted + "\n" +
                "Deaths     : " + DeathCount        + "\n" +
                "Berries    : " + BerriesCollected   + "\n" +
                "Enemies    : " + EnemiesDefeated    + "\n" +
                "Bosses     : " + BossesDefeated     + "\n" +
                "Checkpoints: " + CheckpointsReached + "\n" +
                "Power-Ups  : " + PowerUpsCollected  + "\n" +
                "Best Combo : " + LongestCombo + "x\n" +
                "Day Streak : " + PlayStreak;
        }

        /// <summary>
        /// Records a level-segment change (e.g. Underground to Sky).
        /// Called by LevelEventSystem when the active segment changes.
        /// </summary>
        public void RecordSegmentChange(string segment)
        {
            LogEvent("SEGMENT_CHANGE:" + segment);
        }
    }

    // -- Team 2 -- Idea 1: milestone event for UI notification bus
    /// <summary>
    /// Published on the EventBus when a new milestone is unlocked.
    /// The SMB3Hud and AchievementsScene listen for this to show a banner.
    /// Team 2 (Producer) -- Idea 1.
    /// </summary>
    public sealed class MilestoneEarnedEvent
    {
        /// <summary>Human-readable milestone name.</summary>
        public string Name { get; set; }
    }
}