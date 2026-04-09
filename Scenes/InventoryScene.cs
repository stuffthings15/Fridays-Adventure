using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Inventory screen that displays collected items, current resources, and
    /// player stats. Accessible from the pause menu during gameplay. Labels
    /// update to reflect the latest collection totals.
    /// </summary>
    public sealed class InventoryScene : Scene
    {
        private static readonly Font _titleFont  = new Font("Courier New", 22, FontStyle.Bold);
        private static readonly Font _headerFont = new Font("Courier New", 13, FontStyle.Bold);
        private static readonly Font _bodyFont   = new Font("Courier New", 11);
        private static readonly Font _hintFont   = new Font("Courier New", 10, FontStyle.Bold);

        /// <summary>Reference to the active player (if in a level), or null.</summary>
        private readonly Entities.Player _player;

        /// <summary>
        /// Creates the inventory scene. Optionally accepts a Player reference
        /// so live HP / ICE can be displayed during a level.
        /// </summary>
        public InventoryScene(Entities.Player player = null)
        {
            _player = player;
        }

        private Rectangle _useHealthBtn;
        private Rectangle _useReserveBtn;

        /// <summary>Clickable "Return to Game" button drawn at the top of the screen.</summary>
        private Rectangle _returnBtnTop;
        /// <summary>Clickable "Return to Game" button drawn at the bottom of the screen.</summary>
        private Rectangle _returnBtnBottom;

        public override void OnEnter() { }
        public override void OnExit()  { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;

            // Use health item while inventory is open.
            if (input.IsPressed(System.Windows.Forms.Keys.H) && _player != null)
            {
                if (PowerUpInventory.UseHealthItem(_player))
                {
                    Game.Instance.Audio.BeepHeal();
                    SMB3Hud.ShowToast($"Used medkit. Remaining: {PowerUpInventory.HealthItemCount}");
                }
                else
                {
                    SMB3Hud.ShowToast("No medkit available (or HP is full).");
                }
            }

            // Use reserve item while inventory is open.
            if (input.IsPressed(System.Windows.Forms.Keys.R))
            {
                var used = PowerUpInventory.UseReserve();
                if (used != SuitType.None)
                {
                    Game.Instance.Audio.BeepPowerup();
                    SMB3Hud.ShowToast($"Used reserve item: {used}");
                }
                else
                {
                    SMB3Hud.ShowToast("Reserve box is empty.");
                }
            }

            // Close inventory on Esc, I, or Enter — pop all the way to gameplay
            if (input.PausePressed || input.InteractPressed ||
                input.IsPressed(System.Windows.Forms.Keys.I))
            {
                // Pop this scene first
                Game.Instance.Scenes.Pop();
                // If PauseScene is now on top, pop it too so the player returns to gameplay
                if (Game.Instance.Scenes.Current is PauseScene)
                    Game.Instance.Scenes.Pop();
                return;
            }
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (HandleMainMenuClick(p)) return;

            if (_useHealthBtn.Contains(p) && _player != null)
            {
                if (PowerUpInventory.UseHealthItem(_player))
                {
                    Game.Instance.Audio.BeepHeal();
                    SMB3Hud.ShowToast($"Used medkit. Remaining: {PowerUpInventory.HealthItemCount}");
                }
                else
                {
                    SMB3Hud.ShowToast("No medkit available (or HP is full).");
                }
                return;
            }

            if (_useReserveBtn.Contains(p))
            {
                var used = PowerUpInventory.UseReserve();
                if (used != SuitType.None)
                {
                    Game.Instance.Audio.BeepPowerup();
                    SMB3Hud.ShowToast($"Used reserve item: {used}");
                }
                else
                {
                    SMB3Hud.ShowToast("Reserve box is empty.");
                }
                return;
            }

            // Return to Game buttons (top or bottom)
            if (_returnBtnTop.Contains(p) || _returnBtnBottom.Contains(p))
            {
                PopToGameplay();
                return;
            }

            // Click elsewhere to close
            PopToGameplay();
        }

        // ── Draw ─────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Semi-transparent backdrop
            using (var br = new SolidBrush(Color.FromArgb(210, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            // ── Title ────────────────────────────────────────────────────────
            const string title = "INVENTORY";
            SizeF tsz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.Gold, (W - tsz.Width) / 2f, 8);

            // ── Return to Game button — TOP (always visible) ─────────────────
            DrawReturnButton(g, W, H, isTop: true);

            // ── Layout: two columns ──────────────────────────────────────────
            int colW   = (W - 60) / 2;
            int leftX  = 20;
            int rightX = leftX + colW + 20;
            int startY = 60;

            // ── Left column: Resources & Stats ───────────────────────────────
            DrawSectionHeader(g, leftX, startY, colW, "RESOURCES");
            int y = startY + 30;

            // Health
            if (_player != null)
            {
                DrawStatRow(g, leftX, ref y, colW, "Health (HP)",
                    $"{_player.Health} / {_player.MaxHealth}", Color.LimeGreen,
                    (float)_player.Health / _player.MaxHealth);
            }
            else
            {
                DrawStatRow(g, leftX, ref y, colW, "Health (HP)", "— (not in level)", Color.DimGray, 0f);
            }

            // Magic (ICE reserve)
            if (_player != null)
            {
                DrawStatRow(g, leftX, ref y, colW, "Magic (ICE)",
                    $"{_player.IceReserve} / {_player.MaxIceReserve}", Color.Cyan,
                    (float)_player.IceReserve / _player.MaxIceReserve);
            }
            else
            {
                DrawStatRow(g, leftX, ref y, colW, "Magic (ICE)", "— (not in level)", Color.DimGray, 0f);
            }

            y += 10;

            // Score
            DrawLabelValue(g, leftX, ref y, "Score (Bounty)", $"{Game.Instance.PlayerBounty:N0}", Brushes.Gold);
            DrawLabelValue(g, leftX, ref y, "Crew Bonds", $"{Game.Instance.CrewBonds}", Brushes.Cyan);
            DrawLabelValue(g, leftX, ref y, "Threat Level", $"{Game.Instance.ThreatLevel:P0}", Brushes.OrangeRed);

            y += 16;
            DrawSectionHeader(g, leftX, y, colW, "SHIP STATUS");
            y += 30;
            DrawLabelValue(g, leftX, ref y, "Ship Health", $"{Game.Instance.ShipHealth}", Brushes.LimeGreen);
            DrawLabelValue(g, leftX, ref y, "Cargo", $"{Game.Instance.Cargo}", Brushes.LightGray);
            DrawLabelValue(g, leftX, ref y, "Water Supply", $"{Game.Instance.Water}", Brushes.LightBlue);
            DrawLabelValue(g, leftX, ref y, "Food Supply", $"{Game.Instance.Food}", Brushes.SandyBrown);

            // ── Right column: Collected Items ────────────────────────────────
            int ry = startY;
            DrawSectionHeader(g, rightX, ry, colW, "COLLECTED ITEMS");
            ry += 30;

            // Berries
            DrawItemEntry(g, rightX, ref ry, colW,
                "Berries (Gold)", Game.Instance.TotalBerriesCollected,
                "Increases score when collected", Color.Gold);

            // Health Pickups (inventory-backed consumables)
            DrawItemEntry(g, rightX, ref ry, colW,
                "Health Items", PowerUpInventory.HealthItemCount,
                "Press H or click USE to restore 30 HP", Color.FromArgb(220, 55, 55));

            _useHealthBtn = new Rectangle(rightX + 28, ry - 4, 110, 26);
            using (var br = new SolidBrush(Color.FromArgb(120, 40, 90, 40)))
                g.FillRectangle(br, _useHealthBtn);
            g.DrawRectangle(Pens.LimeGreen, _useHealthBtn);
            using (var bf = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("USE (H)", bf, Brushes.LimeGreen, _useHealthBtn.X + 12, _useHealthBtn.Y + 4);
            ry += 28;

            // Ice Reserve
            DrawItemEntry(g, rightX, ref ry, colW,
                "ICE Power", -1,
                "Regenerates over time — powers abilities", Color.FromArgb(180, 220, 255));

            // Reserve box item (hotkey + clickable).
            string reserveLabel = PowerUpInventory.ReserveItem == SuitType.None
                ? "Empty"
                : PowerUpInventory.ReserveItem.ToString();
            DrawItemEntry(g, rightX, ref ry, colW,
                "Reserve Box", -1,
                $"Current: {reserveLabel}  (Press R or click USE)", Color.FromArgb(255, 220, 120));

            _useReserveBtn = new Rectangle(rightX + 28, ry - 4, 110, 26);
            using (var br = new SolidBrush(Color.FromArgb(120, 90, 70, 20)))
                g.FillRectangle(br, _useReserveBtn);
            g.DrawRectangle(Pens.Gold, _useReserveBtn);
            using (var bf = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("USE (R)", bf, Brushes.Gold, _useReserveBtn.X + 12, _useReserveBtn.Y + 4);
            ry += 28;

            // Sea Stone counter
            if (Game.Instance.SeaStoneCount > 0)
            {
                DrawItemEntry(g, rightX, ref ry, colW,
                    "Sea Stone Encounters", Game.Instance.SeaStoneCount,
                    "Suppresses Devil Fruit powers", Color.Olive);
            }

            ry += 16;
            DrawSectionHeader(g, rightX, ry, colW, "CHARACTER");
            ry += 30;

            string charName;
            switch (Game.Instance.SelectedCharacter)
            {
                case PlayableCharacter.Orca:       charName = "ORCA — Brawler"; break;
                case PlayableCharacter.Swan:        charName = "SWAN — Speedster"; break;
                default:                            charName = "MISS FRIDAY — Lead"; break;
            }
            DrawLabelValue(g, rightX, ref ry, "Active", charName, Brushes.White);
            DrawLabelValue(g, rightX, ref ry, "Player", Game.Instance.PlayerName, Brushes.LightGray);

            // ── Return to Game button — BOTTOM (large, bright) ─────────────
            DrawReturnButton(g, W, H, isTop: false);

            // ── Control hints ────────────────────────────────────────────────
            g.DrawString("[Esc] Return to Game   [H] Medkit   [R] Reserve Item",
                _hintFont, Brushes.DimGray, W / 2 - 220, H - 20);

            DrawMainMenuReturnButton(g);
            DrawDevMenuButton(g);
        }

        // ── Drawing helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Pops this scene and any PauseScene beneath it so the player
        /// returns directly to gameplay instead of just one menu layer.
        /// </summary>
        private void PopToGameplay()
        {
            Game.Instance.Scenes.Pop();
            if (Game.Instance.Scenes.Current is PauseScene)
                Game.Instance.Scenes.Pop();
        }

        /// <summary>
        /// Draws a large, bright "RETURN TO GAME" button. Called twice —
        /// once at the top of the inventory and once at the bottom —
        /// so there is always a visible exit no matter where the user looks.
        /// </summary>
        private void DrawReturnButton(Graphics g, int W, int H, bool isTop)
        {
            int rbW = 320, rbH = 46;
            int rbX = (W - rbW) / 2;
            int rbY = isTop ? 38 : H - 76;
            var rect = new Rectangle(rbX, rbY, rbW, rbH);

            // Store reference so HandleClick can detect it
            if (isTop) _returnBtnTop = rect;
            else       _returnBtnBottom = rect;

            // Bright orange-red fill so it's impossible to miss
            using (var br = new SolidBrush(Color.FromArgb(230, 200, 60, 20)))
                g.FillRectangle(br, rect);
            using (var pen = new Pen(Color.Gold, 3))
                g.DrawRectangle(pen, rect);
            using (var bf = new Font("Courier New", 16, FontStyle.Bold))
            {
                const string label = "\u25C0  RETURN TO GAME  \u25C0";
                SizeF sz = g.MeasureString(label, bf);
                g.DrawString(label, bf, Brushes.White,
                    rect.X + (rect.Width  - sz.Width)  / 2f,
                    rect.Y + (rect.Height - sz.Height) / 2f);
            }
        }

        /// <summary>Draws a section header bar with a label.</summary>
        private void DrawSectionHeader(Graphics g, int x, int y, int w, string label)
        {
            using (var br = new SolidBrush(Color.FromArgb(100, 40, 80, 140)))
                g.FillRectangle(br, x, y, w, 24);
            using (var pen = new Pen(Color.FromArgb(160, Color.Cyan), 1))
                g.DrawLine(pen, x, y + 24, x + w, y + 24);
            g.DrawString(label, _headerFont, Brushes.Cyan, x + 6, y + 3);
        }

        /// <summary>Draws a stat row with a label, value text, and a fill bar.</summary>
        private void DrawStatRow(Graphics g, int x, ref int y, int w,
            string label, string valueText, Color barColor, float fillPct)
        {
            g.DrawString(label, _bodyFont, Brushes.LightGray, x + 8, y);
            g.DrawString(valueText, _bodyFont, Brushes.White, x + 160, y);

            // Progress bar
            int barX = x + 8, barW = w - 16, barH = 10;
            y += 18;
            using (var br = new SolidBrush(Color.FromArgb(60, 60, 60)))
                g.FillRectangle(br, barX, y, barW, barH);
            if (fillPct > 0f)
                using (var br = new SolidBrush(barColor))
                    g.FillRectangle(br, barX, y, (int)(barW * Math.Min(1f, fillPct)), barH);
            g.DrawRectangle(Pens.DimGray, barX, y, barW, barH);
            y += 18;
        }

        /// <summary>Draws a simple label: value pair on one line.</summary>
        private static void DrawLabelValue(Graphics g, int x, ref int y,
            string label, string value, Brush valueBrush)
        {
            g.DrawString($"{label}:", _bodyFont, Brushes.LightGray, x + 8, y);
            g.DrawString(value, _bodyFont, valueBrush, x + 180, y);
            y += 22;
        }

        /// <summary>Draws a collected-item entry with icon, count, and description.</summary>
        private void DrawItemEntry(Graphics g, int x, ref int y, int w,
            string name, int count, string description, Color iconColor)
        {
            // Small coloured icon
            using (var br = new SolidBrush(iconColor))
                g.FillEllipse(br, x + 8, y + 2, 14, 14);
            using (var pen = new Pen(Color.FromArgb(180, Color.White), 1))
                g.DrawEllipse(pen, x + 8, y + 2, 14, 14);

            // Name + count
            string countStr = count >= 0 ? $"  x{count}" : "";
            g.DrawString($"{name}{countStr}", _headerFont, Brushes.White, x + 28, y);
            y += 20;
            g.DrawString(description, _bodyFont, Brushes.DimGray, x + 28, y);
            y += 24;
        }

        // Static font reference for DrawLabelValue
        private static readonly Font _bodyFontStatic = _bodyFont;
    }
}
