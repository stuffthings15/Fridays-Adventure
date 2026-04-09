// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 9: UI Programmer
// Feature: Save Game Slot Picker
// Purpose: Lets the player choose one of 3 save slots to write current
//          runtime progress into, accessible from the Options menu.
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
    /// Three-slot save-file picker that writes the current game state to the
    /// selected slot.  Mirrors <see cref="SaveSlotScene"/> visually but
    /// performs a save instead of a load.
    /// </summary>
    /// <remarks>PHASE 2 - Team 9: UI Programmer — Save Game slot picker</remarks>
    public sealed class SaveGameSlotScene : Scene
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const int SlotCount = 3;

        // ── State ─────────────────────────────────────────────────────────────
        private int  _selected    = 0;
        private bool _confirmSave = false;
        private float _anim;

        // ── Slot preview data ─────────────────────────────────────────────────
        private SaveData[] _previews;

        // ── Click targets ─────────────────────────────────────────────────────
        private readonly Rectangle[] _slotRects = new Rectangle[SlotCount];
        private Rectangle _backBtn;
        private Rectangle _confirmYesBtn;
        private Rectangle _confirmNoBtn;

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font _titleFont = new Font("Courier New", 24, FontStyle.Bold);
        private static readonly Font _slotFont  = new Font("Courier New", 13, FontStyle.Bold);
        private static readonly Font _infoFont  = new Font("Courier New", 10);
        private static readonly Font _btnFont   = new Font("Courier New", 12, FontStyle.Bold);
        private static readonly Font _hintFont  = new Font("Courier New", 9);

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>Loads slot previews and highlights the currently active slot.</summary>
        public override void OnEnter()
        {
            // Load preview data for all 3 slots so the player sees what each contains
            _previews = new SaveData[SlotCount];
            for (int i = 0; i < SlotCount; i++)
                _previews[i] = SaveData.LoadSlot(i);

            // Default cursor to the slot the player is already using
            _selected = Math.Max(0, Math.Min(SlotCount - 1, Game.Instance.SaveSlot));
        }

        public override void OnExit() { }

        // ── Update ────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            _anim += dt;
            var input = Game.Instance.Input;

            // Overwrite confirmation sub-state
            if (_confirmSave)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.Y))
                    ExecuteSave(_selected);
                if (input.IsPressed(System.Windows.Forms.Keys.N) ||
                    input.IsPressed(System.Windows.Forms.Keys.Escape))
                    _confirmSave = false;
                return;
            }

            // Keyboard navigation between the 3 slots
            if (input.IsPressed(System.Windows.Forms.Keys.Up))
                _selected = Math.Max(0, _selected - 1);
            if (input.IsPressed(System.Windows.Forms.Keys.Down))
                _selected = Math.Min(SlotCount - 1, _selected + 1);

            // Confirm with Enter / F (interact)
            if (input.InteractPressed || input.JumpPressed)
                BeginSave(_selected);

            // Escape goes back to options
            if (input.PausePressed)
                Game.Instance.Scenes.Pop();
        }

        // ── Click handling ────────────────────────────────────────────────────

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_backBtn.Contains(p)) { Game.Instance.Scenes.Pop(); return; }

            if (_confirmSave)
            {
                if (_confirmYesBtn.Contains(p)) { ExecuteSave(_selected); return; }
                // Click anywhere else cancels the prompt
                _confirmSave = false;
                return;
            }

            // Click a slot card to select and begin save
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slotRects[i].Contains(p))
                {
                    _selected = i;
                    BeginSave(i);
                    return;
                }
            }
        }

        // ── Save logic ───────────────────────────────────────────────────────

        /// <summary>
        /// Starts the save process.  If the target slot already contains
        /// meaningful progress, the player sees an overwrite confirmation.
        /// </summary>
        private void BeginSave(int slot)
        {
            // A slot is "occupied" when it has data beyond the starting point
            bool occupied = _previews[slot] != null &&
                            !string.IsNullOrEmpty(_previews[slot].CurrentNodeId) &&
                            _previews[slot].CurrentNodeId != "start";

            if (occupied)
                _confirmSave = true;   // Show "Overwrite?" dialog
            else
                ExecuteSave(slot);     // Empty slot — save immediately
        }

        /// <summary>
        /// Syncs all runtime state and writes the save file to the chosen slot.
        /// Also switches the active slot so future auto-saves target the same file.
        /// </summary>
        private void ExecuteSave(int slot)
        {
            try
            {
                // Sync current runtime values into the SaveData object
                Game.Instance.SyncRuntimeToSaveData();

                // Write to the slot-specific file path
                string slotPath = SaveData.SavePathForSlot(slot);
                Game.Instance.Save.SaveToPath(slotPath);

                // Switch the active slot so future auto-saves go to the same file
                if (Game.Instance.SaveSlot != slot)
                    Game.Instance.SwitchSaveSlot(slot);

                // Refresh the preview so the UI reflects the new data
                _previews[slot] = SaveData.LoadSlot(slot);

                _confirmSave = false;
                SMB3Hud.ShowToast($"Saved to Slot {slot + 1}!");
                System.Diagnostics.Debug.WriteLine($"[SaveGameSlotScene] Saved to slot {slot}.");

                // Return to the options menu
                Game.Instance.Scenes.Pop();
            }
            catch (Exception ex)
            {
                _confirmSave = false;
                SMB3Hud.ShowToast("Save failed!");
                System.Diagnostics.Debug.WriteLine($"[SaveGameSlotScene] Save failed: {ex.Message}");
            }
        }

        // ── Draw ──────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            DrawBackground(g, W, H);
            DrawTitle(g, W);
            DrawSlots(g, W, H);
            DrawButtons(g, W, H);
            if (_confirmSave) DrawConfirmOverlay(g, W, H);
            DrawDevMenuButton(g);
        }

        /// <summary>Draws a starry deep-blue background matching SaveSlotScene.</summary>
        private void DrawBackground(Graphics g, int W, int H)
        {
            // Dark blue gradient matching the load-slot screen
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(20, 30, 80), Color.FromArgb(5, 10, 40), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // Star dots for visual flavour
            var rng = new Random(99);
            using (var br = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                for (int i = 0; i < 60; i++)
                    g.FillEllipse(br, rng.Next(W), rng.Next(H), 2, 2);
        }

        /// <summary>Draws the centred "SAVE GAME" title and hint subtitle.</summary>
        private void DrawTitle(Graphics g, int W)
        {
            // Title
            const string title = "SAVE GAME";
            var sz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.LimeGreen, (W - sz.Width) / 2f, 20);

            // Subtitle hint
            const string hint = "Select a slot to save your progress.";
            var hz = g.MeasureString(hint, _hintFont);
            g.DrawString(hint, _hintFont, Brushes.LightGray, (W - hz.Width) / 2f, 55);
        }

        /// <summary>Draws the 3 save-slot cards with previews and selection highlight.</summary>
        private void DrawSlots(Graphics g, int W, int H)
        {
            int sw = (int)(W * 0.62f);
            int sh = 82;
            int sx = (W - sw) / 2;
            int startY = 85;
            int spacing = 18;

            for (int i = 0; i < SlotCount; i++)
            {
                int sy = startY + i * (sh + spacing);
                _slotRects[i] = new Rectangle(sx, sy, sw, sh);

                bool sel = _selected == i;
                bool isActiveSlot = Game.Instance.SaveSlot == i;

                // Selection glow pulse (animated)
                if (sel)
                {
                    float pulse = (float)(Math.Sin(_anim * 5) * 0.4 + 0.6);
                    using (var br = new SolidBrush(Color.FromArgb((int)(80 * pulse), Color.LimeGreen)))
                        g.FillRectangle(br, sx - 4, sy - 4, sw + 8, sh + 8);
                }

                // Slot card background
                Color slotBg = sel ? Color.FromArgb(40, 90, 50)
                                   : Color.FromArgb(25, 35, 70);
                using (var br = new SolidBrush(slotBg))
                    g.FillRectangle(br, _slotRects[i]);
                using (var pen = new Pen(sel ? Color.LimeGreen : Color.FromArgb(60, 80, 140), 2))
                    g.DrawRectangle(pen, _slotRects[i]);

                // Slot label — mark the active slot with a star
                string slotLabel = isActiveSlot
                    ? $"\u2605 SLOT {i + 1}  (Active)"
                    : $"SLOT {i + 1}";
                g.DrawString(slotLabel, _slotFont,
                    sel ? Brushes.LimeGreen : Brushes.LightGray,
                    sx + 12, sy + 10);

                // Preview of what's stored in this slot
                DrawSlotPreview(g, _previews[i], sx + 12, sy + 34, sw - 24);

                // Bouncing arrow on the selected slot
                if (sel)
                {
                    float arrow = (float)(Math.Sin(_anim * 6) * 4);
                    using (var br = new SolidBrush(Color.LimeGreen))
                        g.FillPolygon(br, new PointF[]
                        {
                            new PointF(sx - 20 + arrow, sy + sh / 2f),
                            new PointF(sx - 8 + arrow,  sy + sh / 2f - 8),
                            new PointF(sx - 8 + arrow,  sy + sh / 2f + 8)
                        });
                }
            }
        }

        /// <summary>
        /// Renders a compact preview of the data stored in a save slot.
        /// Shows world, lives, and berry count — or "EMPTY" for blank slots.
        /// </summary>
        private void DrawSlotPreview(Graphics g, SaveData data, float x, float y, float w)
        {
            // An empty or missing save file shows "[ EMPTY ]"
            if (data == null || string.IsNullOrEmpty(data.CurrentNodeId))
            {
                g.DrawString("[ EMPTY ]", _infoFont, Brushes.DarkGray, x, y);
                return;
            }

            // Show summary stats from the slot's persisted data
            int berries = data.PlayerBounty;
            int world   = Math.Max(1, data.GetInt("runtime.world", 1));
            int lives   = Math.Max(1, data.GetInt("runtime.lives", 3));

            g.DrawString($"World {world}   \u2665\u00D7{lives}   Berries: {berries:N0}",
                _infoFont, Brushes.LightCyan, x, y);
        }

        /// <summary>Draws the Back button and keyboard hints at the bottom.</summary>
        private void DrawButtons(Graphics g, int W, int H)
        {
            int bw = 130, bh = 32;
            _backBtn = new Rectangle(W / 2 - bw / 2, H - 60, bw, bh);
            DrawBtn(g, _backBtn, "\u2190 BACK", Brushes.LightGray);

            // Keyboard hint bar
            g.DrawString("Up/Down Select   Enter Save   Esc Cancel",
                _hintFont, Brushes.DimGray, W / 2 - 160, H - 22);
        }

        /// <summary>Helper to draw a small labelled button rectangle.</summary>
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

        /// <summary>Draws a centred "Overwrite?" confirmation dialog with Yes/No buttons.</summary>
        private void DrawConfirmOverlay(Graphics g, int W, int H)
        {
            // Darken the background
            using (var br = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            // Warning text
            string msg = $"Overwrite Slot {_selected + 1}?";
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

            // Yes button (green — confirm save)
            using (var br = new SolidBrush(Color.FromArgb(30, 100, 30)))
                g.FillRectangle(br, _confirmYesBtn);
            using (var pen = new Pen(Color.LimeGreen, 2))
                g.DrawRectangle(pen, _confirmYesBtn);
            var ysz = g.MeasureString("YES (Y)", _btnFont);
            g.DrawString("YES (Y)", _btnFont, Brushes.LimeGreen,
                _confirmYesBtn.X + (_confirmYesBtn.Width  - ysz.Width)  / 2f,
                _confirmYesBtn.Y + (_confirmYesBtn.Height - ysz.Height) / 2f);

            // No button (red — cancel)
            using (var br = new SolidBrush(Color.FromArgb(100, 30, 30)))
                g.FillRectangle(br, _confirmNoBtn);
            using (var pen = new Pen(Color.IndianRed, 2))
                g.DrawRectangle(pen, _confirmNoBtn);
            var nsz = g.MeasureString("NO (N)", _btnFont);
            g.DrawString("NO (N)", _btnFont, Brushes.IndianRed,
                _confirmNoBtn.X + (_confirmNoBtn.Width  - nsz.Width)  / 2f,
                _confirmNoBtn.Y + (_confirmNoBtn.Height - nsz.Height) / 2f);
        }
    }
}
