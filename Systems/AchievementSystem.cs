using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// SMB3-inspired achievement / badge system.
    ///
    /// Team 1  (Game Director)  — defines the achievement roster and progression rewards.
    /// Team 2  (Producer)       — milestone tracking, completion % feed.
    /// Team 19 (QA Tester)      — achievement list included in QA report.
    ///
    /// Achievements are stored in SaveData via flag names (prefix "ach_").
    /// A notification banner is raised via EventBus when one is earned.
    /// </summary>
    public static class AchievementSystem
    {
        // ── Achievement definition ────────────────────────────────────────────
        public sealed class Achievement
        {
            /// <summary>Unique save-flag key (prefixed with "ach_").</summary>
            public string Id          { get; set; }
            /// <summary>Display name shown in the achievements scene and banner.</summary>
            public string Name        { get; set; }
            /// <summary>Short description (one sentence).</summary>
            public string Description { get; set; }
            /// <summary>Icon label used for the badge tile (1–2 chars or emoji-safe ASCII).</summary>
            public string Icon        { get; set; }
            /// <summary>Banner accent color for the notification.</summary>
            public Color  Color       { get; set; } = Color.Gold;
        }

        // ── Achievement registry ──────────────────────────────────────────────
        public static readonly IReadOnlyList<Achievement> All = BuildList();

        private static List<Achievement> BuildList()
        {
            return new List<Achievement>
            {
                // ── Progression milestones (SMB3-style world completion) ──────
                new Achievement { Id="ach_first_step",   Name="First Step",       Description="Complete your first island level.",               Icon="★", Color=Color.Gold },
                new Achievement { Id="ach_sky_walker",   Name="Sky Walker",       Description="Complete the Sky Island without taking damage.",   Icon="☁", Color=Color.DeepSkyBlue },
                new Achievement { Id="ach_storm_rider",  Name="Storm Rider",      Description="Survive the full storm sequence.",                 Icon="⚡", Color=Color.Yellow },
                new Achievement { Id="ach_boss_slayer",  Name="Boss Slayer",      Description="Defeat the Marine Captain.",                       Icon="☠", Color=Color.OrangeRed },
                new Achievement { Id="ach_warlord_bane", Name="Warlord's Bane",   Description="Defeat all four Warlord variants.",               Icon="W", Color=Color.Crimson },

                // ── Collection (SMB3 coin/item goals) ─────────────────────────
                new Achievement { Id="ach_berry_100",    Name="Coin Collector",   Description="Collect 100 berries in a single session.",        Icon="●", Color=Color.LimeGreen },
                new Achievement { Id="ach_berry_500",    Name="Berry Hoarder",    Description="Collect 500 berries total.",                      Icon="◉", Color=Color.Green },
                new Achievement { Id="ach_powerup_3",    Name="Power Hungry",     Description="Collect 3 power-ups in one session.",             Icon="P", Color=Color.Magenta },

                // ── Combat (Mega Man-style mastery) ───────────────────────────
                new Achievement { Id="ach_combo_5",      Name="Combo Starter",    Description="Defeat 5 enemies in a row without taking damage.", Icon="×", Color=Color.Orange },
                new Achievement { Id="ach_combo_10",     Name="Combo Master",     Description="Defeat 10 enemies in a row without taking damage.",Icon="✕", Color=Color.Gold },
                new Achievement { Id="ach_no_death",     Name="Untouchable",      Description="Complete any level without dying.",                Icon="♥", Color=Color.HotPink },

                // ── Exploration ───────────────────────────────────────────────
                new Achievement { Id="ach_checkpoint",   Name="Safe Harbor",      Description="Reach your first checkpoint.",                    Icon="⚑", Color=Color.Cyan },
                new Achievement { Id="ach_crew_5",       Name="Crew United",      Description="Reach 5 Crew Bonds.",                            Icon="⚓", Color=Color.CornflowerBlue },

                // ── SMB3 secrets ──────────────────────────────────────────────
                new Achievement { Id="ach_wall_jump",    Name="Wall Climber",     Description="Perform a wall jump.",                            Icon="↑", Color=Color.LightBlue },
                new Achievement { Id="ach_ground_pound", Name="Ground Pounder",   Description="Defeat an enemy with a ground pound.",            Icon="↓", Color=Color.SaddleBrown },
                new Achievement { Id="ach_full_clear",   Name="World Complete",   Description="Clear all four numbered levels.",                 Icon="♛", Color=Color.Gold },

                // ── QA / secrets ─────────────────────────────────────────────
                new Achievement { Id="ach_dev_menu",     Name="Behind the Curtain",Description="Open the dev menu.",                            Icon="D", Color=Color.DimGray },
                new Achievement { Id="ach_god_mode",     Name="Invincible",        Description="Activate God Mode.",                            Icon="G", Color=Color.Silver },

                // ── Session ───────────────────────────────────────────────────
                new Achievement { Id="ach_marathon",     Name="Marathon Runner",  Description="Play for 30 minutes in one session.",             Icon="⌚", Color=Color.Teal },
            };
        }

        // ── Grant ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Grants an achievement by id. No-ops if already earned or id is unknown.
        /// Saves immediately and fires an AchievementEarnedEvent via EventBus.
        /// </summary>
        public static void Grant(string id)
        {
            var save = Engine.Game.Instance?.Save;
            if (save == null) return;

            string flag = EnsurePrefix(id);
            if (save.GetFlag(flag)) return;   // already earned

            var ach = FindById(id);
            if (ach == null) return;

            save.SetFlag(flag);
            save.Save();

            // Notify subscribers (banner, analytics, QA log).
            EventBus.Publish(new AchievementEarnedEvent { Achievement = ach });
            DebugLogger.LogInfo("Achievement.Grant", $"Earned: {ach.Name}");
        }

        /// <summary>Returns true if the achievement has been earned.</summary>
        public static bool IsEarned(string id)
        {
            var save = Engine.Game.Instance?.Save;
            return save != null && save.GetFlag(EnsurePrefix(id));
        }

        /// <summary>Returns count of earned achievements.</summary>
        public static int EarnedCount()
        {
            int count = 0;
            foreach (var a in All)
                if (IsEarned(a.Id)) count++;
            return count;
        }

        // ── Checks (called by gameplay systems) ───────────────────────────────
        /// <summary>Check berry-collection milestones.</summary>
        public static void CheckBerryMilestones(int sessionBerries, int totalBerries)
        {
            if (sessionBerries >= 100) Grant("ach_berry_100");
            if (totalBerries   >= 500) Grant("ach_berry_500");
        }

        /// <summary>Check combo milestones.</summary>
        public static void CheckCombo(int currentCombo)
        {
            if (currentCombo >= 5)  Grant("ach_combo_5");
            if (currentCombo >= 10) Grant("ach_combo_10");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static Achievement FindById(string id)
        {
            foreach (var a in All)
                if (a.Id == EnsurePrefix(id) || a.Id == id)
                    return a;
            return null;
        }

        private static string EnsurePrefix(string id)
        {
            return id.StartsWith("ach_") ? id : "ach_" + id;
        }
    }

    // ── Achievement earned event ──────────────────────────────────────────────
    /// <summary>Published on EventBus when an achievement is first earned.</summary>
    public struct AchievementEarnedEvent
    {
        public AchievementSystem.Achievement Achievement;
    }
}
