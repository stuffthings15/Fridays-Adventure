using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Systems
{
    /// <summary>
    /// Extended HUD renderer providing SMB3-style UI panels.
    ///
    /// Team 1  (Game Director)      — Idea 3: World/Level identifier on HUD.
    /// Team 9  (UI Programmer)      — Idea 1: lives counter (♥ ×N).
    /// Team 9  (UI Programmer)      — Idea 2: power-up reserve item box.
    /// Team 9  (UI Programmer)      — Idea 4: pause-style overlays.
    /// Team 9  (UI Programmer)      — Idea 6: combo counter display.
    /// Team 9  (UI Programmer)      — Idea 7: named boss HP bar.
    /// Team 9  (UI Programmer)      — Idea 8: world-level label.
    /// Team 9  (UI Programmer)      — Idea 3: level timer display.
    /// Team 9  (UI Programmer)      — Idea 4: score display.
    /// Team 9  (UI Programmer)      — Idea 5: coin counter.
    /// Team 9  (UI Programmer)      — Idea 6: status effect icons.
    /// Team 9  (UI Programmer)      — Idea 7: toast notification system.
    /// Team 9  (UI Programmer)      — Idea 8: character portrait panel.
    /// Team 9  (UI Programmer)      — Idea 9: P-Meter bar.
    /// Team 9  (UI Programmer)      — Idea 10: death fade overlay.
    /// Team 15 (UI/UX Artist)       — Idea 4: blinking cursor / arrow selector.
    /// Team 16 (2D Animator)        — Idea 1: lives display slide-in.
    /// </summary>
    public static class SMB3Hud
    {
        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font _hudFont  = new Font("Courier New", 13, FontStyle.Bold);
        private static readonly Font _smFont   = new Font("Courier New", 9,  FontStyle.Bold);
        private static readonly Font _bigFont  = new Font("Courier New", 22, FontStyle.Bold);

        // ── Top HUD quick-action buttons ─────────────────────────────────────
        private static Rectangle _inventoryHudButton;
        private static Rectangle _medkitHudButton;

        // ── GET READY overlay ──────────────────────────────────────────────────
        private static float  _getReadyTimer;
        private static bool   _getReadyActive;
        private const  float  GetReadyDuration = 2.2f;

        /// <summary>
        /// Triggers the SMB3 "GET READY!" overlay that appears at level start.
        /// Team 1 (Game Director) — Idea 4.
        /// </summary>
        public static void TriggerGetReady() { _getReadyTimer = GetReadyDuration; _getReadyActive = true; }

        // ── World/level slide-in ───────────────────────────────────────────────
        private static string _worldLabel;
        private static float  _worldLabelTimer;
        private const  float  WorldLabelDuration = 3.5f;

        /// <summary>
        /// Shows "WORLD 1-1" or equivalent label at the top of the screen.
        /// Team 9 (UI Programmer) — Idea 8.
        /// </summary>
        public static void ShowWorldLabel(string label)
        {
            _worldLabel      = label;
            _worldLabelTimer = WorldLabelDuration;
        }

        // ── Boss HP bar ────────────────────────────────────────────────────────
        private static string _bossName;
        private static float  _bossHpNorm;   // 0–1
        private static bool   _bossBarActive;

        /// <summary>
        /// Shows/updates the named boss HP bar at the bottom of the screen.
        /// Team 9 (UI Programmer) — Idea 7.
        /// </summary>
        public static void SetBossBar(string name, int current, int max)
        {
            _bossName    = name;
            _bossHpNorm  = max > 0 ? (float)current / max : 0f;
            _bossBarActive = current > 0;
        }

        /// <summary>Hides the boss HP bar (call when boss scene exits).</summary>
        public static void HideBossBar() => _bossBarActive = false;

        // ── Update ─────────────────────────────────────────────────────────────

        /// <summary>Advances all HUD timers.</summary>
        public static void Update(float dt)
        {
            if (_getReadyActive)
            {
                _getReadyTimer -= dt;
                if (_getReadyTimer <= 0f) _getReadyActive = false;
            }
            if (_worldLabelTimer > 0f) _worldLabelTimer -= dt;
        }

        // ── Draw ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Draws all SMB3-style HUD elements.  Call from each gameplay scene's
        /// Draw() method after the game world but before debug overlays.
        /// </summary>
        public static void Draw(Graphics g, int W, int H)
        {
            // Consistent top HUD container for all gameplay scenes.
            // Solid black band ensures readability on every background.
            const int topBarHeight = 104;
            g.FillRectangle(Brushes.Black, 0, 0, W, topBarHeight);
            g.DrawLine(Pens.DimGray, 0, topBarHeight, W, topBarHeight);

            DrawLivesCounter(g, W, H);
            DrawWorldLabel(g, W, H);
            DrawGetReady(g, W, H);
            DrawBossBar(g, W, H);
            DrawLevelTimer(g, W);
            DrawScore(g);
            DrawCoinCounter(g, W);

            DrawTopHudQuickAccess(g, W);
        }

        /// <summary>
        /// Draws player HP and ICE reserve bars in the top HUD band.
        /// </summary>
        private static void DrawTopVitals(Graphics g, Entities.Player player)
        {
            if (player == null) return;

            int x = 6;
            int hpY = 62;
            int iceY = 80;

            // HP
            g.DrawString("HP", _smFont, Brushes.White, x, hpY - 2);
            g.FillRectangle(Brushes.DarkRed, x + 24, hpY, 150, 10);
            using (var br = new SolidBrush(Color.LimeGreen))
                g.FillRectangle(br, x + 24, hpY,
                    (int)(150f * Math.Max(0f, Math.Min(1f, (float)player.Health / Math.Max(1, player.MaxHealth)))), 10);
            g.DrawRectangle(Pens.DimGray, x + 24, hpY, 150, 10);

            // ICE
            g.DrawString("ICE", _smFont, Brushes.Cyan, x, iceY - 2);
            g.FillRectangle(Brushes.DarkSlateBlue, x + 24, iceY, 150, 10);
            using (var br = new SolidBrush(Color.FromArgb(180, 220, 255)))
                g.FillRectangle(br, x + 24, iceY,
                    (int)(150f * Math.Max(0f, Math.Min(1f, (float)player.IceReserve / Math.Max(1, player.MaxIceReserve)))), 10);
            g.DrawRectangle(Pens.DimGray, x + 24, iceY, 150, 10);
        }

        /// <summary>
        /// Draws quick-access inventory and medkit indicators in the top HUD.
        /// </summary>
        private static void DrawTopHudQuickAccess(Graphics g, int W)
        {
            _inventoryHudButton = new Rectangle(W - 390, 72, 86, 24);
            _medkitHudButton    = new Rectangle(W - 296, 72, 114, 24);

            using (var br = new SolidBrush(Color.FromArgb(170, 20, 20, 32)))
            {
                g.FillRectangle(br, _inventoryHudButton);
                g.FillRectangle(br, _medkitHudButton);
            }
            g.DrawRectangle(Pens.Cyan, _inventoryHudButton);
            g.DrawRectangle(Pens.LimeGreen, _medkitHudButton);

            using (var f = new Font("Courier New", 8, FontStyle.Bold))
            {
                g.DrawString("INV [I]", f, Brushes.Cyan, _inventoryHudButton.X + 8, _inventoryHudButton.Y + 6);
                g.DrawString($"MED x{PowerUpInventory.HealthItemCount}", f, Brushes.LimeGreen, _medkitHudButton.X + 6, _medkitHudButton.Y + 6);
            }
        }

        /// <summary>
        /// Handles mouse clicks on top HUD quick actions.
        /// Returns true if the click was consumed.
        /// </summary>
        public static bool HandleHudClick(Point p, Entities.Player player)
        {
            if (_inventoryHudButton.Contains(p))
            {
                Game.Instance.Scenes.Push(new Fridays_Adventure.Scenes.InventoryScene(player));
                return true;
            }

            if (_medkitHudButton.Contains(p))
            {
                if (PowerUpInventory.UseHealthItem(player))
                {
                    Game.Instance.Audio.BeepHeal();
                    ShowToast($"Used medkit. Remaining: {PowerUpInventory.HealthItemCount}");
                }
                else
                {
                    ShowToast("No medkit available (or HP is full).");
                }
                return true;
            }

            return false;
        }

        // ── Lives counter ─────────────────────────────────────────────────────

        /// <summary>
        /// Draws the SMB3-style lives counter (♥ × N) in the top-right corner.
        /// Team 9 (UI Programmer) — Idea 1.
        /// </summary>
        private static void DrawLivesCounter(Graphics g, int W, int H)
        {
            int lives = Game.Instance.CurrentLives;
            int x = W - 130, y = 6;

            using (var br = new SolidBrush(Color.FromArgb(160, 4, 4, 16)))
                g.FillRectangle(br, x - 6, y - 2, 120, 24);

            // Heart icon (red filled ellipse).
            using (var br = new SolidBrush(Color.FromArgb(220, Color.Crimson)))
                g.FillEllipse(br, x, y + 3, 13, 13);

            using (var f = _smFont)
            {
                g.DrawString($"× {lives}", f, Brushes.White, x + 17, y + 3);
            }
        }

        // ── World label ───────────────────────────────────────────────────────

        /// <summary>
        /// Draws a sliding "WORLD 1-1" label at the top centre of the screen.
        /// Team 9 (UI Programmer) — Idea 8.
        /// </summary>
        private static void DrawWorldLabel(Graphics g, int W, int H)
        {
            if (_worldLabelTimer <= 0f || string.IsNullOrEmpty(_worldLabel)) return;

            float t     = _worldLabelTimer / WorldLabelDuration;
            float alpha = t > 0.8f ? (1f - t) / 0.2f
                        : t < 0.2f ? t / 0.2f
                        :            1f;

            using (var f = _smFont)
            {
                SizeF sz = g.MeasureString(_worldLabel, f);
                float bx = (W - sz.Width) / 2f;

                using (var br = new SolidBrush(Color.FromArgb((int)(160 * alpha), 4, 4, 16)))
                    g.FillRectangle(br, bx - 8, 4, sz.Width + 16, 22);
                using (var br = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Gold)))
                    g.DrawString(_worldLabel, f, br, bx, 6);
            }
        }

        // ── GET READY ────────────────────────────────────────────────────────

        /// <summary>
        /// Draws the SMB3 "GET READY!" centre-screen banner.
        /// Team 1 (Game Director) — Idea 4.
        /// </summary>
        private static void DrawGetReady(Graphics g, int W, int H)
        {
            if (!_getReadyActive) return;

            float t     = _getReadyTimer / GetReadyDuration;
            float alpha = t > 0.85f ? (1f - t) / 0.15f
                        : t < 0.15f ? t / 0.15f
                        :             1f;

            using (var f = _bigFont)
            {
                const string text = "GET  READY!";
                SizeF sz = g.MeasureString(text, f);
                float x  = (W - sz.Width) / 2f;
                float y  = H / 2f - sz.Height / 2f - 40;

                // Shadow.
                using (var br = new SolidBrush(Color.FromArgb((int)(200 * alpha), 4, 4, 16)))
                    g.FillRectangle(br, x - 14, y - 8, sz.Width + 28, sz.Height + 16);

                // Text.
                using (var br = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Gold)))
                    g.DrawString(text, f, br, x + 2, y + 2);
                using (var br = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.White)))
                    g.DrawString(text, f, br, x, y);
            }
        }

        // ── Boss HP bar ───────────────────────────────────────────────────────

        /// <summary>
        /// Draws the full-width named boss HP bar at the bottom of the screen.
        /// Team 9 (UI Programmer) — Idea 7.
        /// </summary>
        private static void DrawBossBar(Graphics g, int W, int H)
        {
            if (!_bossBarActive || string.IsNullOrEmpty(_bossName)) return;

            int barH  = 26;
            int barY  = H - barH - 6;
            int barPad = 80;

            // Background.
            using (var br = new SolidBrush(Color.FromArgb(200, 8, 4, 16)))
                g.FillRectangle(br, 0, barY - 4, W, barH + 8);

            // Boss name.
            using (var f = _smFont)
                g.DrawString(_bossName, f, Brushes.OrangeRed, barPad, barY + 4);

            // HP bar track.
            int trackX = barPad + 140;
            int trackW = W - trackX - barPad;
            using (var br = new SolidBrush(Color.FromArgb(60, 60, 60, 60)))
                g.FillRectangle(br, trackX, barY + 4, trackW, 16);

            // HP fill (red → orange as HP decreases).
            float hp = Math.Max(0f, Math.Min(1f, _bossHpNorm));
            Color fill = hp > 0.6f ? Color.OrangeRed
                       : hp > 0.3f ? Color.DarkOrange
                       :              Color.Red;
            using (var br = new SolidBrush(fill))
                g.FillRectangle(br, trackX, barY + 4, (int)(trackW * hp), 16);

            // Track border.
            using (var pen = new Pen(Color.FromArgb(120, 200, 80, 0), 1))
                g.DrawRectangle(pen, trackX, barY + 4, trackW, 16);

            // Percentage text.
            using (var f = _smFont)
                g.DrawString($"{(int)(hp * 100)}%", f, Brushes.White, trackX + trackW + 4, barY + 4);
        }

        // ── Blinking arrow cursor ─────────────────────────────────────────────

        /// <summary>
        /// Draws the SMB3-style blinking arrow cursor next to a menu item.
        /// Team 15 (UI/UX Artist) — Idea 4.
        /// </summary>
        public static void DrawMenuCursor(Graphics g, int x, int y)
        {
            bool blink = (int)(Environment.TickCount / 220) % 2 == 0;
            if (!blink) return;
            using (var f = _hudFont)
                g.DrawString("▶", f, Brushes.Gold, x, y);
        }

        // ── Power-up reserve box ──────────────────────────────────────────────

        /// <summary>
        /// Draws the SMB3-style reserve item box.
        /// Team 9 (UI Programmer) — Idea 2.
        /// </summary>
        public static void DrawReserveItemBox(Graphics g, PowerUpState.PowerUp reserve, int x, int y)
        {
            // Box background.
            using (var br = new SolidBrush(Color.FromArgb(180, 10, 10, 30)))
                g.FillRectangle(br, x, y, 36, 36);
            using (var pen = new Pen(Color.FromArgb(120, 200, 200, 60), 2))
                g.DrawRectangle(pen, x, y, 36, 36);

            // Item symbol inside box.
            using (var f = new Font("Courier New", 14, FontStyle.Bold))
            {
                string sym = PowerUpSymbol(reserve);
                SizeF  sz  = g.MeasureString(sym, f);
                Color  col = PowerUpColor(reserve);
                using (var br = new SolidBrush(col))
                    g.DrawString(sym, f, br, x + (36 - sz.Width) / 2f, y + (36 - sz.Height) / 2f);
            }

            // Label below.
            using (var f = new Font("Courier New", 7))
                g.DrawString("ITEM", f, Brushes.Gray, x + 6, y + 38);
        }

        // ── Combo counter display ─────────────────────────────────────────────

        /// <summary>
        /// Draws the SMB3-style combo / stomp chain counter.
        /// Team 9 (UI Programmer) — Idea 6.
        /// </summary>
        public static void DrawComboCounter(Graphics g, int combo, int x, int y)
        {
            if (combo < 2) return;

            using (var br = new SolidBrush(Color.FromArgb(180, 4, 4, 16)))
                g.FillRectangle(br, x, y, 110, 28);

            using (var f = _hudFont)
            {
                Color col = combo >= 8  ? Color.Gold
                          : combo >= 5  ? Color.OrangeRed
                          :               Color.LimeGreen;
                using (var br = new SolidBrush(col))
                    g.DrawString($"COMBO ×{combo}", f, br, x + 6, y + 3);
            }
        }

        // ── Helper lookups ────────────────────────────────────────────────────

        private static string PowerUpSymbol(PowerUpState.PowerUp p)
        {
            switch (p)
            {
                case PowerUpState.PowerUp.Mushroom:   return "M";
                case PowerUpState.PowerUp.FireFlower: return "F";
                case PowerUpState.PowerUp.Star:       return "★";
                default:                              return "—";
            }
        }

        private static Color PowerUpColor(PowerUpState.PowerUp p)
        {
            switch (p)
            {
                case PowerUpState.PowerUp.Mushroom:   return Color.OrangeRed;
                case PowerUpState.PowerUp.FireFlower: return Color.Orange;
                case PowerUpState.PowerUp.Star:       return Color.Gold;
                default:                              return Color.DimGray;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Team 9 (UI Programmer) — Ideas 3-10 additions
        // ═══════════════════════════════════════════════════════════════════════

        // ── Idea 3: Level timer display ───────────────────────────────────────

        /// <summary>
        /// Draws the level countdown timer in a styled box at the top of the screen.
        /// Flashes red when time is urgent (&lt; 100 seconds).
        /// Team 9 (UI Programmer) — Idea 3.
        /// </summary>
        public static void DrawLevelTimer(Graphics g, int W)
        {
            if (!PowerUpInventory.TimerActive) return;

            float t    = PowerUpInventory.TimeRemaining;
            bool  warn = PowerUpInventory.TimerUrgent;
            bool  blink = warn && ((int)(Environment.TickCount / 200) % 2 == 0);

            // Timer panel background
            int px = W / 2 - 50, py = 6;
            using (var br = new SolidBrush(Color.FromArgb(180, blink ? 80 : 8, 4, 16)))
                g.FillRectangle(br, px, py, 100, 26);
            using (var pen = new Pen(warn ? Color.OrangeRed : Color.FromArgb(80, 180, 180, 180), 1))
                g.DrawRectangle(pen, px, py, 100, 26);

            // Timer label + value
            using (var f = _smFont)
            {
                g.DrawString("TIME", f, Brushes.LightGray, px + 4, py + 2);
                Color vCol = warn ? Color.OrangeRed : Color.White;
                using (var br = new SolidBrush(vCol))
                    g.DrawString($"{(int)t,4}", f, br, px + 50, py + 2);
            }
        }

        // ── Idea 4: Score display ─────────────────────────────────────────────

        /// <summary>
        /// Draws the current score and active multiplier in the top-left HUD panel.
        /// Team 9 (UI Programmer) — Idea 4.
        /// </summary>
        public static void DrawScore(Graphics g)
        {
            long score = ScoreManager.Score;
            float mult = ScoreManager.Multiplier;
            int px = 6, py = 6;

            using (var br = new SolidBrush(Color.FromArgb(160, 4, 4, 16)))
                g.FillRectangle(br, px, py, 160, 24);

            using (var f = _smFont)
            {
                g.DrawString($"SCORE {score:N0}", f, Brushes.White, px + 4, py + 2);
                if (mult > 1f)
                    using (var br = new SolidBrush(Color.Gold))
                        g.DrawString($"×{mult:F1}", f, br, px + 120, py + 2);
            }
        }

        // ── Idea 5: Coin counter display ──────────────────────────────────────

        /// <summary>
        /// Draws a coin (●) counter showing coins towards the next 1-UP.
        /// Team 9 (UI Programmer) — Idea 5.
        /// </summary>
        public static void DrawCoinCounter(Graphics g, int W)
        {
            var game = Game.Instance;
            int coins = game.CoinCount;
            int px = W - 270, py = 6;

            using (var br = new SolidBrush(Color.FromArgb(160, 4, 4, 16)))
                g.FillRectangle(br, px, py, 130, 24);

            using (var f = _smFont)
            {
                // Gold coin symbol
                using (var br = new SolidBrush(Color.Gold))
                    g.DrawString("●", f, br, px + 4, py + 2);
                g.DrawString($"× {coins:D2}/100", f, Brushes.White, px + 20, py + 2);
            }
        }

        // ── Idea 6: Status effect icons ───────────────────────────────────────

        /// <summary>
        /// Draws small status-effect icons (Suppressed, Burning, etc.) under the HP bar.
        /// Team 9 (UI Programmer) — Idea 6.
        /// </summary>
        public static void DrawStatusIcons(Graphics g, Entities.Player player, int x, int y)
        {
            if (player == null) return;
            int ix = x;

            if (player.HasEffect(StatusEffect.Suppressed))
            { DrawStatusIcon(g, ix, y, "SU", Color.Purple);   ix += 22; }
            if (player.HasEffect(StatusEffect.Frozen))
            { DrawStatusIcon(g, ix, y, "FR", Color.Cyan);     ix += 22; }
            if (player.HasEffect(StatusEffect.Burning))
            { DrawStatusIcon(g, ix, y, "BU", Color.OrangeRed); ix += 22; }
            if (player.HasEffect(StatusEffect.Dodging))
            { DrawStatusIcon(g, ix, y, "I", Color.LimeGreen);  ix += 22; }
        }

        private static void DrawStatusIcon(Graphics g, int x, int y, string label, Color color)
        {
            using (var br = new SolidBrush(Color.FromArgb(200, color)))
                g.FillRectangle(br, x, y, 20, 14);
            using (var f = new Font("Courier New", 6, FontStyle.Bold))
                g.DrawString(label, f, Brushes.Black, x + 1, y + 1);
        }

        // ── Idea 7: Notification toast system ────────────────────────────────

        private static readonly System.Collections.Generic.Queue<(string text, float timer)> _toasts =
            new System.Collections.Generic.Queue<(string, float)>();
        private static (string text, float timer) _activeToast;
        private const float ToastDuration = 2.5f;

        /// <summary>
        /// Queues a sliding toast notification message.
        /// Team 9 (UI Programmer) — Idea 7.
        /// </summary>
        public static void ShowToast(string message)
        {
            _toasts.Enqueue((message, ToastDuration));
        }

        /// <summary>Advances the toast queue; call from Update().</summary>
        public static void UpdateToasts(float dt)
        {
            if (_activeToast.timer > 0f)
            {
                _activeToast.timer -= dt;
            }
            else if (_toasts.Count > 0)
            {
                _activeToast = _toasts.Dequeue();
            }
        }

        /// <summary>Draws the active toast notification.</summary>
        public static void DrawToast(Graphics g, int W, int H)
        {
            if (_activeToast.timer <= 0f || string.IsNullOrEmpty(_activeToast.text)) return;

            float t     = _activeToast.timer / ToastDuration;
            float alpha = t > 0.85f ? (1f - t) / 0.15f
                        : t < 0.15f ? t / 0.15f
                        :             1f;

            using (var f = _smFont)
            {
                SizeF sz = g.MeasureString(_activeToast.text, f);
                float bx = (W - sz.Width) / 2f;
                float by = H - 60f;

                using (var br = new SolidBrush(Color.FromArgb((int)(200 * alpha), 4, 4, 16)))
                    g.FillRectangle(br, bx - 10, by - 4, sz.Width + 20, sz.Height + 8);
                using (var br = new SolidBrush(Color.FromArgb((int)(255 * alpha), Color.Gold)))
                    g.DrawString(_activeToast.text, f, br, bx, by);
            }
        }

        // ── Idea 8: Character portrait panel ─────────────────────────────────

        /// <summary>
        /// Draws a small character portrait circle at the bottom-left of the screen.
        /// Uses the archetype colour palette when no portrait sprite is loaded.
        /// Team 9 (UI Programmer) — Idea 8.
        /// </summary>
        public static void DrawCharacterPortrait(Graphics g, int H)
        {
            var game = Game.Instance;
            if (game == null) return;

            int px = 8, py = H - 50;
            // Portrait circle
            Color c = game.SelectedCharacter == PlayableCharacter.Orca  ? Color.SteelBlue
                    : game.SelectedCharacter == PlayableCharacter.Swan  ? Color.WhiteSmoke
                    :                                                       Color.DodgerBlue;
            using (var br = new SolidBrush(Color.FromArgb(220, c)))
                g.FillEllipse(br, px, py, 38, 38);
            using (var pen = new Pen(Color.White, 1))
                g.DrawEllipse(pen, px, py, 38, 38);

            // Name initial inside
            using (var f = _smFont)
            {
                string init = game.SelectedCharacter == PlayableCharacter.Orca ? "O"
                            : game.SelectedCharacter == PlayableCharacter.Swan ? "S"
                            : "F";
                SizeF sz = g.MeasureString(init, f);
                g.DrawString(init, f, Brushes.White,
                    px + (38 - sz.Width) / 2f,
                    py + (38 - sz.Height) / 2f);
            }
        }

        // ── Idea 9: P-Meter bar display ────────────────────────────────────────

        /// <summary>
        /// Draws the SMB3-style P-Meter speed charge bar.
        /// Fills left-to-right as the player builds sprint speed.
        /// Team 9 (UI Programmer) — Idea 9.
        /// </summary>
        public static void DrawPMeter(Graphics g, float pMeterCharge, float threshold, int x, int y)
        {
            float fill = Math.Min(1f, pMeterCharge / threshold);
            int barW = 80, barH = 8;

            // Track
            using (var br = new SolidBrush(Color.FromArgb(100, 40, 40, 60)))
                g.FillRectangle(br, x, y, barW, barH);

            // Fill (gold when full, blue when filling)
            Color fc = fill >= 1f ? Color.Gold : Color.FromArgb(60, 120, 220);
            using (var br = new SolidBrush(fc))
                g.FillRectangle(br, x, y, (int)(barW * fill), barH);

            // Border
            using (var pen = new Pen(Color.FromArgb(80, 200, 200, 80), 1))
                g.DrawRectangle(pen, x, y, barW, barH);

            // "P" label
            using (var f = new Font("Courier New", 7, FontStyle.Bold))
                g.DrawString("P", f, Brushes.White, x + barW + 2, y - 1);
        }

        // ── Idea 10: Death / fade-out overlay ────────────────────────────────

        private static float _deathFadeAlpha;
        private static bool  _deathFadeActive;

        /// <summary>
        /// Triggers a black fade-in overlay used on death / game-over transitions.
        /// Team 9 (UI Programmer) — Idea 10.
        /// </summary>
        public static void TriggerDeathFade() { _deathFadeAlpha = 0f; _deathFadeActive = true; }

        /// <summary>Advances the death fade timer.</summary>
        public static void UpdateDeathFade(float dt)
        {
            if (!_deathFadeActive) return;
            _deathFadeAlpha = Math.Min(1f, _deathFadeAlpha + dt * 1.5f);
            if (_deathFadeAlpha >= 1f) _deathFadeActive = false;
        }

        /// <summary>Draws the death fade overlay.</summary>
        public static void DrawDeathFade(Graphics g, int W, int H)
        {
            if (_deathFadeAlpha <= 0f) return;
            using (var br = new SolidBrush(Color.FromArgb((int)(255 * _deathFadeAlpha), Color.Black)))
                g.FillRectangle(br, 0, 0, W, H);
        }

        // ── Ability Cooldown Display ──────────────────────────────────────────
        
        /// <summary>
        /// Draws three ability cooldown progress bars showing Q/E/R ability status.
        /// Displays recharge time remaining in seconds when on cooldown.
        /// Team 9 (UI Programmer) — Idea 9: ability cooldown indicators.
        /// </summary>
        private static void DrawAbilityCooldowns(Graphics g, Player player, int W, int H)
        {
            if (player == null) return;

            // Keep cooldown bars in the top HUD bar for map-to-map consistency.
            int panelX = 200;
            int panelY = 64;
            int barWidth = 44;
            int barHeight = 8;
            int slotWidth = barWidth + 28; // includes key/time gutter from DrawAbilityBar panel

            // ── Ice Wall (Q) ──
            DrawAbilityBar(g, "Q", player.IceWallCooldownProgress,
                player.IceWallCooldownRemaining, player.IceWallReady,
                panelX, panelY, barWidth, barHeight);

            // ── Flash Freeze (E) ──
            DrawAbilityBar(g, "E", player.FlashFreezeCooldownProgress,
                player.FlashFreezeCooldownRemaining, player.FlashFreezeReady,
                panelX + slotWidth, panelY, barWidth, barHeight);

            // ── Break Wall (R) ──
            DrawAbilityBar(g, "R", player.BreakWallCooldownProgress,
                player.BreakWallCooldownRemaining, player.BreakWallReady,
                panelX + slotWidth * 2, panelY, barWidth, barHeight);
        }

        /// <summary>
        /// Draws a single ability cooldown bar with key label and recharge time.
        /// </summary>
        private static void DrawAbilityBar(Graphics g, string key, float progress, 
            float remainingCooldown, bool isReady, int x, int y, int w, int h)
        {
            // Background panel
            using (var br = new SolidBrush(Color.FromArgb(180, 20, 20, 30)))
                g.FillRectangle(br, x, y, w + 24, h + 20);
            
            // Key label
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString(key, f, Brushes.Gold, x + 4, y + 2);
            
            // Cooldown bar background
            using (var br = new SolidBrush(Color.FromArgb(60, 60, 60)))
                g.FillRectangle(br, x + 4, y + 16, w, h);
            
            // Cooldown progress fill
            if (progress > 0)
            {
                Color fillColor = isReady ? Color.LimeGreen : Color.DarkOrange;
                using (var br = new SolidBrush(fillColor))
                    g.FillRectangle(br, x + 4, y + 16, (int)(w * progress), h);
            }
            
            // Cooldown border
            using (var pen = new Pen(isReady ? Color.LimeGreen : Color.Gray, 1))
                g.DrawRectangle(pen, x + 4, y + 16, w, h);
            
            // Time remaining text (only when cooling down)
            if (!isReady && remainingCooldown > 0)
            {
                string timeText = remainingCooldown.ToString("F1");
                using (var f = new Font("Courier New", 8, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(timeText, f);
                    g.DrawString(timeText, f, Brushes.White, x + w / 2f - sz.Width / 2f, y + 2);
                }
            }
            else if (isReady)
            {
                // "READY" indicator
                using (var f = new Font("Courier New", 7, FontStyle.Bold))
                    g.DrawString("RDY", f, Brushes.LimeGreen, x + 6, y + 2);
            }
        }

        /// <summary>
        /// Draws only shared overlay elements (GET READY, world label, boss bar,
        /// level timer, toast, death fade) without re-filling the HUD band.
        /// Call this in scenes that render their own primary HUD (e.g. IslandScene),
        /// so the local HUD and the shared overlays do not fight each other.
        /// Team 9 (UI Programmer) — shared overlay layer.
        /// </summary>
        /// <summary>
        /// Draws temporal overlay elements only — GET READY, world-label slide-in,
        /// toast notifications, and the death fade. Does NOT re-draw the HUD band.
        /// Called by <see cref="GameHUD.Draw"/> at the end of every frame.
        /// Team 9 (UI Programmer) — shared overlay layer.
        /// </summary>
        public static void DrawOverlays(Graphics g, int W, int H)
        {
            // World label slide-in (centre, temporary)
            DrawWorldLabel(g, W, H);

            // GET READY banner (centre screen, at level start)
            DrawGetReady(g, W, H);

            // Toast notification (bottom-centre)
            DrawToast(g, W, H);

            // Death fade overlay (full-screen, always on top)
            DrawDeathFade(g, W, H);
        }

        // ── Composite draw helper ──────────────────────────────────────────────

        /// <summary>
        /// Draws the complete SMB3-style HUD for a gameplay scene in one call.
        /// Renders lives, score, world label, GET READY overlay, boss HP bar,
        /// level timer, coin counter, character portrait, P-Meter, combo counter,
        /// status icons, and any active toast notifications.
        ///
        /// <paramref name="player"/> provides live per-player stats (may be null if
        /// the player has not yet been created this frame).
        /// <paramref name="boss"/> is the active boss entity — pass null outside
        /// boss fights.
        ///
        /// Call from each gameplay scene's Draw() method after the world tiles
        /// but before debug overlays:
        ///   SMB3Hud.DrawAll(g, _player, null, W, H);
        ///
        /// Team 9  (UI Programmer)   — Ideas 1–10: full HUD suite.
        /// Team 1  (Game Director)   — Idea 3: World/Level label.
        /// Team 16 (2D Animator)     — Idea 1: lives counter slide-in.
        /// </summary>
        /// <param name="g">GDI+ graphics context.</param>
        /// <param name="player">Active player instance, or null during transitions.</param>
        /// <param name="boss">Active boss entity for the HP bar, or null.</param>
        /// <param name="W">Canvas width in pixels.</param>
        /// <param name="H">Canvas height in pixels.</param>
        public static void DrawAll(Graphics g, Entities.Player player, object boss, int W, int H)
        {
            // Core HUD panels (lives, world label, GET READY, boss bar, timer, score, coins).
            Draw(g, W, H);

            if (player != null)
            {
                // Explicit player vitals in the top HUD bar.
                DrawTopVitals(g, player);

                // Ability cooldowns (Q/E/R) inside the top HUD band.
                DrawAbilityCooldowns(g, player, W, H);

                // Character portrait in top-left cluster.
                DrawCharacterPortrait(g, 100);

                // P-Meter charge bar in top-left cluster.
                DrawPMeter(g, player.PMeterCharge, 1.5f, 52, 50);

                // Combo / stomp-chain counter and status icons in top band.
                DrawComboCounter(g, player.StompChain, 6, 34);
                DrawStatusIcons(g, player, 6, 48);

                // Reserve item box near top-right.
                DrawReserveItemBox(g, PowerUpState.PowerUp.None, W - 52, 38);
            }

            // Toast notifications (bottom-centre).
            DrawToast(g, W, H);

            // Death fade (full-screen, always on top of HUD).
            DrawDeathFade(g, W, H);
        }
    }
}
