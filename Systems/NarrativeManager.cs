using System;
using System.Collections.Generic;
using System.Drawing;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Narrative event manager — drives in-game text overlays, mission objectives,
    /// and dialogue triggers tied to level progression.
    ///
    /// Team 6  (Narrative Designer) — Idea 1: level intro text overlay.
    /// Team 6  (Narrative Designer) — Idea 4: mission objective HUD line.
    /// Team 6  (Narrative Designer) — Idea 3: boss defeat story text.
    /// Team 6  (Narrative Designer) — Idea 9: world map area lore text.
    /// Team 1  (Game Director)      — Idea 9: mission briefing overlay.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   NarrativeManager.ShowIntro("Dinosaur Island", "Survive the wild jungle...");
    ///   NarrativeManager.SetObjective("Defeat the boss and reach the exit flag.");
    ///   NarrativeManager.ShowBossDefeat("The Marine Captain has been defeated!");
    ///   NarrativeManager.Update(dt);
    ///   NarrativeManager.Draw(g, W, H);
    /// </summary>
    public static class NarrativeManager
    {
        // ── Intro overlay ──────────────────────────────────────────────────────
        private static string _introTitle;
        private static string _introBody;
        private static float  _introTimer;     // seconds remaining for intro display
        private const  float  IntroDuration = 3.5f;

        // ── Mission objective ──────────────────────────────────────────────────
        private static string _objective = "";

        // ── Boss defeat text ───────────────────────────────────────────────────
        private static string _bossDefeatText;
        private static float  _bossDefeatTimer;
        private const  float  BossDefeatDuration = 4.0f;

        // ── Level world/lore description ───────────────────────────────────────
        private static readonly Dictionary<string, string> _areaLore = new Dictionary<string, string>
        {
            { "dino",         "DINOSAUR ISLAND — Ancient predators roam a jungle frozen in time." },
            { "storm1",       "STORM BELT — Lightning tears through the sea lanes." },
            { "sky",          "SKY ISLAND — Cities float above the clouds, beyond Marine reach." },
            { "wano",         "BLADE NATION — A hidden country of samurai honour and fire." },
            { "blockade",     "MARINE BLOCKADE — The Navy's iron fist surrounds the passage." },
            { "warlord1",     "WARLORD: SUDO — A fire-wielding tyrant guards the New World gate." },
            { "harbor",       "HARBOR TOWN — A trade hub teetering between pirates and law." },
            { "coral",        "CORAL REEF — Breathtaking beauty conceals razor-sharp danger." },
            { "tundra",       "TUNDRA PEAK — Snowstorms drive even the hardiest crews below." },
            { "abyss",        "THE ABYSS — Nothing that descends here is ever found again." },
            { "centipede_final","CENTIPEDE BOSS — The guardian of the deep stirs from its slumber." },
        };

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static Font _titleFont = new Font("Courier New", 16, FontStyle.Bold);
        private static Font _bodyFont  = new Font("Courier New", 11, FontStyle.Regular);
        private static Font _objFont   = new Font("Courier New", 9,  FontStyle.Bold);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Shows a level intro overlay (title + body text) for a fixed duration.
        /// Team 6 (Narrative Designer) — Idea 1.
        /// </summary>
        public static void ShowIntro(string title, string body)
        {
            _introTitle = title;
            _introBody  = body;
            _introTimer = IntroDuration;
        }

        /// <summary>
        /// Shows the intro using the built-in lore table for the given node id.
        /// </summary>
        public static void ShowIntroForNode(string nodeId, string levelName)
        {
            string lore = _areaLore.ContainsKey(nodeId)
                ? _areaLore[nodeId]
                : $"{levelName.ToUpper()} — Adventure awaits beyond the horizon.";
            ShowIntro(levelName, lore);
        }

        /// <summary>
        /// Sets or clears the active mission objective displayed in the HUD.
        /// Team 6 (Narrative Designer) — Idea 4.
        /// </summary>
        public static void SetObjective(string objective) => _objective = objective ?? "";

        /// <summary>
        /// Displays a boss defeat story text banner.
        /// Team 6 (Narrative Designer) — Idea 3.
        /// </summary>
        public static void ShowBossDefeat(string text)
        {
            _bossDefeatText  = text;
            _bossDefeatTimer = BossDefeatDuration;
        }

        // ── Update ─────────────────────────────────────────────────────────────

        /// <summary>Advances all narrative timers.</summary>
        public static void Update(float dt)
        {
            if (_introTimer     > 0f) _introTimer     -= dt;
            if (_bossDefeatTimer > 0f) _bossDefeatTimer -= dt;
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws all active narrative overlays over the current scene.
        /// Call from scene Draw() AFTER all game elements.
        /// </summary>
        public static void Draw(Graphics g, int W, int H)
        {
            DrawIntroOverlay(g, W, H);
            DrawMissionObjective(g, W, H);
            DrawBossDefeatText(g, W, H);
        }

        // ── Draw helpers ───────────────────────────────────────────────────────

        /// <summary>Draws the translucent level intro title card (fades in/out).</summary>
        private static void DrawIntroOverlay(Graphics g, int W, int H)
        {
            if (_introTimer <= 0f || string.IsNullOrEmpty(_introTitle)) return;

            // Fade in/out alpha.
            float t     = _introTimer / IntroDuration;
            float alpha = t > 0.8f ? (1f - t) / 0.2f      // fade in
                        : t < 0.2f ? t / 0.2f              // fade out
                        :            1f;                    // fully visible

            int panelH = 80;
            using (var br = new SolidBrush(Color.FromArgb((int)(200 * alpha), 4, 8, 24)))
                g.FillRectangle(br, 0, H / 3 - panelH / 2, W, panelH);

            using (var f = _titleFont)
            {
                SizeF sz = g.MeasureString(_introTitle, f);
                using (var br = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Gold)))
                    g.DrawString(_introTitle, f, br, (W - sz.Width) / 2f, H / 3 - panelH / 2 + 6);
            }

            if (!string.IsNullOrEmpty(_introBody))
                using (var f = _bodyFont)
                {
                    SizeF sz = g.MeasureString(_introBody, f);
                    using (var br = new SolidBrush(Color.FromArgb((int)(220 * alpha), Color.LightCyan)))
                        g.DrawString(_introBody, f, br, (W - sz.Width) / 2f, H / 3 - panelH / 2 + 40);
                }
        }

        /// <summary>Draws the mission objective in a small strip at the top of the HUD.</summary>
        private static void DrawMissionObjective(Graphics g, int W, int H)
        {
            if (string.IsNullOrEmpty(_objective)) return;
            using (var f = _objFont)
            {
                string label = $"★ OBJECTIVE: {_objective}";
                SizeF  sz    = g.MeasureString(label, f);
                using (var br = new SolidBrush(Color.FromArgb(160, 8, 8, 20)))
                    g.FillRectangle(br, W - (int)sz.Width - 14, 4, (int)sz.Width + 10, 18);
                using (var br = new SolidBrush(Color.FromArgb(200, 220, 220, 80)))
                    g.DrawString(label, f, br, W - sz.Width - 8, 6);
            }
        }

        /// <summary>Draws the boss defeat story text as a centred banner.</summary>
        private static void DrawBossDefeatText(Graphics g, int W, int H)
        {
            if (_bossDefeatTimer <= 0f || string.IsNullOrEmpty(_bossDefeatText)) return;

            float alpha = _bossDefeatTimer < 0.5f ? _bossDefeatTimer / 0.5f : 1f;

            using (var f = _titleFont)
            {
                SizeF sz = g.MeasureString(_bossDefeatText, f);
                float bx = (W - sz.Width) / 2f;
                float by = H / 2f - 50;

                using (var br = new SolidBrush(Color.FromArgb((int)(180 * alpha), 10, 10, 30)))
                    g.FillRectangle(br, bx - 16, by - 10, sz.Width + 32, sz.Height + 20);

                using (var br = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Gold)))
                    g.DrawString(_bossDefeatText, f, br, bx, by);
            }
        }
    }
}
