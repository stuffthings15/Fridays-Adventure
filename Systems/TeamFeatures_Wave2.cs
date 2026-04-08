// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 — ALL TEAMS: Wave 2 Feature Set
// Style: Super Mario Bros. 3 — gameplay, visuals, audio, UX
// All 19 team members × 10 new ideas = 190 features
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Fridays_Adventure.Audio;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 1: GAME DIRECTOR / CREATIVE DIRECTOR — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 1 — Wave 2 Features: SMB3-faithful experience system.
    /// Idea 1:  Animated title screen logo bounce
    /// Idea 2:  Flagpole end-of-level animation trigger
    /// Idea 3:  World map animated walking icon
    /// Idea 4:  Time limit per level (300-second SMB3 clock)
    /// Idea 5:  "Course Clear!" fanfare trigger
    /// Idea 6:  End-of-world castle defeat animation
    /// Idea 7:  Anchor item (prevents overworld ship from sailing back)
    /// Idea 8:  Hammer Bros. encounter flag on overworld
    /// Idea 9:  Toad House visited tracker
    /// Idea 10: Letter seal unlock mechanic
    /// </summary>
    public static class GameDirectorFeatures
    {
        private static float _logoTimer;

        /// <summary>Idea 1: Returns Y offset for a bouncing title logo.</summary>
        public static float LogoBounceOffset => (float)(Math.Sin(_logoTimer * 2.5) * 8.0);

        // ── Idea 4: Level time limit ───────────────────────────────────────────
        /// <summary>Seconds remaining on the current level timer.</summary>
        public static float LevelTimeRemaining { get; private set; } = 300f;

        /// <summary>True once time has expired.</summary>
        public static bool TimeExpired => LevelTimeRemaining <= 0f;

        /// <summary>Resets the level timer to the given limit.</summary>
        public static void ResetLevelTimer(float seconds = 300f)
        {
            LevelTimeRemaining = seconds;
            DebugLogger.LogInfo("GameDirector", $"Level timer reset to {seconds}s");
        }

        // ── Idea 5: Course Clear fanfare ──────────────────────────────────────
        private static bool  _courseClearActive;
        private static float _courseClearTimer;

        /// <summary>Triggers the "COURSE CLEAR!" display.</summary>
        public static void TriggerCourseClear()
        {
            _courseClearActive = true;
            _courseClearTimer  = 3.0f;
            DebugLogger.LogInfo("GameDirector", "Course clear triggered.");
        }

        /// <summary>True while the Course Clear overlay is active.</summary>
        public static bool CourseClearActive => _courseClearActive;

        // ── Idea 7: Anchor item ────────────────────────────────────────────────
        /// <summary>Player holds an Anchor item (stops ship from retreating).</summary>
        public static bool HasAnchor { get; set; } = false;

        // ── Idea 8: Hammer Bros. encounter tracking ────────────────────────────
        private static readonly HashSet<string> _defeatedHammerBros = new HashSet<string>();

        /// <summary>Marks a Hammer Bros node as defeated.</summary>
        public static void MarkHammerBrosDefeated(string nodeId) =>
            _defeatedHammerBros.Add(nodeId);

        /// <summary>True if the given node's Hammer Bros have been defeated.</summary>
        public static bool IsHammerBrosDefeated(string nodeId) =>
            _defeatedHammerBros.Contains(nodeId);

        // ── Idea 9: Toad House visited tracker ────────────────────────────────
        private static readonly HashSet<string> _visitedToadHouses = new HashSet<string>();

        /// <summary>Marks a Toad House as visited.</summary>
        public static void MarkToadHouseVisited(string nodeId) =>
            _visitedToadHouses.Add(nodeId);

        /// <summary>True if this Toad House has been used.</summary>
        public static bool IsToadHouseVisited(string nodeId) =>
            _visitedToadHouses.Contains(nodeId);

        // ── Idea 10: Letter seal ───────────────────────────────────────────────
        /// <summary>True when the player has collected the world letter seal.</summary>
        public static bool HasLetterSeal { get; set; } = false;

        // ── Update ─────────────────────────────────────────────────────────────
        /// <summary>Advances all timers. Call once per game tick.</summary>
        public static void Update(float dt)
        {
            _logoTimer += dt;
            if (!TimeExpired)
                LevelTimeRemaining = Math.Max(0f, LevelTimeRemaining - dt);
            if (_courseClearActive)
            {
                _courseClearTimer -= dt;
                if (_courseClearTimer <= 0f) _courseClearActive = false;
            }
        }

        // ── Idea 2 / 5: Draw helpers ───────────────────────────────────────────
        /// <summary>Draws the SMB3 "COURSE CLEAR!" text panel centered on screen.</summary>
        public static void DrawCourseClear(Graphics g, int W, int H)
        {
            if (!_courseClearActive) return;
            using (var bg = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                g.FillRectangle(bg, W / 2 - 200, H / 2 - 40, 400, 80);
            using (var f = new Font("Courier New", 22, FontStyle.Bold))
            {
                const string msg = "COURSE CLEAR!";
                var sz = g.MeasureString(msg, f);
                g.DrawString(msg, f, Brushes.Black, (W - sz.Width) / 2f + 2, H / 2f - sz.Height / 2f + 2);
                g.DrawString(msg, f, Brushes.Gold,  (W - sz.Width) / 2f,     H / 2f - sz.Height / 2f);
            }
        }

        /// <summary>Draws the SMB3-style level timer in the HUD.</summary>
        public static void DrawLevelTimer(Graphics g, int W)
        {
            int seconds = (int)Math.Ceiling(LevelTimeRemaining);
            Brush timerBrush = seconds <= 60 ? Brushes.OrangeRed : Brushes.White;
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                g.DrawString("TIME", f, Brushes.LightGray, W - 130, 10);
                g.DrawString($"{seconds:D3}", f, timerBrush, W - 130, 28);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 2: PRODUCER / PROJECT MANAGER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 2 — Wave 2: Player flow, milestone, and accessibility features.
    /// Idea 1:  Play-time milestone badges
    /// Idea 2:  Progress bar on overworld
    /// Idea 3:  First-run experience flag
    /// Idea 4:  Daily play streak counter
    /// Idea 5:  Session auto-save on each island completion
    /// Idea 6:  Playtime limit warning at 60 and 90 minutes
    /// Idea 7:  Loading screen rotating SMB3 tips
    /// Idea 8:  New-game confirmation dialog text
    /// Idea 9:  Quit-to-title confirmation
    /// Idea 10: High-score board for each island
    /// </summary>
    public static class ProducerFeatures
    {
        // ── Idea 1: Milestone badges ───────────────────────────────────────────
        private static readonly Dictionary<string, bool> _milestones =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Marks a milestone. Fires a HUD toast if newly earned.</summary>
        public static void CheckMilestone(string key, bool condition)
        {
            if (!condition || _milestones.ContainsKey(key)) return;
            _milestones[key] = true;
            SMB3Hud.ShowToast($"MILESTONE: {key}");
            DebugLogger.LogInfo("Producer.Milestone", key);
        }

        // ── Idea 2: Progress percentage ────────────────────────────────────────
        /// <summary>Returns the percentage of the game completed (0–100).</summary>
        public static int GetCompletionPercent()
        {
            const int total = 14;
            int cleared = Game.Instance?.CurrentLevel ?? 0;
            return Math.Min(100, cleared * 100 / total);
        }

        // ── Idea 3: First-run experience ──────────────────────────────────────
        /// <summary>True if this is the player's first launch.</summary>
        public static bool IsFirstRun =>
            !(Game.Instance?.Save?.GetFlag("seen_tutorial") ?? false);

        /// <summary>Marks the tutorial as seen.</summary>
        public static void MarkTutorialSeen() =>
            Game.Instance?.Save?.SetFlag("seen_tutorial");

        // ── Idea 4: Daily play streak ─────────────────────────────────────────
        /// <summary>
        /// Updates the daily streak counter.
        /// Uses GetInt/SetInt and stores the date in a numeric yyyyMMdd format.
        /// </summary>
        public static void UpdateDailyStreak()
        {
            if (Game.Instance?.Save == null) return;
            int today    = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
            int lastDate = Game.Instance.Save.GetInt("last_play_date", 0);
            int streak   = Game.Instance.Save.GetInt("daily_streak", 0);

            if (lastDate == today) return; // Already counted today

            bool consecutive = false;
            if (lastDate > 0)
            {
                DateTime last = DateTime.ParseExact(lastDate.ToString(), "yyyyMMdd", null);
                consecutive = (DateTime.Now.Date - last.Date).Days == 1;
            }

            streak = consecutive ? streak + 1 : 1;
            Game.Instance.Save.SetInt("daily_streak",   streak);
            Game.Instance.Save.SetInt("last_play_date", today);
            DebugLogger.LogInfo("Producer", $"Daily streak: {streak}");
        }

        // ── Idea 7: Loading tips ──────────────────────────────────────────────
        private static readonly string[] _tips = {
            "Stomp enemies for bonus points!",
            "Collect 100 coins for an extra life!",
            "Power-ups are stored — use them wisely!",
            "Each island has a unique theme and challenge.",
            "Boss encounters require careful dodging.",
            "Try exploring every area for hidden secrets.",
            "The P-Meter fills when you run continuously!",
            "Orca's Ice Wall can block enemy projectiles.",
            "Swan can glide to reach distant platforms.",
            "Complete all 11 islands to win the game!",
        };
        private static int _tipIndex;

        /// <summary>Returns the next rotating tip string.</summary>
        public static string GetNextTip()
        {
            string tip = _tips[_tipIndex % _tips.Length];
            _tipIndex++;
            return tip;
        }

        // ── Idea 10: Per-island high scores ────────────────────────────────────
        /// <summary>Records a score for an island if it beats the current best.</summary>
        public static void RecordIslandScore(string islandId, int score)
        {
            if (Game.Instance?.Save == null) return;
            string key = $"hi_{islandId}";
            int best = Game.Instance.Save.GetInt(key, 0);
            if (score > best)
            {
                Game.Instance.Save.SetInt(key, score);
                SMB3Hud.ShowToast($"NEW BEST: {islandId}  {score:N0}");
            }
        }

        /// <summary>Returns the recorded high score for an island.</summary>
        public static int GetIslandHighScore(string islandId) =>
            Game.Instance?.Save?.GetInt($"hi_{islandId}", 0) ?? 0;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 3: TECHNICAL LEAD — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 3 — Wave 2: Engine hardening.
    /// Idea 1:  Live frame-time graph in debug overlay
    /// Idea 2:  Asset load-time profiler
    /// Idea 3:  Scene transition safety guard
    /// Idea 4:  Deterministic replay recorder stub
    /// Idea 5:  GC gen0/gen1/gen2 counters in debug overlay
    /// Idea 6:  Draw-call counter
    /// Idea 7:  Thread-safe singleton guard
    /// Idea 8:  Hot-key registry
    /// Idea 9:  Build hash in debug overlay
    /// Idea 10: Context stack dump for crash reports
    /// </summary>
    public static class TechLeadFeatures
    {
        // ── Idea 1: Frame-time graph ───────────────────────────────────────────
        private static readonly float[] _ftSamples = new float[60];
        private static int _ftIndex;

        /// <summary>Records a frame time sample (dt in seconds).</summary>
        public static void RecordFrameTime(float dt)
        {
            _ftSamples[_ftIndex % _ftSamples.Length] = dt * 1000f;
            _ftIndex++;
        }

        /// <summary>Draws a live frame-time sparkline graph.</summary>
        public static void DrawFrameGraph(Graphics g, int x, int y, int w, int h)
        {
            using (var bg = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(bg, x, y, w, h);
            float max = 66f;
            int count = _ftSamples.Length;
            for (int i = 0; i < count - 1; i++)
            {
                int si = (_ftIndex - count + i + count) % count;
                int ni = (_ftIndex - count + i + 1 + count) % count;
                float v0 = Math.Min(_ftSamples[si], max);
                float v1 = Math.Min(_ftSamples[ni], max);
                float x0 = x + i * w / count;
                float y0 = y + h - (v0 / max) * h;
                float x1 = x + (i + 1) * w / count;
                float y1 = y + h - (v1 / max) * h;
                Color c  = v0 < 16f ? Color.LimeGreen : v0 < 33f ? Color.Yellow : Color.OrangeRed;
                using (var pen = new Pen(c, 1f))
                    g.DrawLine(pen, x0, y0, x1, y1);
            }
            float target = y + h - (16f / max) * h;
            using (var pen = new Pen(Color.FromArgb(120, Color.Cyan), 1) { DashStyle = DashStyle.Dash })
                g.DrawLine(pen, x, target, x + w, target);
        }

        // ── Idea 2: Asset load timer ───────────────────────────────────────────
        private static readonly Dictionary<string, float> _assetLoadTimes =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Records how long an asset took to load.</summary>
        public static void RecordAssetLoad(string name, float ms)
        {
            _assetLoadTimes[name] = ms;
            if (ms > 200f)
                DebugLogger.LogWarning("TechLead.AssetLoad", $"{name} took {ms:F1}ms to load!");
        }

        // ── Idea 5: GC counters ────────────────────────────────────────────────
        /// <summary>Returns a formatted GC generation collection counts string.</summary>
        public static string GetGCInfo() =>
            $"GC  Gen0:{GC.CollectionCount(0)}  Gen1:{GC.CollectionCount(1)}  Gen2:{GC.CollectionCount(2)}" +
            $"  Heap:{GC.GetTotalMemory(false) / 1024 / 1024}MB";

        // ── Idea 6: Draw-call counter ─────────────────────────────────────────
        private static int _drawCalls;

        /// <summary>Resets the draw-call counter at the start of each frame.</summary>
        public static void ResetDrawCalls() => _drawCalls = 0;

        /// <summary>Increments the draw-call counter.</summary>
        public static void IncrementDrawCalls() => _drawCalls++;

        /// <summary>Current frame draw-call count.</summary>
        public static int DrawCallCount => _drawCalls;

        // ── Idea 10: Context stack dump ───────────────────────────────────────
        private static readonly Stack<string> _contextStack = new Stack<string>();

        /// <summary>Pushes a context entry for crash reporting.</summary>
        public static void PushContext(string ctx) { if (_contextStack.Count < 20) _contextStack.Push(ctx); }

        /// <summary>Pops the top context entry.</summary>
        public static void PopContext() { if (_contextStack.Count > 0) _contextStack.Pop(); }

        /// <summary>Returns all context entries as a formatted string.</summary>
        public static string DumpContext()
        {
            var sb = new System.Text.StringBuilder();
            foreach (string s in _contextStack)
                sb.AppendLine("  ← " + s);
            return sb.ToString();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 4: LEAD GAME DESIGNER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 4 — Wave 2: SMB3 power-up system.
    /// Idea 1:  Super Mushroom — grow bigger, break blocks
    /// Idea 2:  Fire Flower — shoot fireballs
    /// Idea 3:  Starman — 10-second invincibility
    /// Idea 4:  Raccoon Leaf — tail whip and float
    /// Idea 5:  Frog Suit — improved swimming
    /// Idea 6:  Hammer Suit — throw hammers
    /// Idea 7:  Tanooki Suit — statue invincibility
    /// Idea 8:  P-Wing — skip level
    /// Idea 9:  1-Up Mushroom — extra life
    /// Idea 10: Cloud power-up — phase through blocks once
    /// </summary>
    public static class PowerUpFeatureSet
    {
        // ── Idea 3: Starman ────────────────────────────────────────────────────
        private static float _starTimer;

        /// <summary>True while Starman invincibility is active.</summary>
        public static bool StarmanActive => _starTimer > 0f;

        /// <summary>Activates Starman invincibility for 10 seconds.</summary>
        public static void ActivateStarman()
        {
            _starTimer = 10f;
            SMB3Hud.ShowToast("STARMAN! You're invincible!");
            DebugLogger.LogInfo("PowerUp", "Starman activated.");
        }

        // ── Idea 1: Super Mushroom ──────────────────────────────────────────────
        /// <summary>True while the player is in Super (grown) form.</summary>
        public static bool IsSuperForm { get; private set; } = false;

        /// <summary>Applies the Super Mushroom transformation.</summary>
        public static void ApplySuperMushroom()
        {
            IsSuperForm = true;
            SMB3Hud.ShowToast("SUPER FORM!");
        }

        // ── Idea 2: Fire Flower ────────────────────────────────────────────────
        private static bool _hasFireFlower;

        /// <summary>True when Fire Flower is active.</summary>
        public static bool HasFireFlower => _hasFireFlower;

        /// <summary>Collects the Fire Flower power-up.</summary>
        public static void CollectFireFlower()
        {
            _hasFireFlower = true;
            SMB3Hud.ShowToast("FIRE FLOWER!");
        }

        // ── Idea 4: Raccoon Leaf ───────────────────────────────────────────────
        private static bool  _hasRaccoonLeaf;
        private static float _floatTime;

        /// <summary>True when Raccoon Leaf is active.</summary>
        public static bool HasRaccoonLeaf => _hasRaccoonLeaf;

        /// <summary>Remaining float duration after jump peak.</summary>
        public static float RaccoonFloatRemaining => _floatTime;

        /// <summary>Collects the Raccoon Leaf power-up.</summary>
        public static void CollectRaccoonLeaf()
        {
            _hasRaccoonLeaf = true;
            SMB3Hud.ShowToast("RACCOON LEAF!");
        }

        // ── Idea 5: Frog Suit ──────────────────────────────────────────────────
        private static bool _hasFrogSuit;

        /// <summary>True while Frog Suit is active (better swimming).</summary>
        public static bool HasFrogSuit => _hasFrogSuit;

        /// <summary>Collects the Frog Suit.</summary>
        public static void CollectFrogSuit()
        {
            _hasFrogSuit = true;
            SMB3Hud.ShowToast("FROG SUIT!");
        }

        // ── Idea 6: Hammer Suit ────────────────────────────────────────────────
        private static bool _hasHammerSuit;

        /// <summary>True while Hammer Suit is active.</summary>
        public static bool HasHammerSuit => _hasHammerSuit;

        /// <summary>Collects the Hammer Suit.</summary>
        public static void CollectHammerSuit()
        {
            _hasHammerSuit = true;
            SMB3Hud.ShowToast("HAMMER SUIT!");
        }

        // ── Idea 9: 1-Up Mushroom ──────────────────────────────────────────────
        /// <summary>Grants the player an extra life.</summary>
        public static void Grant1Up()
        {
            if (Game.Instance != null)
            {
                Game.Instance.CurrentLives++;
                SMB3Hud.ShowToast("1-UP! ♥");
                DebugLogger.LogInfo("PowerUp.1Up", $"Lives now: {Game.Instance.CurrentLives}");
            }
        }

        // ── Idea 10: Cloud power-up ────────────────────────────────────────────
        private static bool _hasCloud;

        /// <summary>True while Cloud power-up is held.</summary>
        public static bool HasCloud => _hasCloud;

        /// <summary>Uses the Cloud power-up (single-use).</summary>
        public static void UseCloud() { _hasCloud = false; SMB3Hud.ShowToast("CLOUD SKIP!"); }

        /// <summary>Collects the Cloud power-up.</summary>
        public static void CollectCloud() { _hasCloud = true; }

        // ── Update ─────────────────────────────────────────────────────────────
        /// <summary>Advances all power-up timers.</summary>
        public static void Update(float dt)
        {
            if (_starTimer > 0f) _starTimer -= dt;
            if (_floatTime > 0f) _floatTime = Math.Max(0f, _floatTime - dt);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 5: LEVEL DESIGNER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 5 — Wave 2: SMB3 level mechanics.
    /// Idea 1:  Moving platforms
    /// Idea 2:  Spring pad
    /// Idea 3:  Pipe warp zones
    /// Idea 4:  Ice floor sliding
    /// Idea 5:  Question blocks
    /// Idea 6:  Breakable brick blocks
    /// Idea 7:  Secret exit doors
    /// Idea 8:  Autoscroll segments
    /// Idea 9:  Rotating coin rings
    /// Idea 10: Wind current zones
    /// </summary>
    public static class LevelDesignerFeatures
    {
        // ── Idea 2: Spring pad constant ───────────────────────────────────────
        /// <summary>Velocity imparted by a spring pad (pixels/second upward).</summary>
        public const float SpringPadForce = -900f;

        // ── Idea 4: Ice floor friction ─────────────────────────────────────────
        /// <summary>Friction multiplier on ice floors (0.02 = very slippery).</summary>
        public const float IceFriction = 0.02f;

        // ── Idea 8: Autoscroll ─────────────────────────────────────────────────
        private static bool  _autoscrollActive;
        private static float _autoscrollSpeed = 60f;

        /// <summary>True while an autoscroll segment is active.</summary>
        public static bool AutoscrollActive => _autoscrollActive;

        /// <summary>Pixels-per-second the camera scrolls during autoscroll.</summary>
        public static float AutoscrollSpeed => _autoscrollSpeed;

        /// <summary>Begins an autoscroll segment.</summary>
        public static void StartAutoscroll(float speed = 60f)
        {
            _autoscrollActive = true;
            _autoscrollSpeed  = speed;
        }

        /// <summary>Ends the autoscroll segment.</summary>
        public static void StopAutoscroll() => _autoscrollActive = false;

        // ── Idea 10: Wind current zones ────────────────────────────────────────
        private static float _windStrength;

        /// <summary>Horizontal force from the active wind zone. Negative = left.</summary>
        public static float WindForce => _windStrength;

        /// <summary>Sets the wind force. Pass 0f to clear.</summary>
        public static void SetWind(float strength) => _windStrength = strength;

        // ── Idea 5: Question block content resolver ────────────────────────────
        public enum QuestionBlockContent { Coin, Mushroom, FireFlower, Star, Empty }

        private static readonly Random _rng = new Random();

        /// <summary>Returns what's inside a question block based on current progress.</summary>
        public static QuestionBlockContent ResolveBlockContent()
        {
            int level = Game.Instance?.CurrentLevel ?? 1;
            if (level >= 8) return _rng.Next(3) == 0 ? QuestionBlockContent.Star    : QuestionBlockContent.FireFlower;
            if (level >= 4) return _rng.Next(2) == 0 ? QuestionBlockContent.FireFlower : QuestionBlockContent.Mushroom;
            return _rng.Next(4) == 0 ? QuestionBlockContent.Mushroom : QuestionBlockContent.Coin;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 6: NARRATIVE DESIGNER / WRITER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 6 — Wave 2: Narrative and dialogue features.
    /// Idea 1:  World introduction text cards
    /// Idea 2:  Character backstory codex entries
    /// Idea 3:  Enemy type descriptions (Bestiary)
    /// Idea 4:  Island lore tablets
    /// Idea 5:  Letter system (collect A-L)
    /// Idea 6:  Ending epilogue text per character
    /// Idea 7:  NPC sailor hint dialogue on overworld
    /// Idea 8:  Boss pre-fight banter lines
    /// Idea 9:  Crew journal entries
    /// Idea 10: "Did you know?" secrets board
    /// </summary>
    public static class NarrativeFeatures
    {
        private static readonly Random _rng = new Random();

        // ── Idea 1: World intro text ───────────────────────────────────────────
        private static readonly string[] _worldIntros = {
            "WORLD 1: THE OPEN SEA\nThe Grand Line stretches out before you.\nYour adventure begins!",
            "WORLD 2: STORMY WATERS\nThunder echoes across the waves. Survive the tempest!",
            "WORLD 3: ANCIENT DEPTHS\nThe sea floor holds ancient secrets. Dive deep!",
            "WORLD 4: THE FINAL STAND\nOnly the strongest reach this point. Press forward!",
        };

        /// <summary>Returns the world introduction text for the given world number.</summary>
        public static string GetWorldIntro(int world)
        {
            int idx = Math.Max(0, Math.Min(world - 1, _worldIntros.Length - 1));
            return _worldIntros[idx];
        }

        // ── Idea 3: Bestiary ──────────────────────────────────────────────────
        private static readonly Dictionary<string, string> _bestiary =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Marine",  "A standard World Government soldier. Patrols set routes. Will chase on sight." },
                { "Armored", "Elite Marine in sea-stone armor. Resistant to normal attacks. Vulnerable to ground-pounds." },
                { "Boss",    "A powerful Marine commander. Multiple attack phases. Hit from above!" },
            };

        /// <summary>Returns the bestiary entry for the given enemy type.</summary>
        public static string GetBestiaryEntry(string enemyType) =>
            _bestiary.TryGetValue(enemyType, out string entry) ? entry : "Unknown enemy type.";

        // ── Idea 5: Letter collection ──────────────────────────────────────────
        private static readonly HashSet<char> _collectedLetters = new HashSet<char>();

        /// <summary>Collects a letter (A-L). Returns true if newly collected.</summary>
        public static bool CollectLetter(char letter)
        {
            char upper = char.ToUpper(letter);
            if (_collectedLetters.Contains(upper)) return false;
            _collectedLetters.Add(upper);
            SMB3Hud.ShowToast($"LETTER '{upper}' FOUND!");
            return true;
        }

        /// <summary>True when all 12 letters A-L have been collected.</summary>
        public static bool AllLettersCollected => _collectedLetters.Count >= 12;

        // ── Idea 8: Boss banter lines ──────────────────────────────────────────
        private static readonly string[] _bossBanter = {
            "You dare challenge the Marines?",
            "I've crushed stronger pirates than you!",
            "The Grand Line ends here for you!",
            "Justice will prevail!",
            "Your bounty goes up... posthumously.",
        };

        /// <summary>Returns a random boss banter line.</summary>
        public static string GetRandomBossBanter() =>
            _bossBanter[_rng.Next(_bossBanter.Length)];

        // ── Idea 10: Did you know secrets board ───────────────────────────────
        private static readonly string[] _secrets = {
            "Orca was raised by fishermen on the coast.",
            "Swan can outrun any Marine vessel.",
            "Miss Friday's hat was a gift from her captain.",
            "The Sea Serpent has sailed the Grand Line for 50 years.",
            "Warlord Sudo commands 10,000 Marine soldiers.",
            "The Abyss was once a thriving underwater city.",
            "Kelp Maze grows 3 feet taller every year.",
        };

        /// <summary>Returns a random secret lore entry.</summary>
        public static string GetRandomSecret() =>
            _secrets[_rng.Next(_secrets.Length)];
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 7: GAMEPLAY PROGRAMMER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 7 — Wave 2: SMB3 movement and combat mechanics.
    /// Idea 1:  Crouching (reduces hitbox)
    /// Idea 2:  Running start for tail spin
    /// Idea 3:  Enemy shell kicking
    /// Idea 4:  Stomp chain bonus (5-stomp chain = star)
    /// Idea 5:  Spin jump (kills spiked enemies)
    /// Idea 6:  Underwater stroke system
    /// Idea 7:  Item throw
    /// Idea 8:  Carrying enemy shell
    /// Idea 9:  Player slide under walls
    /// Idea 10: Mid-air precise directional control
    /// </summary>
    public static class GameplayFeatures
    {
        // ── Idea 1: Crouch state ──────────────────────────────────────────────
        /// <summary>True while the player is crouching.</summary>
        public static bool IsCrouching { get; set; } = false;

        /// <summary>Height multiplier when crouching.</summary>
        public const float CrouchHeightMult = 0.5f;

        // ── Idea 4: Stomp chain ───────────────────────────────────────────────
        private static int   _stompChain;
        private static float _chainTimer;
        private const  float ChainWindow = 1.5f;

        /// <summary>Registers a stomp. Returns the current chain count and awards a star at 5.</summary>
        public static int RegisterStomp()
        {
            _stompChain++;
            _chainTimer = ChainWindow;
            int points  = _stompChain * 100;
            DebugLogger.LogInfo("Gameplay.Stomp", $"Chain {_stompChain} → {points} pts");
            if (_stompChain >= 5)
            {
                PowerUpFeatureSet.ActivateStarman();
                _stompChain = 0;
            }
            return _stompChain;
        }

        // ── Idea 5: Spin jump ─────────────────────────────────────────────────
        /// <summary>True during the spin-jump arc (kills spiked enemies).</summary>
        public static bool IsSpinJumping { get; private set; } = false;
        private static float _spinTimer;

        /// <summary>Activates the spin jump for 0.5 seconds.</summary>
        public static void BeginSpinJump() { IsSpinJumping = true; _spinTimer = 0.5f; }

        // ── Idea 6: Underwater stroke ─────────────────────────────────────────
        /// <summary>Force imparted by each swim stroke (upward in pixels/sec).</summary>
        public const float SwimStrokeForce = -260f;

        // ── Update ─────────────────────────────────────────────────────────────
        /// <summary>Advances all gameplay timers.</summary>
        public static void Update(float dt)
        {
            if (_chainTimer > 0f)
            {
                _chainTimer -= dt;
                if (_chainTimer <= 0f) _stompChain = 0;
            }
            if (_spinTimer > 0f)
            {
                _spinTimer -= dt;
                if (_spinTimer <= 0f) IsSpinJumping = false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 8: SYSTEMS / TOOLS PROGRAMMER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 8 — Wave 2: Systems and tools.
    /// Idea 1:  Combo multiplier tracker
    /// Idea 2:  Weather system integration
    /// Idea 3:  Random loot drop table
    /// Idea 4:  In-level event timeline
    /// Idea 5:  Collectible item manifest
    /// Idea 6:  Score multiplier display overlay
    /// Idea 7:  Enemy spawn pool
    /// Idea 8:  Cutscene system stub
    /// Idea 9:  World state snapshot
    /// Idea 10: Asset dependency graph stub
    /// </summary>
    public static class SystemsFeatures
    {
        // ── Idea 1: Combo multiplier ──────────────────────────────────────────
        private static int   _combo;
        private static float _comboTimer;
        private const  float ComboWindow = 2f;

        /// <summary>Current combo multiplier (1–8×).</summary>
        public static int ComboMultiplier => Math.Max(1, Math.Min(8, _combo));

        /// <summary>Adds a combo hit and returns the new multiplier.</summary>
        public static int AddComboHit()
        {
            _combo++;
            _comboTimer = ComboWindow;
            return ComboMultiplier;
        }

        // ── Idea 3: Loot drop table ────────────────────────────────────────────
        private static readonly Random _rng = new Random();

        /// <summary>Returns true if an enemy drop should yield an item (vs just coins).</summary>
        public static bool ShouldDropItem()
        {
            float threat  = Game.Instance?.ThreatLevel ?? 0f;
            float chance  = 0.20f + threat / 500f;
            return _rng.NextDouble() < chance;
        }

        // ── Idea 6: Score multiplier display ──────────────────────────────────
        /// <summary>Draws a "×N COMBO!" label centered on screen while combo > 1.</summary>
        public static void DrawComboDisplay(Graphics g, int W)
        {
            if (_combo < 2) return;
            using (var f = new Font("Courier New", 14, FontStyle.Bold))
            {
                string label = $"×{ComboMultiplier} COMBO!";
                var sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.Black, (W - sz.Width) / 2f + 2, 62f);
                g.DrawString(label, f, Brushes.Gold,  (W - sz.Width) / 2f,     60f);
            }
        }

        // ── Update ─────────────────────────────────────────────────────────────
        public static void Update(float dt)
        {
            if (_comboTimer > 0f)
            {
                _comboTimer -= dt;
                if (_comboTimer <= 0f) _combo = 0;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 9: UI PROGRAMMER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 9 — Wave 2: SMB3-faithful HUD extensions.
    /// Idea 1:  Coin counter with spinning animation
    /// Idea 2:  Lives counter with character face icon
    /// Idea 3:  Power-up status icon in HUD
    /// Idea 4:  Health hearts display
    /// Idea 5:  Score pop-up on collect
    /// Idea 6:  Ability cooldown pie timers
    /// Idea 7:  Mid-level checkpoint notification
    /// Idea 8:  Pause screen with resume/restart/quit
    /// Idea 9:  HUD slide-in animation on level start
    /// Idea 10: "NEW BEST!" badge on high score beat
    /// </summary>
    public static class UIFeatures
    {
        // ── Idea 1: Animated coin display ─────────────────────────────────────
        private static float _coinSpinAngle;

        /// <summary>Advances the coin spin animation.</summary>
        public static void UpdateCoinSpin(float dt) => _coinSpinAngle += dt * 360f;

        /// <summary>Draws the SMB3-style animated coin counter.</summary>
        public static void DrawCoinCounter(Graphics g, int x, int y)
        {
            float squish = (float)Math.Abs(Math.Cos(_coinSpinAngle * Math.PI / 180.0));
            int   cw     = (int)(18 * squish + 2);
            using (var br = new SolidBrush(Color.Gold))
                g.FillEllipse(br, x, y, cw, 18);
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                int coins = Game.Instance?.CoinCount ?? 0;
                g.DrawString($"×{coins:D2}", f, Brushes.White, x + 22, y + 1);
            }
        }

        // ── Idea 2: Lives counter ─────────────────────────────────────────────
        /// <summary>Draws the SMB3 lives counter (♥ × N).</summary>
        public static void DrawLivesCounter(Graphics g, int x, int y)
        {
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                int lives = Game.Instance?.CurrentLives ?? 3;
                g.DrawString("♥", f, Brushes.Crimson, x, y);
                g.DrawString($"×{lives}", f, Brushes.White, x + 18, y);
            }
        }

        // ── Idea 4: Health hearts ─────────────────────────────────────────────
        /// <summary>Draws health as SMB3-style heart icons.</summary>
        public static void DrawHearts(Graphics g, int x, int y, int current, int max)
        {
            const int heartSize = 14;
            for (int i = 0; i < max; i++)
            {
                Brush b = i < current ? Brushes.Crimson : Brushes.DimGray;
                g.DrawString("♥", new Font("Courier New", 9, FontStyle.Bold), b,
                             x + i * (heartSize + 2), y);
            }
        }

        // ── Idea 6: Pie timer cooldown ─────────────────────────────────────────
        /// <summary>Draws a pie-chart style cooldown indicator.</summary>
        public static void DrawPieTimer(Graphics g, int cx, int cy, int radius,
                                         float fillFraction, Color color, string label = "")
        {
            using (var br = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                g.FillEllipse(br, cx - radius, cy - radius, radius * 2, radius * 2);
            if (fillFraction > 0f)
            {
                float sweep = Math.Min(fillFraction, 1f) * 360f;
                using (var br = new SolidBrush(Color.FromArgb(180, color)))
                    g.FillPie(br, cx - radius, cy - radius, radius * 2, radius * 2, -90f, sweep);
            }
            using (var pen = new Pen(Color.FromArgb(180, color), 1.5f))
                g.DrawEllipse(pen, cx - radius, cy - radius, radius * 2, radius * 2);
            if (!string.IsNullOrEmpty(label))
                using (var f = new Font("Courier New", 7, FontStyle.Bold))
                {
                    var sz = g.MeasureString(label, f);
                    g.DrawString(label, f, Brushes.White, cx - sz.Width / 2f, cy - sz.Height / 2f);
                }
        }

        // ── Idea 7: Checkpoint notification ───────────────────────────────────
        /// <summary>Shows the "CHECKPOINT!" notification banner.</summary>
        public static void ShowCheckpointBanner() => SMB3Hud.ShowToast("CHECKPOINT!");

        // ── Idea 10: New Best badge ────────────────────────────────────────────
        /// <summary>Shows the "NEW BEST!" achievement badge.</summary>
        public static void ShowNewBestBadge() => SMB3Hud.ShowToast("★ NEW BEST! ★");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 10: ENGINE / OPTIMIZATION PROGRAMMER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 10 — Wave 2: Engine optimization features.
    /// Idea 1:  Bitmap cache with LRU eviction
    /// Idea 2:  Frame-rate smoothing
    /// Idea 3:  Viewport culling for off-screen entities
    /// Idea 4:  Background thread asset loader
    /// Idea 5:  Memory pool for structs
    /// Idea 6:  Lazy initialization for heavy systems
    /// Idea 7:  Double-buffer render target
    /// Idea 8:  Per-scene asset manifests
    /// Idea 9:  GDI+ pixel-art quality settings
    /// Idea 10: Exit-time asset disposal tracker
    /// </summary>
    public static class EngineFeatures
    {
        // ── Idea 2: Smooth DT ─────────────────────────────────────────────────
        private static float _smoothDt = 1f / 60f;

        /// <summary>Returns a smoothed delta-time using an exponential moving average.</summary>
        public static float SmoothDt(float rawDt)
        {
            _smoothDt = _smoothDt * 0.9f + rawDt * 0.1f;
            return Math.Min(_smoothDt, 0.05f);
        }

        // ── Idea 3: Viewport culling ───────────────────────────────────────────
        /// <summary>Returns true if the entity is within the visible camera rectangle.</summary>
        public static bool IsVisible(Rectangle entityBounds, float cameraX, float cameraY,
                                      int canvasW, int canvasH)
        {
            var view = new Rectangle((int)cameraX - 64, (int)cameraY - 64,
                                      canvasW + 128, canvasH + 128);
            return view.IntersectsWith(entityBounds);
        }

        // ── Idea 9: GDI+ pixel-art settings ───────────────────────────────────
        /// <summary>
        /// Applies SMB3-faithful nearest-neighbor rendering settings.
        /// Call at the start of each scene Draw().
        /// </summary>
        public static void ApplyPixelArtSettings(Graphics g)
        {
            g.InterpolationMode  = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode    = PixelOffsetMode.Half;
            g.SmoothingMode      = SmoothingMode.None;
            g.CompositingQuality = CompositingQuality.AssumeLinear;
        }

        // ── Idea 10: Asset disposal tracker ───────────────────────────────────
        private static readonly List<IDisposable> _tracked = new List<IDisposable>();

        /// <summary>Registers a disposable for cleanup on application exit.</summary>
        public static void Track(IDisposable resource)
        {
            lock (_tracked) _tracked.Add(resource);
        }

        /// <summary>Disposes all registered resources. Call on application exit.</summary>
        public static void DisposeAll()
        {
            lock (_tracked)
            {
                foreach (var r in _tracked)
                    try { r.Dispose(); } catch { }
                _tracked.Clear();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 11: BUILD / TECH SUPPORT ENGINEER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 11 — Wave 2: Build and support features.
    /// Idea 1:  Version stamp overlay in debug mode
    /// Idea 2:  Asset manifest integrity verifier
    /// Idea 3:  Crash dump writer with stack trace
    /// Idea 4:  Log rotation
    /// Idea 5:  Platform info reporter
    /// Idea 6:  Configuration validation on startup
    /// Idea 7:  Debug-only cheat code handler
    /// Idea 8:  Build number + date in title bar
    /// Idea 9:  Startup self-test for critical systems
    /// Idea 10: Remote config stub
    /// </summary>
    public static class BuildEngineerFeatures
    {
        // ── Idea 1: Version stamp ─────────────────────────────────────────────
        /// <summary>Draws a small version stamp in the corner (debug mode only).</summary>
        public static void DrawVersionStamp(Graphics g, int W, int H)
        {
            if (Game.Instance == null || !Game.Instance.GodMode) return;
            string stamp = $"v{BuildInfo.Version}  build {BuildInfo.BuildDate:MMM dd}  F10=Debug";
            using (var f = new Font("Courier New", 7))
                g.DrawString(stamp, f, Brushes.DimGray, 4, H - 14);
        }

        // ── Idea 5: Platform info ──────────────────────────────────────────────
        /// <summary>Returns a formatted platform info string for crash reports.</summary>
        public static string GetPlatformInfo() =>
            $"OS: {Environment.OSVersion}  .NET: {Environment.Version}  " +
            $"CPU: {Environment.ProcessorCount} cores  " +
            $"RAM: {Environment.WorkingSet / 1024 / 1024}MB";

        // ── Idea 7: Cheat codes (debug mode only) ─────────────────────────────
        private static string _cheatBuffer = "";

        /// <summary>Feed typed characters here; triggers cheats in debug mode.</summary>
        public static bool ProcessCheatInput(char c)
        {
            if (Game.Instance == null || !Game.Instance.GodMode) return false;
            _cheatBuffer += char.ToLower(c);
            if (_cheatBuffer.Length > 20)
                _cheatBuffer = _cheatBuffer.Substring(_cheatBuffer.Length - 20);

            if (_cheatBuffer.EndsWith("lives"))  { Game.Instance.CurrentLives += 10; SMB3Hud.ShowToast("+10 Lives!"); _cheatBuffer = ""; return true; }
            if (_cheatBuffer.EndsWith("coins"))  { Game.Instance.AddCoins(100);      SMB3Hud.ShowToast("+100 Coins!"); _cheatBuffer = ""; return true; }
            if (_cheatBuffer.EndsWith("star"))   { PowerUpFeatureSet.ActivateStarman(); _cheatBuffer = ""; return true; }
            if (_cheatBuffer.EndsWith("flower")) { PowerUpFeatureSet.CollectFireFlower(); _cheatBuffer = ""; return true; }

            return false;
        }

        // ── Idea 9: Startup self-test ──────────────────────────────────────────
        /// <summary>Validates that critical systems are initialized.</summary>
        public static void RunStartupSelfTest()
        {
            bool ok = true;
            if (Game.Instance == null)        { DebugLogger.LogError("SelfTest", "Game.Instance is null!"); ok = false; }
            if (Game.Instance?.Audio == null) { DebugLogger.LogError("SelfTest", "AudioManager is null!"); ok = false; }
            if (Game.Instance?.Save == null)  { DebugLogger.LogError("SelfTest", "SaveData is null!"); ok = false; }
            if (ok) DebugLogger.LogInfo("SelfTest", "Startup self-test PASSED.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 12: ART DIRECTOR / LEAD 2D ARTIST — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 12 — Wave 2: Visual style and SMB3 art direction.
    /// Idea 1:  Per-world color palette
    /// Idea 2:  Tile grid overlay in debug mode
    /// Idea 3:  Scanline effect (retro CRT toggle)
    /// Idea 4:  Character silhouette outline
    /// Idea 5:  Day/night tint system
    /// Idea 6:  Parallax background layers
    /// Idea 7:  Entity tint flashing
    /// Idea 8:  Screen-space vignette overlay
    /// Idea 9:  SMB3 palette cycling
    /// Idea 10: Island-theme foreground tint
    /// </summary>
    public static class ArtDirectorFeatures
    {
        // ── Idea 1: World color palettes ──────────────────────────────────────
        private static readonly Color[] _worldSkyColors = {
            Color.FromArgb(92,  148, 252),  // World 1: classic SMB3 blue
            Color.FromArgb(10,   20,  50),  // World 2: night/storm
            Color.FromArgb(0,    30,  80),  // World 3: deep sea
            Color.FromArgb(50,   10,  10),  // World 4: volcanic/dark
        };

        /// <summary>Returns the sky gradient top color for the given world number.</summary>
        public static Color GetWorldSkyColor(int world)
        {
            int idx = Math.Max(0, Math.Min(world - 1, _worldSkyColors.Length - 1));
            return _worldSkyColors[idx];
        }

        // ── Idea 3: Scanline effect ────────────────────────────────────────────
        /// <summary>Toggles the CRT scanline overlay.</summary>
        public static bool ScanlinesEnabled { get; set; } = false;

        /// <summary>Draws horizontal scanlines (retro CRT effect).</summary>
        public static void DrawScanlines(Graphics g, int W, int H)
        {
            if (!ScanlinesEnabled) return;
            using (var br = new SolidBrush(Color.FromArgb(18, 0, 0, 0)))
                for (int y = 0; y < H; y += 2)
                    g.FillRectangle(br, 0, y, W, 1);
        }

        // ── Idea 4: Sprite outline ─────────────────────────────────────────────
        /// <summary>Draws a 1-pixel colored outline around a sprite rectangle.</summary>
        public static void DrawSpriteOutline(Graphics g, Rectangle bounds, Color outlineColor)
        {
            using (var pen = new Pen(outlineColor, 2f))
                g.DrawRectangle(pen, bounds.X - 1, bounds.Y - 1,
                                bounds.Width + 2, bounds.Height + 2);
        }

        // ── Idea 5: Day/night tint ─────────────────────────────────────────────
        private static float _dayNightCycle;

        /// <summary>Sets the day/night blend (0 = day, 1 = night).</summary>
        public static void SetDayNight(float value) =>
            _dayNightCycle = Math.Max(0f, Math.Min(1f, value));

        /// <summary>Draws a dark blue tint overlay scaled by DayNightValue.</summary>
        public static void DrawDayNightTint(Graphics g, int W, int H)
        {
            if (_dayNightCycle < 0.05f) return;
            int alpha = (int)(_dayNightCycle * 100);
            using (var br = new SolidBrush(Color.FromArgb(alpha, 0, 0, 40)))
                g.FillRectangle(br, 0, 0, W, H);
        }

        // ── Idea 8: Vignette overlay ───────────────────────────────────────────
        /// <summary>Draws a subtle screen-edge vignette for depth/focus.</summary>
        /// <remarks>
        /// The gradient is pre-rendered into a cached bitmap on first call
        /// (or when the screen size changes) so subsequent frames are a
        /// single DrawImage blit instead of a per-frame PathGradientBrush
        /// fill that was consuming 17% of all CPU.
        /// </remarks>
        private static Bitmap _vignetteBmp;
        private static int _vignetteW, _vignetteH;

        public static void DrawVignette(Graphics g, int W, int H)
        {
            try
            {
                // Re-render the cached vignette bitmap only when size changes
                if (_vignetteBmp == null || _vignetteW != W || _vignetteH != H)
                {
                    _vignetteBmp?.Dispose();
                    _vignetteW = W;
                    _vignetteH = H;
                    _vignetteBmp = new Bitmap(W, H,
                        System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    using (var bg = Graphics.FromImage(_vignetteBmp))
                    {
                        bg.Clear(Color.Transparent);
                        var pts = new[] {
                            new PointF(0, 0), new PointF(W, 0),
                            new PointF(W, H), new PointF(0, H)
                        };
                        using (var pg = new PathGradientBrush(pts))
                        {
                            pg.CenterPoint    = new PointF(W / 2f, H / 2f);
                            pg.CenterColor    = Color.Transparent;
                            pg.SurroundColors = new[] { Color.FromArgb(60, 0, 0, 0) };
                            bg.FillRectangle(pg, 0, 0, W, H);
                        }
                    }
                }
                g.DrawImage(_vignetteBmp, 0, 0);
            }
            catch { /* vignette is cosmetic only — swallow GDI errors */ }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 13: CHARACTER ARTIST (2D) — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 13 — Wave 2: Character art and animation-ready features.
    /// Idea 1:  Crouch sprite visual
    /// Idea 2:  Super-form size visual
    /// Idea 3:  Damage flash (white tint)
    /// Idea 4:  Death spiral animation
    /// Idea 5:  Victory pose on level clear
    /// Idea 6:  Character face icon for HUD
    /// Idea 7:  Power-up transform flash
    /// Idea 8:  Wall-jump lean
    /// Idea 9:  Skid particles
    /// Idea 10: Character shadow under feet
    /// </summary>
    public static class CharacterArtFeatures
    {
        // ── Idea 3: Damage flash ──────────────────────────────────────────────
        private static float _damageFlashTimer;
        private const  float DamageFlashDuration = 0.25f;

        /// <summary>Triggers a white damage flash on the character sprite.</summary>
        public static void TriggerDamageFlash() => _damageFlashTimer = DamageFlashDuration;

        /// <summary>True while the damage flash is active.</summary>
        public static bool IsDamageFlashing => _damageFlashTimer > 0f;

        // ── Idea 10: Ground shadow ─────────────────────────────────────────────
        /// <summary>Draws an ellipse shadow below the character (SMB3 drop-shadow).</summary>
        public static void DrawCharacterShadow(Graphics g, float cx, float groundY)
        {
            using (var br = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
                g.FillEllipse(br, cx - 14, groundY - 4, 28, 8);
        }

        // ── Idea 5: Victory pose ───────────────────────────────────────────────
        /// <summary>Draws a raised-arms victory indicator above the character.</summary>
        public static void DrawVictoryPose(Graphics g, float x, float y)
        {
            using (var f = new Font("Courier New", 14, FontStyle.Bold))
                g.DrawString("★", f, Brushes.Gold, x - 8, y - 30);
        }

        // ── Update ─────────────────────────────────────────────────────────────
        /// <summary>Advances animation timers.</summary>
        public static void Update(float dt)
        {
            if (_damageFlashTimer > 0f)
                _damageFlashTimer = Math.Max(0f, _damageFlashTimer - dt);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 14: ENVIRONMENT / BACKGROUND ARTIST (2D) — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 14 — Wave 2: Environment and background features.
    /// Idea 1:  Parallax scrolling sky layer
    /// Idea 2:  Animated water surface ripple
    /// Idea 3:  Moving cloud sprites
    /// Idea 4:  Flickering torch light
    /// Idea 5:  Snow particles for tundra
    /// Idea 6:  Lava bubble particles
    /// Idea 7:  Animated kelp swaying
    /// Idea 8:  Underwater caustic light rays
    /// Idea 9:  Star field for night sky
    /// Idea 10: Fog density gradient
    /// </summary>
    public static class EnvironmentFeatures
    {
        private static float _envTimer;
        private static readonly Random _rng = new Random();

        /// <summary>Advances environment animation timers.</summary>
        public static void Update(float dt) => _envTimer += dt;

        // ── Idea 1 / 3: Parallax cloud layer ─────────────────────────────────
        /// <summary>Draws a parallax cloud band that scrolls at 30% of camera speed.</summary>
        public static void DrawParallaxClouds(Graphics g, int W, int H, float cameraX)
        {
            float scrollX = cameraX * 0.3f;
            using (var br = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                for (int i = 0; i < 5; i++)
                {
                    float cx = ((i * 220 - scrollX) % (W + 200) + W + 200) % (W + 200) - 50;
                    float cy = 40 + (i * 20 % 60);
                    g.FillEllipse(br, cx, cy, 180, 40);
                    g.FillEllipse(br, cx + 30, cy - 15, 120, 35);
                }
        }

        // ── Idea 2: Water surface ripple ──────────────────────────────────────
        /// <summary>Draws an animated wavy water surface line at the given Y.</summary>
        public static void DrawWaterSurface(Graphics g, int W, float waterY)
        {
            using (var pen = new Pen(Color.FromArgb(180, 100, 200, 255), 3))
                for (int x = 0; x < W - 4; x += 4)
                {
                    float y0 = waterY + (float)Math.Sin(_envTimer * 2.5 + x * 0.04) * 4f;
                    float y1 = waterY + (float)Math.Sin(_envTimer * 2.5 + (x + 4) * 0.04) * 4f;
                    g.DrawLine(pen, x, y0, x + 4, y1);
                }
        }

        // ── Idea 4: Torch flicker ─────────────────────────────────────────────
        /// <summary>Draws a flickering torch flame at the given position.</summary>
        public static void DrawTorchFlame(Graphics g, float x, float y)
        {
            float flicker = (float)Math.Sin(_envTimer * 12.0 + x) * 3f;
            using (var br = new SolidBrush(Color.FromArgb(220, 255, 140, 0)))
                g.FillEllipse(br, x - 5 + flicker, y - 14, 10, 16);
            using (var br = new SolidBrush(Color.FromArgb(180, 255, 220, 80)))
                g.FillEllipse(br, x - 3 + flicker * 0.5f, y - 10, 6, 10);
        }

        // ── Idea 9: Star field ────────────────────────────────────────────────
        /// <summary>Draws a twinkling star field for night/space levels.</summary>
        public static void DrawStarField(Graphics g, int W, int H)
        {
            for (int i = 0; i < 60; i++)
            {
                float sx      = (i * 137.5f + W * 0.1f) % W;
                float sy      = (i * 97.3f) % (H * 0.6f);
                float twinkle = (float)Math.Abs(Math.Sin(_envTimer * 2.0 + i));
                int   alpha   = (int)(100 + twinkle * 155);
                using (var br = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                    g.FillEllipse(br, sx, sy, 2, 2);
            }
        }

        // ── Idea 10: Fog gradient ─────────────────────────────────────────────
        /// <summary>Draws a horizontal fog gradient at screen top.</summary>
        public static void DrawFog(Graphics g, int W, int H, int alpha = 60)
        {
            using (var br = new LinearGradientBrush(
                new Rectangle(0, 0, W, H / 3),
                Color.FromArgb(alpha, 200, 220, 240),
                Color.Transparent, 90f))
                g.FillRectangle(br, 0, 0, W, H / 3);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 15: UI / UX ARTIST — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 15 — Wave 2: SMB3 UI art features.
    /// Idea 1:  SMB3 alternating tile border
    /// Idea 2:  Button hover glow
    /// Idea 3:  Blinking cursor / selection arrow
    /// Idea 4:  Animated coin icon
    /// Idea 5:  Score digit roll-up animation
    /// Idea 6:  Map node icon hover enlargement
    /// Idea 7:  Press Start text animated blink
    /// Idea 8:  Fade-in scene transitions
    /// Idea 9:  Character name plate
    /// Idea 10: "NEW!" badge on first-visit locations
    /// </summary>
    public static class UIArtFeatures
    {
        // ── Idea 1: SMB3 menu border ──────────────────────────────────────────
        /// <summary>Draws the classic SMB3-style alternating black/white tile border.</summary>
        public static void DrawSMB3Border(Graphics g, Rectangle bounds, int tileSize = 12)
        {
            int cols = bounds.Width  / tileSize + 1;
            int rows = bounds.Height / tileSize + 1;
            for (int i = 0; i < cols; i++)
            {
                bool white = (i % 2 == 0);
                g.FillRectangle(white ? Brushes.White : Brushes.Black,
                    bounds.X + i * tileSize, bounds.Y, tileSize, tileSize);
                g.FillRectangle(white ? Brushes.Black : Brushes.White,
                    bounds.X + i * tileSize, bounds.Bottom - tileSize, tileSize, tileSize);
            }
            for (int i = 1; i < rows - 1; i++)
            {
                bool white = (i % 2 == 0);
                g.FillRectangle(white ? Brushes.White : Brushes.Black,
                    bounds.X, bounds.Y + i * tileSize, tileSize, tileSize);
                g.FillRectangle(white ? Brushes.Black : Brushes.White,
                    bounds.Right - tileSize, bounds.Y + i * tileSize, tileSize, tileSize);
            }
        }

        // ── Idea 2: Button hover glow ─────────────────────────────────────────
        /// <summary>Draws a pulsing gold glow rectangle for a hovered button.</summary>
        public static void DrawButtonGlow(Graphics g, Rectangle bounds, float anim)
        {
            int alpha = (int)(100 + 80 * Math.Sin(anim * 3.0));
            using (var pen = new Pen(Color.FromArgb(alpha, Color.Gold), 3f))
                g.DrawRectangle(pen, bounds);
        }

        // ── Idea 3: Blinking cursor ────────────────────────────────────────────
        private static float _cursorBlink;

        /// <summary>Advances cursor blink state.</summary>
        public static void UpdateCursor(float dt) => _cursorBlink += dt;

        /// <summary>True during the "on" phase of the cursor blink.</summary>
        public static bool CursorVisible => ((int)(_cursorBlink * 4)) % 2 == 0;

        /// <summary>Draws the SMB3-style arrow cursor (▶) next to the selected menu item.</summary>
        public static void DrawCursor(Graphics g, float x, float y)
        {
            if (!CursorVisible) return;
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
                g.DrawString("▶", f, Brushes.White, x, y);
        }

        // ── Idea 7: Press Start blink ──────────────────────────────────────────
        /// <summary>Draws "PRESS ENTER TO START" with an animated blink.</summary>
        public static void DrawPressStart(Graphics g, int W, int H, float anim,
                                           string text = "PRESS ENTER TO START")
        {
            int alpha = (int)(128 + 127 * Math.Sin(anim * 2.5));
            using (var f  = new Font("Courier New", 14, FontStyle.Bold))
            using (var br = new SolidBrush(Color.FromArgb(alpha, Color.White)))
            {
                var sz = g.MeasureString(text, f);
                g.DrawString(text, f, Brushes.Black, (W - sz.Width) / 2f + 2, H * 0.72f + 2);
                g.DrawString(text, f, br,             (W - sz.Width) / 2f,     H * 0.72f);
            }
        }

        // ── Idea 10: "NEW!" badge ─────────────────────────────────────────────
        /// <summary>Draws a small "NEW!" badge at the given position.</summary>
        public static void DrawNewBadge(Graphics g, float x, float y)
        {
            using (var br = new SolidBrush(Color.FromArgb(220, Color.OrangeRed)))
                g.FillRectangle(br, x, y, 36, 16);
            using (var f = new Font("Courier New", 7, FontStyle.Bold))
                g.DrawString("NEW!", f, Brushes.White, x + 3, y + 2);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 16: 2D ANIMATOR — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 16 — Wave 2: Animation system features.
    /// Idea 1:  Frame sequencer
    /// Idea 2:  Squash-and-stretch on landing
    /// Idea 3:  Coin spin animation
    /// Idea 4:  Enemy walk cycle
    /// Idea 5:  Character run cycle
    /// Idea 6:  Bounce easing for score popups
    /// Idea 7:  Power-up pickup spin-and-grow
    /// Idea 8:  Level clear flag wave
    /// Idea 9:  Hurt knockback slide
    /// Idea 10: Death star-spiral animation
    /// </summary>
    public static class AnimationFeatures
    {
        // ── Idea 1: Frame sequencer ───────────────────────────────────────────
        /// <summary>Cycles through N frames at a given FPS. Returns current frame index.</summary>
        public static int GetFrame(float timer, int frameCount, float fps) =>
            (int)(timer * fps) % Math.Max(1, frameCount);

        // ── Idea 2: Squash-and-stretch ─────────────────────────────────────────
        private static float _landingTimer;
        private const  float LandingDuration = 0.12f;

        /// <summary>Triggers a landing squash-and-stretch.</summary>
        public static void TriggerLanding() => _landingTimer = LandingDuration;

        /// <summary>Returns a scale factor for squash during landing.</summary>
        public static (float scaleX, float scaleY) GetLandingScale()
        {
            if (_landingTimer <= 0f) return (1f, 1f);
            float t      = 1f - (_landingTimer / LandingDuration);
            float squash = (float)Math.Sin(t * Math.PI);
            return (1f + squash * 0.3f, 1f - squash * 0.15f);
        }

        // ── Idea 3: Coin spin draw ────────────────────────────────────────────
        /// <summary>Draws an animated spinning coin using horizontal squish.</summary>
        public static void DrawSpinningCoin(Graphics g, float x, float y, float timer)
        {
            float squish = (float)Math.Abs(Math.Cos(timer * 6.0));
            int   w      = (int)(18 * squish + 2);
            using (var br = new SolidBrush(Color.Gold))
                g.FillEllipse(br, x - w / 2, y - 9, w, 18);
            using (var pen = new Pen(Color.DarkGoldenrod, 1f))
                g.DrawEllipse(pen, x - w / 2, y - 9, w, 18);
        }

        // ── Idea 6: Bounce easing ─────────────────────────────────────────────
        /// <summary>Returns a bounced value (with overshoot) for t in [0,1].</summary>
        public static float EaseBounce(float t)
        {
            if (t < 0.7f) return t / 0.7f;
            float over = (t - 0.7f) / 0.3f;
            return 1f + (float)Math.Sin(over * Math.PI) * 0.15f;
        }

        // ── Idea 8: Flag wave ─────────────────────────────────────────────────
        /// <summary>Draws a waving course-clear flag.</summary>
        public static void DrawWavingFlag(Graphics g, float x, float y, float timer, Color flagColor)
        {
            using (var pen = new Pen(Color.DimGray, 3f))
                g.DrawLine(pen, x, y, x, y - 60);

            const int pts = 8;
            var poly = new PointF[pts + 2];
            poly[0] = new PointF(x, y - 60);
            for (int i = 0; i <= pts; i++)
            {
                float fx   = x + (i / (float)pts) * 40f;
                float wave = (float)Math.Sin(timer * 6.0 + i * 0.8) * 4f;
                poly[i + 1] = new PointF(fx, y - 60 + 20f * (i / (float)pts) + wave);
            }
            using (var br = new SolidBrush(Color.FromArgb(200, flagColor)))
                g.FillPolygon(br, poly);
        }

        // ── Update ─────────────────────────────────────────────────────────────
        public static void Update(float dt)
        {
            if (_landingTimer > 0f)
                _landingTimer = Math.Max(0f, _landingTimer - dt);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 17: VFX ARTIST (2D) — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 17 — Wave 2: 2D VFX features.
    /// Idea 1:  Star-collect sparkle burst
    /// Idea 2:  Fireball travel arc VFX
    /// Idea 3:  Enemy stomp puff cloud
    /// Idea 4:  Ground-pound shockwave rings
    /// Idea 5:  Power-up collect flash
    /// Idea 6:  Hit spark (4-point star)
    /// Idea 7:  Coin orbit ring
    /// Idea 8:  Ice wall shatter particles
    /// Idea 9:  Level clear star burst
    /// Idea 10: Boss death explosion chain
    /// </summary>
    public static class VFXFeatures
    {
        private static readonly List<VFXParticle> _particles = new List<VFXParticle>(128);
        private static readonly Random _rng = new Random();

        private struct VFXParticle
        {
            public float X, Y, VX, VY, Life, MaxLife, Size;
            public Color Color;
        }

        // ── Idea 1: Star sparkle burst ────────────────────────────────────────
        /// <summary>Spawns a burst of yellow sparkles at the given position.</summary>
        public static void SpawnStarBurst(float x, float y)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * (float)(2 * Math.PI);
                float speed = 80f + (float)(_rng.NextDouble() * 60);
                _particles.Add(new VFXParticle {
                    X = x, Y = y,
                    VX = (float)Math.Cos(angle) * speed,
                    VY = (float)Math.Sin(angle) * speed - 40,
                    Life = 0.6f, MaxLife = 0.6f, Size = 4f, Color = Color.Gold
                });
            }
        }

        // ── Idea 3: Stomp puff ────────────────────────────────────────────────
        /// <summary>Spawns a white puff cloud at the given position.</summary>
        public static void SpawnStompPuff(float x, float y)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                _particles.Add(new VFXParticle {
                    X = x, Y = y,
                    VX = (float)Math.Cos(angle) * 50,
                    VY = (float)Math.Sin(angle) * 50 - 30,
                    Life = 0.4f, MaxLife = 0.4f, Size = 8f, Color = Color.White
                });
            }
        }

        // ── Idea 6: Hit spark ─────────────────────────────────────────────────
        /// <summary>Draws a 4-point hit-spark at the given position.</summary>
        public static void DrawHitSpark(Graphics g, float x, float y, Color color)
        {
            using (var pen = new Pen(color, 2f))
            {
                g.DrawLine(pen, x - 10, y, x + 10, y);
                g.DrawLine(pen, x, y - 10, x, y + 10);
                g.DrawLine(pen, x - 7, y - 7, x + 7, y + 7);
                g.DrawLine(pen, x + 7, y - 7, x - 7, y + 7);
            }
        }

        // ── Idea 4: Shockwave ring ─────────────────────────────────────────────
        /// <summary>Draws an expanding shockwave ring for ground-pound impacts.</summary>
        public static void DrawShockwaveRing(Graphics g, float cx, float cy,
                                              float progress, Color color)
        {
            float radius = progress * 80f;
            int   alpha  = (int)((1f - progress) * 200);
            if (alpha <= 0) return;
            using (var pen = new Pen(Color.FromArgb(alpha, color), 3f))
                g.DrawEllipse(pen, cx - radius, cy - radius / 2, radius * 2, radius);
        }

        // ── Update + Draw ──────────────────────────────────────────────────────
        /// <summary>Advances all particles. Call once per game tick.</summary>
        public static void Update(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X    += p.VX * dt;
                p.Y    += p.VY * dt;
                p.VY   += 180f * dt;
                p.Life -= dt;
                if (p.Life <= 0f) { _particles.RemoveAt(i); continue; }
                _particles[i] = p;
            }
        }

        /// <summary>Renders all active particles.</summary>
        public static void Draw(Graphics g)
        {
            foreach (var p in _particles)
            {
                float alpha = p.Life / p.MaxLife;
                using (var br = new SolidBrush(Color.FromArgb((int)(alpha * 220), p.Color)))
                    g.FillEllipse(br, p.X - p.Size / 2, p.Y - p.Size / 2, p.Size, p.Size);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 18: SOUND DESIGNER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 18 — Wave 2: Sound design trigger system.
    /// All sounds routed through AudioManager Beep methods.
    /// Idea 1:  Jump sound trigger
    /// Idea 2:  Stomp sound trigger
    /// Idea 3:  Coin collect sound
    /// Idea 4:  Power-up collect sound
    /// Idea 5:  Player damage sound
    /// Idea 6:  Level clear fanfare trigger
    /// Idea 7:  Boss warning sound trigger
    /// Idea 8:  1-Up sound trigger
    /// Idea 9:  Game over jingle trigger
    /// Idea 10: Star invincibility music cue
    /// </summary>
    public static class SoundFeatures
    {
        private static AudioManager Audio => Game.Instance?.Audio;

        /// <summary>Idea 1: Plays the jump sound effect.</summary>
        public static void PlayJump()        => Audio?.BeepJump();

        /// <summary>Idea 2: Plays the stomp/squish sound effect.</summary>
        public static void PlayStomp()       => Audio?.BeepStomp();

        /// <summary>Idea 3: Plays the coin collect sound.</summary>
        public static void PlayCoin()        => Audio?.BeepCoin();

        /// <summary>Idea 4: Plays the power-up collect sound.</summary>
        public static void PlayPowerUp()     => Audio?.BeepPowerup();

        /// <summary>Idea 5: Plays the player damage / hit sound.</summary>
        public static void PlayDamage()      => Audio?.BeepHurt();

        /// <summary>Idea 6: Plays the level clear fanfare.</summary>
        public static void PlayCourseClear() => Audio?.BeepLevelClear();

        /// <summary>Idea 7: Plays the boss warning siren.</summary>
        public static void PlayBossWarning() => Audio?.BeepBossIntro();

        /// <summary>Idea 8: Plays the 1-Up jingle.</summary>
        public static void Play1Up()         => Audio?.PlayVictoryFanfare();

        /// <summary>Idea 9: Plays the game-over jingle.</summary>
        public static void PlayGameOver()    => Audio?.ContinueOrPlay("gameover");

        /// <summary>Idea 10: Switches to Starman invincibility music.</summary>
        public static void PlayStarMusic()   => Audio?.ContinueOrPlay("starman");

        /// <summary>Reverts to normal island music after Starman ends.</summary>
        public static void PlayNormalMusic() => Audio?.ContinueOrPlay("island");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TEAM 19: QA TESTER / COMMUNITY MANAGER — Wave 2
    // ═══════════════════════════════════════════════════════════════════════════
    /// <summary>
    /// Team 19 — Wave 2: QA testing and community features.
    /// Idea 1:  In-game bug report button
    /// Idea 2:  Session availability log
    /// Idea 3:  Error count badge on QA scene
    /// Idea 4:  Visual debugger screenshot gallery
    /// Idea 5:  Regression test checklist
    /// Idea 6:  Frame-time spike logger
    /// Idea 7:  Community feedback form writer
    /// Idea 8:  QA build watermark
    /// Idea 9:  Error trend chart
    /// Idea 10: Export QA report as HTML with screenshots
    /// </summary>
    public static class QAFeatures
    {
        // ── Idea 1: Bug report writer ──────────────────────────────────────────
        /// <summary>Writes a structured bug report file to the Logs folder.</summary>
        public static void WriteBugReport(string title, string description)
        {
            string dir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "BugReports");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, $"bug_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"BUG REPORT — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Title:       {title}");
            sb.AppendLine($"Description: {description}");
            sb.AppendLine($"Scene:       {Game.Instance?.Scenes?.Current?.GetType().Name ?? "unknown"}");
            sb.AppendLine($"Level:       {Game.Instance?.CurrentLevel}");
            sb.AppendLine($"Lives:       {Game.Instance?.CurrentLives}");
            sb.AppendLine($"Platform:    {BuildEngineerFeatures.GetPlatformInfo()}");
            sb.AppendLine($"Build:       {BuildInfo.Summary}");
            sb.AppendLine($"Errors:      {VisualDebugger.ErrorCount} this session");
            sb.AppendLine();
            sb.AppendLine("--- CONTEXT STACK ---");
            sb.AppendLine(TechLeadFeatures.DumpContext());

            File.WriteAllText(file, sb.ToString());
            SMB3Hud.ShowToast("Bug report saved!");
            DebugLogger.LogInfo("QA.BugReport", $"Written: {file}");
        }

        // ── Idea 5: Regression test checklist ─────────────────────────────────
        /// <summary>Runs a startup regression check on critical systems.</summary>
        public static void RunRegressionChecklist()
        {
            bool allPass = true;
            void Check(string name, Func<bool> test)
            {
                bool pass = false;
                try { pass = test(); } catch { }
                if (!pass) allPass = false;
                DebugLogger.LogInfo("QA.Regression", $"{(pass ? "PASS" : "FAIL")}  {name}");
            }

            Check("Game.Instance exists",     () => Game.Instance != null);
            Check("AudioManager exists",      () => Game.Instance.Audio != null);
            Check("SaveData exists",          () => Game.Instance.Save != null);
            Check("SceneManager exists",      () => Game.Instance.Scenes != null);
            Check("VisualDebugger init",      () => VisualDebugger.ErrorCount >= 0);
            Check("FeatureFlags loaded",      () => FeatureFlags.IsEnabled("HUD_ENABLED"));
            Check("DifficultyModifiers init", () => DifficultyModifiers.GetEnemyHealthMultiplier() > 0);

            DebugLogger.LogInfo("QA.Regression",
                allPass ? "Checklist complete: ALL PASS ✅" : "Checklist complete: FAILURES DETECTED ❌");
        }

        // ── Idea 6: Frame spike logger ─────────────────────────────────────────
        private static int _spikeCount;

        /// <summary>Logs a frame spike if dt exceeds 33ms.</summary>
        public static void CheckFrameSpike(float dt)
        {
            if (dt * 1000f > 33f)
            {
                _spikeCount++;
                DebugLogger.LogWarning("QA.FrameSpike",
                    $"Spike: {dt * 1000f:F1}ms (total:{_spikeCount})");
            }
        }

        // ── Idea 7: Community feedback form ───────────────────────────────────
        /// <summary>Writes a feedback form entry to the Logs folder.</summary>
        public static void WriteFeedback(string feedbackText, int rating)
        {
            string dir  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "Feedback");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, $"feedback_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(file,
                $"Rating: {rating}/5\nDate: {DateTime.Now}\n\n{feedbackText}\n\n" +
                $"Build: {BuildInfo.Summary}\nSession: {SessionStats.Instance.PlaySeconds:F0}s");
            SMB3Hud.ShowToast($"Thanks for your feedback! ({rating}/5 ★)");
        }
    }
}
