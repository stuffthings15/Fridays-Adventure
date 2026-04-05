// ────────────────────────────────────────────────────────────────────────────
// GameHUD.cs
// PHASE 2 – Team 9: UI Programmer
// Feature: Unified Heads-Up Display (single call-site for every gameplay scene)
// Purpose: One static method renders the complete HUD reliably on every level.
//          All sub-draws are individually protected so a single failure never
//          blacks out the entire HUD.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Single-source-of-truth HUD renderer.  Every gameplay scene calls:
    ///   <c>GameHUD.Draw(g, _player, W, H);</c>
    /// after resetting the GDI+ transform to screen space.
    ///
    /// Boss scenes additionally pass the boss character:
    ///   <c>GameHUD.Draw(g, _player, W, H, _boss, "BOSS NAME");</c>
    ///
    /// Team 9 (UI Programmer) — complete HUD suite.
    /// </summary>
    public static class GameHUD
    {
        // ── Layout constants ──────────────────────────────────────────────────
        /// <summary>Height of the top HUD band in pixels.</summary>
        public const int BandHeight = 90;

        // Horizontal zone boundaries (right-side anchor off W at draw time)
        private const int LeftX     = 6;      // left column start
        private const int CenterOff = 180;    // half-width of center cluster
        private const int RightW    = 196;    // width of right column

        // ── Shared fonts ──────────────────────────────────────────────────────
        private static readonly Font _f8  = new Font("Courier New",  8, FontStyle.Bold);
        private static readonly Font _f9  = new Font("Courier New",  9, FontStyle.Bold);
        private static readonly Font _f10 = new Font("Courier New", 10, FontStyle.Bold);
        private static readonly Font _f22 = new Font("Courier New", 22, FontStyle.Bold);

        // ── Click targets (updated each frame) ───────────────────────────────
        private static Rectangle _invBtn;
        private static Rectangle _medBtn;

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws the complete HUD.  Call AFTER <c>g.ResetTransform()</c>.
        /// Exceptions inside sub-draws are caught individually so one broken
        /// element never hides the rest of the HUD.
        /// </summary>
        /// <param name="g">GDI+ context (must be in screen space).</param>
        /// <param name="player">Active player, or null during transitions.</param>
        /// <param name="W">Canvas pixel width.</param>
        /// <param name="H">Canvas pixel height.</param>
        /// <param name="boss">Active boss entity, or null outside boss fights.</param>
        /// <param name="bossLabel">Name shown in the boss HP bar.</param>
        public static void Draw(Graphics g, Player player, int W, int H,
                                Character boss = null, string bossLabel = null)
        {
            // ── 1. Solid top band – always drawn first ────────────────────────
            // This is the only call NOT guarded by a per-section try/catch;
            // if the graphics context itself is broken nothing can be drawn anyway.
            g.FillRectangle(Brushes.Black, 0, 0, W, BandHeight);
            using (var pen = new Pen(Color.FromArgb(120, Color.DimGray), 1))
                g.DrawLine(pen, 0, BandHeight, W, BandHeight);

            // ── 2. Left column: player vitals ─────────────────────────────────
            if (player != null)
            {
                try { DrawHP(g, player); }          catch { }
                try { DrawIce(g, player); }         catch { }
                try { DrawStamina(g, player); }     catch { }
                try { DrawLivesRow(g, player); }    catch { }
                try { DrawQuickButtons(g, W); }     catch { /* section guarded */ }
            }

            // ── 3. Center: score, world, ability cooldowns ────────────────────
            try { DrawScore(g, W); }                 catch { /* section guarded */ }
            try { DrawWorldRow(g, W); }              catch { /* section guarded */ }
            if (player != null)
            {
                try { DrawAbilityRow(g, player, W); } catch { /* section guarded */ }
            }

            // ── 4. Right column: coins, berries, timer ────────────────────────
            try { DrawRightColumn(g, W); }           catch { /* section guarded */ }

            // ── 5. Boss HP bar (bottom edge) ──────────────────────────────────
            if (boss != null)
            {
                try { DrawBossBar(g, boss, bossLabel ?? "BOSS", W, H); }
                catch { /* section guarded */ }
            }

            // ── 6. Temporal overlays (GET READY, world label, toast, fade) ────
            try { SMB3Hud.DrawOverlays(g, W, H); }  catch { /* section guarded */ }

            // ── 7. Phase 2 T10 #6: Low-health danger vignette ────────────────
            if (player != null)
            {
                try { DrawVignette(g, player, W, H); }
                catch { /* section guarded */ }
            }
        }

        /// <summary>
        /// Routes a mouse click to HUD buttons.
        /// Returns true if the click was consumed by the HUD.
        /// </summary>
        public static bool HandleClick(Point p, Player player)
        {
            if (_invBtn != default && _invBtn.Contains(p) && player != null)
            {
                try { Game.Instance?.Scenes?.Push(new Scenes.InventoryScene(player)); }
                catch { /* ignore */ }
                return true;
            }
            if (_medBtn != default && _medBtn.Contains(p))
            {
                if (player != null && PowerUpInventory.UseHealthItem(player))
                {
                    try { Game.Instance?.Audio?.BeepHeal(); } catch { /* ignore */ }
                    SMB3Hud.ShowToast($"Medkit used. Remaining: {PowerUpInventory.HealthItemCount}");
                }
                else
                    SMB3Hud.ShowToast("No medkit available (or HP is full).");
                return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE DRAW SECTIONS
        // ─────────────────────────────────────────────────────────────────────

        // ── HP bar ────────────────────────────────────────────────────────────
        private static void DrawHP(Graphics g, Player player)
        {
            // "HP" label
            g.DrawString("HP", _f9, Brushes.White, LeftX, 5);

            // Segmented Mega-Man style bar (20 segments)
            const int barX = 32, barY = 5, segs = 20;
            DrawSegBar(g, player.Health, player.MaxHealth, barX, barY, segs,
                       Color.LimeGreen, Color.OrangeRed);

            // Numeric value to the right of bar
            string val = $"{player.Health}/{player.MaxHealth}";
            g.DrawString(val, _f8, Brushes.White, barX + segs * 10 + 3, barY);
        }

        // ── ENERGY bar (Phase 2 — Team 4 Idea 1 — replaces raw ICE display) ─────
        /// <summary>
        /// Draws the shared ability-energy bar in the left HUD column.
        /// All special abilities drain this pool; it regenerates passively.
        /// Phase 2 — Team 4 (Lead Game Designer) Idea 1: Energy Meter System.
        /// </summary>
        private static void DrawIce(Graphics g, Player player)
        {
            g.DrawString("EN", _f9, Brushes.MediumPurple, LeftX, 26);

            const int barX = 32, barY = 26, segs = 20;
            DrawSegBar(g,
                (int)player.Energy,
                (int)Player.MaxEnergy,
                barX, barY, segs,
                Color.FromArgb(180, 80, 255),   // purple fill
                Color.FromArgb(80, 0, 120));    // dark-purple warn

            string val = $"{(int)player.Energy}/{(int)Player.MaxEnergy}";
            using (var br = new SolidBrush(Color.FromArgb(200, 180, 140, 255)))
                g.DrawString(val, _f8, br, barX + segs * 10 + 3, barY);
        }

        // ── STAMINA bar (Phase 2 — Team 4 Idea 8) ─────────────────────────────
        private static void DrawStamina(Graphics g, Player player)
        {
            g.DrawString("STM", _f9, Brushes.Orange, LeftX, 46);

            const int barX = 32, barY = 46, segs = 20;
            DrawSegBar(g,
                (int)player.Stamina,
                (int)Math.Max(1f, player.MaxStamina),
                barX, barY, segs,
                Color.Gold, Color.OrangeRed);

            string val = $"{(int)player.Stamina}/{(int)player.MaxStamina}";
            using (var br = new SolidBrush(Color.FromArgb(220, 255, 220, 140)))
                g.DrawString(val, _f8, br, barX + segs * 10 + 3, barY);
        }

        // ── Row 3: Lives / P-Meter / Status tags ──────────────────────────────
        private static void DrawLivesRow(Graphics g, Player player)
        {
            int lives = Game.Instance?.CurrentLives ?? 3;

            // Heart
            g.DrawString("♥", _f9, Brushes.Crimson, LeftX, 62);
            g.DrawString($"×{lives}", _f9, Brushes.White, LeftX + 14, 62);

            // P-Meter
            const int pmX = 68, pmY = 65, pmW = 56, pmH = 8;
            float fill = Math.Min(1f, (player.PMeterCharge) / 1.5f);
            using (var br = new SolidBrush(Color.FromArgb(50, 40, 40, 60)))
                g.FillRectangle(br, pmX, pmY, pmW, pmH);
            if (fill > 0)
            {
                Color fc = fill >= 1f ? Color.Gold : Color.DodgerBlue;
                using (var br = new SolidBrush(fc))
                    g.FillRectangle(br, pmX, pmY, (int)(pmW * fill), pmH);
            }
            g.DrawRectangle(Pens.DimGray, pmX, pmY, pmW, pmH);
            g.DrawString("P", _f8, Brushes.White, pmX + pmW + 2, pmY - 1);

            // Status effect tags
            int tx = 140;
            void Tag(string lbl, Color col)
            {
                using (var br = new SolidBrush(Color.FromArgb(200, col)))
                    g.FillRectangle(br, tx, 62, 26, 14);
                using (var f = new Font("Courier New", 6, FontStyle.Bold))
                    g.DrawString(lbl, f, Brushes.Black, tx + 1, 63);
                tx += 28;
            }
            if (player.HasEffect(StatusEffect.Suppressed)) Tag("SUP", Color.Purple);
            if (player.HasEffect(StatusEffect.Frozen))     Tag("FRZ", Color.Cyan);
            if (player.HasEffect(StatusEffect.Burning))    Tag("BRN", Color.OrangeRed);
            if (player.HasEffect(StatusEffect.Dodging))    Tag("INV", Color.LimeGreen);
        }

        // ── INV / MED quick-access buttons (bottom of left column) ───────────
        private static void DrawQuickButtons(Graphics g, int W)
        {
            _invBtn = new Rectangle(LeftX,      70, 80, 18);
            _medBtn = new Rectangle(LeftX + 86, 70, 106, 18);

            using (var br = new SolidBrush(Color.FromArgb(160, 10, 10, 24)))
            {
                g.FillRectangle(br, _invBtn);
                g.FillRectangle(br, _medBtn);
            }
            g.DrawRectangle(Pens.Cyan,      _invBtn);
            g.DrawRectangle(Pens.LimeGreen, _medBtn);

            g.DrawString("[I] INV", _f8, Brushes.Cyan,
                _invBtn.X + 4, _invBtn.Y + 3);
            g.DrawString($"[H] MED ×{PowerUpInventory.HealthItemCount}", _f8, Brushes.LimeGreen,
                _medBtn.X + 4, _medBtn.Y + 3);
        }

        // ── Center: score ─────────────────────────────────────────────────────
        private static void DrawScore(Graphics g, int W)
        {
            string text = $"฿{ScoreManager.Score:N0}";
            int cx = W / 2;
            SizeF sz = g.MeasureString(text, _f10);
            g.DrawString(text, _f10, Brushes.Gold, cx - sz.Width / 2f, 4);
        }

        // ── Center: world/level label ─────────────────────────────────────────
        private static void DrawWorldRow(Graphics g, int W)
        {
            string label = Game.Instance?.WorldLevelLabel ?? "";
            if (string.IsNullOrEmpty(label)) return;
            int cx = W / 2;
            SizeF sz = g.MeasureString(label, _f9);
            using (var br = new SolidBrush(Color.LightSteelBlue))
                g.DrawString(label, _f9, br, cx - sz.Width / 2f, 22);
        }

        // ── Center: ability cooldowns Q / E / R ───────────────────────────────
        private static void DrawAbilityRow(Graphics g, Player player, int W)
        {
            // Six slots: Q, E, R, X, C, B
            int totalW = 6 * 94 + 5 * 4;   // 6 slots + 5 gaps
            int startX = W / 2 - totalW / 2;
            int y = 42;

            // E-key label changes per character: Orca=SLAM, Swan=DASH, Friday=FREEZE
            string eLabel = "E:FREEZE";
            if (player.Archetype == Engine.PlayableCharacter.Orca) eLabel = "E:SLAM";
            else if (player.Archetype == Engine.PlayableCharacter.Swan) eLabel = "E:DASH";

            DrawAbilitySlot(g, "Q:WALL",    player.IceWallCooldownProgress,
                            player.IceWallCooldownRemaining,   player.IceWallReady,
                            startX,          y);
            DrawAbilitySlot(g, eLabel,      player.FlashFreezeCooldownProgress,
                            player.FlashFreezeCooldownRemaining, player.FlashFreezeReady,
                            startX + 98,     y);
            DrawAbilitySlot(g, "R:BREAK",   player.BreakWallCooldownProgress,
                            player.BreakWallCooldownRemaining,  player.BreakWallReady,
                            startX + 196,    y);
            // X: Frost Ball
            DrawAbilitySlot(g, "X:FIRE",   player.FrostBallCooldownProgress,
                            player.FrostBallCooldownRemaining, player.FrostBallReady,
                            startX + 294,    y);
            // C: Air Dash (available if not used this jump)
            bool airDashReady = !player.AirDashUsed && player.IsGrounded;
            DrawAbilitySlot(g, "C:DASH",    airDashReady ? 1f : 0f,
                            airDashReady ? 0f : 1f,  airDashReady,
                            startX + 392,    y);
            // B: Frost Ball
            DrawAbilitySlot(g, "B:FIRE",   player.FrostBallCooldownProgress,
                            player.FrostBallCooldownRemaining, player.FrostBallReady,
                            startX + 490,    y);
        }

        private static void DrawAbilitySlot(Graphics g, string label,
            float progress, float remaining, bool ready, int x, int y)
        {
            const int w = 94, h = 44;

            // Background
            using (var br = new SolidBrush(Color.FromArgb(180, 8, 8, 18)))
                g.FillRectangle(br, x, y, w, h);

            // Progress fill (bottom-up like Mega Man weapon energy)
            if (progress > 0)
            {
                int fillH = (int)(h * progress);
                Color fc = ready ? Color.FromArgb(80, 0, 160, 0) : Color.FromArgb(80, 140, 60, 0);
                using (var br = new SolidBrush(fc))
                    g.FillRectangle(br, x, y + h - fillH, w, fillH);
            }

            // Border
            using (var pen = new Pen(ready ? Color.LimeGreen : Color.FromArgb(100, Color.Gray), 1))
                g.DrawRectangle(pen, x, y, w, h);

            // Key label (top)
            using (var br = new SolidBrush(ready ? Color.White : Color.FromArgb(180, Color.LightGray)))
                g.DrawString(label, _f8, br, x + 3, y + 3);

            // Status text (bottom)
            string status = ready ? "READY" : (remaining > 0.05f ? $"{remaining:F1}s" : "...");
            Color stCol = ready ? Color.LimeGreen : Color.Yellow;
            using (var br = new SolidBrush(stCol))
                g.DrawString(status, _f8, br, x + 3, y + h - 14);
        }

        // ── Right column: coins, berries, timer ───────────────────────────────
        private static void DrawRightColumn(Graphics g, int W)
        {
            int rx = W - RightW;

            // Panel background
            using (var br = new SolidBrush(Color.FromArgb(140, 8, 8, 18)))
                g.FillRectangle(br, rx, 4, RightW - 4, BandHeight - 8);

            int game_coins = Game.Instance?.CoinCount ?? 0;
            g.DrawString("●", _f9, Brushes.Gold, rx + 6, 6);
            g.DrawString($"×{game_coins:D2}/100", _f9, Brushes.White, rx + 22, 6);

            int berries = Game.Instance?.TotalBerriesCollected ?? 0;
            g.DrawString($"Berries: {berries}", _f9, Brushes.Gold, rx + 6, 26);

            // ── Speed-run clock (Phase 2 — Team 1 Idea 3) ────────────────────
            // Reads elapsed level time from Game singleton (set by IslandScene).
            float elapsed = Game.Instance?.LevelElapsedSeconds ?? 0f;
            int   mins    = (int)(elapsed / 60f);
            int   secs    = (int)(elapsed % 60f);
            g.DrawString("TIME",              _f8, Brushes.LightGray, rx + 6,  46);
            g.DrawString($"{mins}:{secs:D2}", _f9, Brushes.White,     rx + 50, 45);

            // Lives
            int lives = Game.Instance?.CurrentLives ?? 3;
            g.DrawString("♥", _f9, Brushes.Crimson, rx + 6, 64);
            g.DrawString($"×{lives}", _f9, Brushes.White, rx + 20, 64);
        }

        // ── Boss HP bar (bottom of screen) ────────────────────────────────────
        private static void DrawBossBar(Graphics g, Character boss, string name, int W, int H)
        {
            if (!boss.IsAlive) return;

            float pct = Math.Max(0f, Math.Min(1f, (float)boss.Health / Math.Max(1, boss.MaxHealth)));
            const int barH = 32;
            int barY = H - barH - 4;
            const int padX = 12;
            int trackX = padX + 170;
            int trackW = W - trackX - padX;

            // Background strip
            using (var br = new SolidBrush(Color.FromArgb(220, 8, 4, 16)))
                g.FillRectangle(br, 0, barY - 6, W, barH + 10);
            using (var pen = new Pen(Color.FromArgb(100, Color.OrangeRed), 1))
                g.DrawLine(pen, 0, barY - 6, W, barY - 6);

            // Boss name
            g.DrawString(name.ToUpper(), _f9, Brushes.OrangeRed, padX, barY + 4);

            // Track
            using (var br = new SolidBrush(Color.FromArgb(50, 50, 50, 50)))
                g.FillRectangle(br, trackX, barY + 4, trackW, 18);

            // Fill (colour shifts red → orange → dark-red as HP falls)
            Color fill = pct > 0.6f ? Color.OrangeRed
                       : pct > 0.3f ? Color.DarkOrange
                       :               Color.Red;
            using (var br = new SolidBrush(fill))
                g.FillRectangle(br, trackX, barY + 4, (int)(trackW * pct), 18);

            // Highlight bead on top of fill
            using (var br = new SolidBrush(Color.FromArgb(60, Color.White)))
                g.FillRectangle(br, trackX, barY + 4, (int)(trackW * pct), 4);

            // Border
            g.DrawRectangle(Pens.Gray, trackX, barY + 4, trackW, 18);

            // Percentage
            g.DrawString($"{(int)(pct * 100)}%", _f8, Brushes.White,
                trackX + trackW + 5, barY + 6);
        }

        // ─────────────────────────────────────────────────────────────────────
        // ── Phase 2 T10 #6: Low-health danger vignette ───────────────────────
        /// <summary>
        /// Draws a red/orange radial vignette around the screen edges when the player
        /// is at or below 25% HP. Intensity increases as HP drops toward 0.
        /// Phase 2 — Team 10 (Engine Programmer) Idea 6: Vignette Renderer.
        /// </summary>
        private static void DrawVignette(Graphics g, Player player, int W, int H)
        {
            float hpPct = (float)player.Health / Math.Max(1, player.MaxHealth);
            if (hpPct > 0.35f) return;   // only visible below 35 % HP

            // Pulse the intensity for urgency (faster at lower HP)
            float pulseSpeed = 3f + (1f - hpPct) * 6f;
            float pulse      = 0.5f + 0.5f * (float)Math.Sin(
                (Game.Instance?.Stats?.PlaySeconds ?? 0) * pulseSpeed);
            float intensity  = (0.35f - hpPct) / 0.35f;   // 0 at 35%, 1 at 0%

            int alpha = (int)(intensity * pulse * 160);
            if (alpha <= 0) return;

            // Four semi-transparent edge rectangles forming a vignette frame.
            int thickness = (int)(W * 0.18f * intensity);
            using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, BandHeight, thickness, H),
                Color.FromArgb(alpha, Color.DarkRed), Color.Transparent,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                g.FillRectangle(br, 0, BandHeight, thickness, H - BandHeight);

            using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(W - thickness, BandHeight, thickness, H),
                Color.Transparent, Color.FromArgb(alpha, Color.DarkRed),
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                g.FillRectangle(br, W - thickness, BandHeight, thickness, H - BandHeight);

            int botThick = (int)(H * 0.18f * intensity);
            using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, H - botThick, W, botThick),
                Color.Transparent, Color.FromArgb(alpha, Color.DarkRed),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                g.FillRectangle(br, 0, H - botThick, W, botThick);
        }

        // SHARED PRIMITIVE: segmented bar (Mega Man weapon energy style)
        // ─────────────────────────────────────────────────────────────────────
        private static void DrawSegBar(Graphics g, int current, int max,
                                        int x, int y, int segs,
                                        Color fillColor, Color warnColor)
        {
            if (max <= 0) return;
            const int segW = 9, gap = 1, segH = 16;
            float pct    = Math.Max(0f, Math.Min(1f, (float)current / max));
            int   filled = Math.Min(segs, (int)(pct * segs + 0.5f));

            for (int i = 0; i < segs; i++)
            {
                int sx = x + i * (segW + gap);
                bool isOn = (i < filled);

                // Low HP warning: shift to warn colour for bottom quarter of bar
                Color fc = (isOn && pct < 0.25f) ? warnColor : fillColor;

                using (var br = new SolidBrush(isOn ? fc : Color.FromArgb(38, 38, 50)))
                    g.FillRectangle(br, sx, y, segW, segH);

                // Specular highlight on filled segments
                if (isOn)
                    using (var br = new SolidBrush(Color.FromArgb(55, Color.White)))
                        g.FillRectangle(br, sx, y, segW, 3);
            }

            // Outer border
            g.DrawRectangle(Pens.DimGray, x, y, segs * (segW + gap) - gap, segH);
        }
    }
}
