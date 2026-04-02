$base = "C:\Users\stuff\Desktop\Classes\CS-120\PROJECTS\CS-120\Weeks\Week 10\Fridays Adventure"
$file = Join-Path $base "Systems\SessionStats.cs"

# Keep the intact first 252 lines
$goodLines = (Get-Content $file)[0..251]
$good = $goodLines -join "`r`n"

$tail = @'

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
'@

[System.IO.File]::WriteAllText($file, $good + "`r`n" + $tail, [System.Text.Encoding]::UTF8)
Write-Host "SessionStats.cs rebuilt successfully."
