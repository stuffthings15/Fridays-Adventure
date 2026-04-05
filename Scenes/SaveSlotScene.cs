// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 – Multi-Team Implementation
// Scenes/SaveSlotScene.cs
// Purpose: SMB3-style 3-slot save file selection screen.
// ────────────────────────────────────────────────────────────────────────────
// Team 1  (Game Director)  – Idea 13: 3-slot save file management
// Team 1  (Game Director)  – Idea 14: New game slot creation
// Team 1  (Game Director)  – Idea 15: Clear / delete save slot option
// Team 9  (UI Programmer)  – Idea 1:  Keyboard & mouse slot selection
// Team 9  (UI Programmer)  – Idea 2:  Slot preview (player name, world, berries)
// Team 9  (UI Programmer)  – Idea 3:  Animated cursor / selection highlight
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Three-slot save file selection screen (SMB3-style).
    /// Player picks a slot to load or start a new game.
    ///
    /// Team 1 (Game Director) — Ideas 13–15.
    /// Team 9 (UI Programmer) — Ideas 1–3.
    /// </summary>
    public sealed class SaveSlotScene : Scene
    {
        // ── Layout ────────────────────────────────────────────────────────────
        private const int SlotCount = 3;
        private int  _selected      = 0;   // highlighted slot
        private bool _confirmClear  = false;
        private float _anim;

        // ── Slot data previews (Team 1 — Idea 13) ────────────────────────────
        private SaveData[] _previews;

        // ── Click targets ─────────────────────────────────────────────────────
        private Rectangle[] _slotRects = new Rectangle[SlotCount];
        private Rectangle   _deleteBtn;
        private Rectangle   _backBtn;
        private Rectangle   _confirmYesBtn;
        private Rectangle   _confirmNoBtn;

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font _titleFont = new Font("Courier New", 24, FontStyle.Bold);
        private static readonly Font _slotFont  = new Font("Courier New", 13, FontStyle.Bold);
        private static readonly Font _infoFont  = new Font("Courier New", 10);
        private static readonly Font _btnFont   = new Font("Courier New", 12, FontStyle.Bold);

        public SaveSlotScene() { }

        public override void OnEnter()
        {
            // Load preview data for all 3 slots (Team 1 — Idea 13)
            _previews = new SaveData[SlotCount];
            for (int i = 0; i < SlotCount; i++)
                _previews[i] = SaveData.LoadSlot(i);
        }

        public override void OnExit() { }

        // ── Update ─────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            _anim += dt;

            var input = Game.Instance.Input;

            if (_confirmClear)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.Y))
                    ExecuteClear(_selected);
                if (input.IsPressed(System.Windows.Forms.Keys.N) ||
                    input.IsPressed(System.Windows.Forms.Keys.Escape))
                    _confirmClear = false;
                return;
            }

            // Keyboard navigation (Team 9 — Idea 1)
            if (input.IsPressed(System.Windows.Forms.Keys.Up))
                _selected = Math.Max(0, _selected - 1);
            if (input.IsPressed(System.Windows.Forms.Keys.Down))
                _selected = Math.Min(SlotCount - 1, _selected + 1);

            // Confirm selection — load / new game (Team 1 — Idea 14)
            if (input.InteractPressed || input.JumpPressed)
                LoadSlot(_selected);

            if (input.PausePressed)
                Game.Instance.Scenes.Pop();
        }

        // ── Click handling ─────────────────────────────────────────────────────

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_backBtn.Contains(p)) { Game.Instance.Scenes.Pop(); return; }

            if (_confirmClear)
            {
                // Click Yes to confirm delete, No (or anywhere else) to cancel.
                if (_confirmYesBtn.Contains(p))
                {
                    ExecuteClear(_selected);
                    return;
                }
                // No button or clicking outside dismisses the confirmation.
                _confirmClear = false;
                return;
            }

            for (int i = 0; i < SlotCount; i++)
            {
                if (_slotRects[i].Contains(p))
                {
                    _selected = i;
                    LoadSlot(i);
                    return;
                }
            }

            if (_deleteBtn.Contains(p))
                _confirmClear = true;
        }

        // ── Slot operations ────────────────────────────────────────────────────

        /// <summary>
        /// Loads the selected slot and transitions to the overworld (existing save)
        /// or to CharacterSelectScene (new save with no progress).
        /// Team 1 (Game Director) — Idea 14.
        /// </summary>
        private void LoadSlot(int slot)
        {
            Game.Instance.SwitchSaveSlot(slot);
            Game.Instance.ApplySaveData(Game.Instance.Save);

            // New game if no player name OR this slot has never completed character selection.
            // (Legacy saves without the marker will be routed once through character select.)
            bool isNewGame = string.IsNullOrEmpty(Game.Instance.PlayerName) ||
                             Game.Instance.Save.GetInt("runtime.characterSelected", 0) == 0;

            if (isNewGame)
            {
                // Clear stale data so the new run starts clean
                Game.Instance.PlayerName = "";
                Game.Instance.Scenes.Replace(new CharacterSelectScene());
            }
            else
            {
                // Existing save — jump straight back to the world map
                Game.Instance.Scenes.Replace(new OverworldScene());
            }
        }

        /// <summary>
        /// Clears (resets) the save file in the selected slot.
        /// Team 1 (Game Director) — Idea 15.
        /// </summary>
        private void ExecuteClear(int slot)
        {
            var blank = new SaveData();
            blank.Save();   // SaveData.Save writes to the default path; slot path handled below
            System.IO.File.WriteAllText(SaveData.SavePathForSlot(slot), "");
            _previews[slot] = new SaveData();
            _confirmClear   = false;
            SMB3Hud.ShowToast($"Slot {slot + 1} cleared.");
        }

        // ── Draw ───────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            DrawBackground(g, W, H);
            DrawTitle(g, W);
            DrawSlots(g, W, H);
            DrawButtons(g, W, H);
            if (_confirmClear) DrawConfirmOverlay(g, W, H);
            DrawDevMenuButton(g);
        }

        private void DrawBackground(Graphics g, int W, int H)
        {
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(20, 30, 80), Color.FromArgb(5, 10, 40), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Star dots
            var rng = new Random(42);
            using (var br = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                for (int i = 0; i < 60; i++)
                    g.FillEllipse(br, rng.Next(W), rng.Next(H), 2, 2);
        }

        private void DrawTitle(Graphics g, int W)
        {
            const string title = "SELECT FILE";
            var sz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.Gold, (W - sz.Width) / 2f, 20);
        }

        private void DrawSlots(Graphics g, int W, int H)
        {
            int sw = (int)(W * 0.62f);
            int sh = 82;
            int sx = (W - sw) / 2;
            int startY = 80;
            int spacing = 18;

            _deleteBtn = Rectangle.Empty;

            for (int i = 0; i < SlotCount; i++)
            {
                int sy = startY + i * (sh + spacing);
                _slotRects[i] = new Rectangle(sx, sy, sw, sh);

                bool sel = _selected == i;

                // Selection glow (Team 9 — Idea 3)
                if (sel)
                {
                    float pulse = (float)(Math.Sin(_anim * 5) * 0.4 + 0.6);
                    using (var br = new SolidBrush(Color.FromArgb((int)(80 * pulse), Color.Gold)))
                        g.FillRectangle(br, sx - 4, sy - 4, sw + 8, sh + 8);
                }

                // Slot background
                Color slotBg = sel
                    ? Color.FromArgb(50, 70, 140)
                    : Color.FromArgb(25, 35, 70);
                using (var br = new SolidBrush(slotBg))
                    g.FillRectangle(br, _slotRects[i]);
                using (var pen = new Pen(sel ? Color.Gold : Color.FromArgb(60, 80, 140), 2))
                    g.DrawRectangle(pen, _slotRects[i]);

                // Slot label
                string slotLabel = $"FILE {i + 1}";
                g.DrawString(slotLabel, _slotFont,
                    sel ? Brushes.Gold : Brushes.LightGray,
                    sx + 12, sy + 10);

                // Preview data (Team 9 — Idea 2)
                DrawSlotPreview(g, _previews[i], sx + 12, sy + 34, sw - 24);

                // Arrow indicator (Team 9 — Idea 3)
                if (sel)
                {
                    float arrow = (float)(Math.Sin(_anim * 6) * 4);
                    using (var br = new SolidBrush(Color.Gold))
                        g.FillPolygon(br, new PointF[]
                        {
                            new PointF(sx - 20 + arrow, sy + sh / 2f),
                            new PointF(sx - 8 + arrow,  sy + sh / 2f - 8),
                            new PointF(sx - 8 + arrow,  sy + sh / 2f + 8)
                        });

                    // Delete button shown next to the currently selected save slot.
                    _deleteBtn = new Rectangle(sx + sw + 10, sy + 24, 128, 32);
                    DrawBtn(g, _deleteBtn, "DELETE SAVE", Brushes.IndianRed);
                }
            }
        }

        private void DrawSlotPreview(Graphics g, SaveData data, float x, float y, float w)
        {
            if (data == null)
            {
                g.DrawString("[ NEW GAME ]", _infoFont, Brushes.LimeGreen, x, y);
                return;
            }

            // Player name, world, berries
            string name    = string.IsNullOrEmpty(data.CurrentNodeId) ? "New Game" : "Save File";
            int    berries = data.PlayerBounty;
            int    world   = Math.Max(1, data.GetInt("runtime.world", 1));
            int    lives   = Math.Max(1, data.GetInt("runtime.lives", 3));

            g.DrawString($"World {world}   ♥×{lives}   Berries: {berries:N0}",
                _infoFont, Brushes.LightCyan, x, y);
        }

        private void DrawButtons(Graphics g, int W, int H)
        {
            int bw = 130, bh = 32;
            _backBtn  = new Rectangle(W / 2 - bw / 2, H - 60, bw, bh);

            DrawBtn(g, _backBtn,  "← BACK",     Brushes.LightGray);
        }

        private void DrawBtn(Graphics g, Rectangle r, string label, Brush fg)
        {
            using (var br = new SolidBrush(Color.FromArgb(40, 50, 80)))
                g.FillRectangle(br, r);
            using (var pen = new Pen(Color.FromArgb(80, 100, 160), 1))
                g.DrawRectangle(pen, r);
            var sz = g.MeasureString(label, _btnFont);
            g.DrawString(label, _btnFont, fg,
                r.X + (r.Width  - sz.Width)  / 2f,
                r.Y + (r.Height - sz.Height) / 2f);
        }

        private void DrawConfirmOverlay(Graphics g, int W, int H)
        {
            // Darken background
            using (var br = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            // Prompt text
            string msg = $"Delete File {_selected + 1}?";
            var sz = g.MeasureString(msg, _slotFont);
            g.DrawString(msg, _slotFont, Brushes.OrangeRed,
                (W - sz.Width) / 2f, H / 2f - 40);

            // Yes / No buttons
            int bw = 120, bh = 36;
            int gap = 20;
            int cx = W / 2;
            int by = H / 2;

            _confirmYesBtn = new Rectangle(cx - bw - gap / 2, by, bw, bh);
            _confirmNoBtn  = new Rectangle(cx + gap / 2,       by, bw, bh);

            // Yes button (green)
            using (var br = new SolidBrush(Color.FromArgb(30, 100, 30)))
                g.FillRectangle(br, _confirmYesBtn);
            using (var pen = new Pen(Color.LimeGreen, 2))
                g.DrawRectangle(pen, _confirmYesBtn);
            var ysz = g.MeasureString("YES (Y)", _btnFont);
            g.DrawString("YES (Y)", _btnFont, Brushes.LimeGreen,
                _confirmYesBtn.X + (_confirmYesBtn.Width - ysz.Width) / 2f,
                _confirmYesBtn.Y + (_confirmYesBtn.Height - ysz.Height) / 2f);

            // No button (red)
            using (var br = new SolidBrush(Color.FromArgb(100, 30, 30)))
                g.FillRectangle(br, _confirmNoBtn);
            using (var pen = new Pen(Color.IndianRed, 2))
                g.DrawRectangle(pen, _confirmNoBtn);
            var nsz = g.MeasureString("NO (N)", _btnFont);
            g.DrawString("NO (N)", _btnFont, Brushes.IndianRed,
                _confirmNoBtn.X + (_confirmNoBtn.Width - nsz.Width) / 2f,
                _confirmNoBtn.Y + (_confirmNoBtn.Height - nsz.Height) / 2f);
        }
    }
}
