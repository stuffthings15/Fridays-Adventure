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

        public override void OnEnter() { }
        public override void OnExit()  { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            // Close inventory on Esc, I, or Enter
            if (input.PausePressed || input.InteractPressed ||
                input.IsPressed(System.Windows.Forms.Keys.I))
                Game.Instance.Scenes.Pop();
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            // Click anywhere to close
            Game.Instance.Scenes.Pop();
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
            g.DrawString(title, _titleFont, Brushes.Gold, (W - tsz.Width) / 2f, 16);

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

            // Health Pickups (tracked indirectly — show description)
            DrawItemEntry(g, rightX, ref ry, colW,
                "Health Pickups", -1,
                "Red cross items — restores 30 HP", Color.FromArgb(220, 55, 55));

            // Ice Reserve
            DrawItemEntry(g, rightX, ref ry, colW,
                "ICE Power", -1,
                "Regenerates over time — powers abilities", Color.FromArgb(180, 220, 255));

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

            // ── Control hints ────────────────────────────────────────────────
            g.DrawString("[Esc / I / Enter] Close Inventory",
                _hintFont, Brushes.DimGray, W / 2 - 130, H - 24);

            DrawDevMenuButton(g);
        }

        // ── Drawing helpers ──────────────────────────────────────────────────

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
