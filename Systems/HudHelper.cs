using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Systems
{
    public static class HudHelper
    {
        /// <summary>
        /// Draws a single ability cooldown panel in Mega Man sub-weapon style.
        /// A segmented vertical energy bar on the left shows charge level;
        /// the key label and cooldown countdown appear on the right.
        /// </summary>
        public static void DrawAbilityIcon(Graphics g, int x, int y, string label, float progress, bool suppressed, float remainingSecs)
        {
            // ── Dark Mega Man panel background ───────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(220, 12, 18, 36)))
                g.FillRectangle(br, x, y, 90, 74);

            // ── Segmented energy bar (left side, 14 segments, fills bottom-up) ──
            const int segCount = 14, segH = 4, segGap = 1, barW = 16;
            bool ready = !suppressed && progress >= 1f;
            Color fillColor = suppressed    ? Color.FromArgb(55, 60, 70)
                            : ready         ? Color.FromArgb(60, 220, 240)  // cyan when ready
                            :                 Color.FromArgb(28, 120, 190); // blue-steel charging
            int filledSegs = suppressed ? 0 : Math.Min(segCount, (int)(progress * segCount));

            for (int i = 0; i < segCount; i++)
            {
                // Segment y: index 0 = bottom-most segment
                int sy = y + 70 - i * (segH + segGap);
                bool filled = i < filledSegs;
                using (var br = new SolidBrush(filled ? fillColor : Color.FromArgb(28, 38, 52)))
                    g.FillRectangle(br, x + 4, sy, barW, segH);
                // Bright top-edge highlight on filled ready segments
                if (filled && ready)
                    using (var br = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
                        g.FillRectangle(br, x + 4, sy, barW, 1);
            }

            // ── Cyan/gray border ─────────────────────────────────────────────
            using (var pen = new Pen(suppressed ? Color.FromArgb(55, 80, 80) : Color.FromArgb(80, 200, 220), 1))
                g.DrawRectangle(pen, x, y, 90, 74);

            // ── Ability label (right of bar) ─────────────────────────────────
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString(label, f, suppressed ? Brushes.DimGray : Brushes.White, x + 24, y + 22);

            // ── Cooldown / ready indicator ───────────────────────────────────
            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                if (suppressed)
                    g.DrawString("LOCK", f, Brushes.DimGray, x + 28, y + 52);
                else if (remainingSecs > 0.05f)
                    g.DrawString($"{remainingSecs:F1}s", f, Brushes.Yellow, x + 28, y + 52);
                else
                    g.DrawString("READY", f, Brushes.LimeGreen, x + 16, y + 52);
            }
        }

        /// <summary>
        /// Draws all three ability cooldown panels side by side.
        /// </summary>
        public static void DrawAbilityBar(Graphics g, Player player, int startX, int startY)
        {
            DrawAbilityIcon(g, startX,       startY, "Q:Wall",   player.IceWallCooldownProgress,      player.IsSuppressed, player.IceWallCooldownRemaining);
            DrawAbilityIcon(g, startX + 94,  startY, "E:Freeze", player.FlashFreezeCooldownProgress,  player.IsSuppressed, player.FlashFreezeCooldownRemaining);
            DrawAbilityIcon(g, startX + 188, startY, "R:Break",  player.BreakWallCooldownProgress,    false,               player.BreakWallCooldownRemaining);
        }

        /// <summary>
        /// Draws status effect tags (SINKING, SUPPRESSED, etc.) next to the HUD.
        /// </summary>
        public static void DrawStatusTags(Graphics g, Player player, int startX, int startY)
        {
            int x = startX;
            if (player.HasEffect(StatusEffect.Sinking))    DrawTag(g, ref x, startY, "SINKING",    Color.Blue);
            if (player.HasEffect(StatusEffect.Suppressed)) DrawTag(g, ref x, startY, "SUPPRESSED", Color.Olive);
            if (player.HasEffect(StatusEffect.Burning))    DrawTag(g, ref x, startY, "BURNING",    Color.OrangeRed);
            if (player.HasEffect(StatusEffect.Frozen))     DrawTag(g, ref x, startY, "FROZEN",     Color.LightBlue);
        }

        private static void DrawTag(Graphics g, ref int x, int y, string text, Color color)
        {
            using (var br = new SolidBrush(Color.FromArgb(180, color)))
                g.FillRectangle(br, x, y, 100, 20);
            using (var f = new Font("Courier New", 9, FontStyle.Bold))
                g.DrawString(text, f, Brushes.White, x + 3, y + 2);
            x += 104;
        }

        /// <summary>
        /// Draws the score and berry count with SMB3-style coin icon prefix.
        /// </summary>
        public static void DrawBountyAndBerries(Graphics g, int x, int y)
        {
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                // SMB3-style coin icon before score text
                using (var br = new SolidBrush(Color.FromArgb(255, 220, 0)))
                    g.FillEllipse(br, x, y + 3, 11, 11);
                using (var br = new SolidBrush(Color.FromArgb(200, 150, 0)))
                    g.FillEllipse(br, x + 3, y + 6, 5, 5);

                g.DrawString($"SCORE: {BountySystem.Formatted()}", f, Brushes.Gold, x + 14, y);

                // Coin icon for berry line too
                using (var br = new SolidBrush(Color.FromArgb(255, 220, 0)))
                    g.FillEllipse(br, x, y + 27, 11, 11);
                using (var br = new SolidBrush(Color.FromArgb(200, 150, 0)))
                    g.FillEllipse(br, x + 3, y + 30, 5, 5);

                g.DrawString($"Berries: {Game.Instance.TotalBerriesCollected}", f, Brushes.Gold, x + 14, y + 24);
            }
        }

        public static void DrawBreakShockwave(Graphics g, float timer, float worldX, float worldY)
        {
            if (timer <= 0f) return;
            float prog   = timer / 0.4f;
            float radius = prog * 100f;
            int   alpha  = (int)(200 * (1f - prog));
            using (var pen = new Pen(Color.FromArgb(Math.Max(0, alpha), Color.OrangeRed), 3))
                g.DrawEllipse(pen, worldX - radius, worldY - radius, radius * 2f, radius * 2f);
        }

        public static void UpdateBreakShockwaveTimer(ref float timer, float dt)
        {
            if (timer > 0f) { timer += dt; if (timer >= 0.40f) timer = 0f; }
        }
    }
}
