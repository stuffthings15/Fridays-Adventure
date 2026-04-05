using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    public sealed class TitleScene : Scene
    {
        private Bitmap    _bg;
        private float     _timer;
        private float     _promptBlink;
        private bool      _showPrompt = true;

        // Button rectangles — computed in Draw, used in HandleClick
        private Rectangle _optionsBtn;
        private Rectangle _exitBtn;
        private Rectangle _scoresBtn;
        private Rectangle _saveBtn;
        private Rectangle _loadBtn;

        // Name entry — shown when player has not entered a name yet
        private bool   _nameActive;
        private string _nameInput = "";
        private float  _nameCursor;
        // Both "Luffy" and "Loofy" (case-insensitive) unlock the dev menu.
        private static readonly string[] SecretPasswords = { "Luffy", "Loofy" };

        public override void OnEnter()
        {
            // Background candidates — character art should not be used as backgrounds.
            string[] candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites", "bg_title.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites", "bg_deck.jpg"),
            };
            foreach (string c in candidates)
                if (File.Exists(c)) { _bg = new Bitmap(c); break; }

            // Continue lyrical theme music started in LoadingScene; ContinueOrPlay avoids a restart.
            Game.Instance.Audio.PlayTheme();

            // If no name has been set yet, show the name entry prompt
            if (string.IsNullOrEmpty(Game.Instance.PlayerName))
                _nameActive = true;
        }

        public override void OnExit()
        {
            _bg?.Dispose();
            _bg = null;
        }

        public override void Update(float dt)
        {
            _timer       += dt;
            _promptBlink += dt;
            if (_promptBlink >= 0.55f) { _showPrompt = !_showPrompt; _promptBlink = 0; }

            var input = Game.Instance.Input;

            // ── Name entry box ────────────────────────────────────────────────
            // PHASE 2 - Team 9: UI Programmer - Allow dev menu access during name entry
            if (_nameActive && !Game.Instance.GodMode)  // Only show name entry if dev mode NOT activated
            {
                _nameCursor += dt;

                string typed = input.ConsumeTyped();
                if (typed.Length > 0)
                {
                    _nameInput += typed;
                    if (_nameInput.Length > 16) _nameInput = _nameInput.Substring(0, 16);
                }

                if (input.IsPressed(System.Windows.Forms.Keys.Back) && _nameInput.Length > 0)
                    _nameInput = _nameInput.Substring(0, _nameInput.Length - 1);

                if (input.IsPressed(System.Windows.Forms.Keys.Return) && _nameInput.Length > 0)
                {
                    // Check all accepted secret passwords (case-insensitive).
                    bool isSecret = false;
                    foreach (string pw in SecretPasswords)
                        if (string.Equals(_nameInput, pw, StringComparison.OrdinalIgnoreCase))
                        { isSecret = true; break; }

                    if (isSecret)
                    {
                        Game.Instance.GodMode   = true;
                        Game.Instance.PlayerName = _nameInput;
                        _nameActive = false;
                        Game.Instance.Scenes.Push(new DevMenuScene());
                    }
                    else
                    {
                        Game.Instance.PlayerName = _nameInput;
                        _nameActive = false;
                    }
                    return;
                }

                return; // block regular navigation while entering name
            }

            // ── Regular navigation ────────────────────────────────────────────
            // Route through Save Slot selection before starting the game so the
            // player can pick/continue a save file (SMB3 file select style).
            if (input.InteractPressed || input.AttackPressed || input.JumpPressed)
                Game.Instance.Scenes.Replace(new SaveSlotScene());

            // Main-menu save/load shortcuts
            if (input.IsPressed(System.Windows.Forms.Keys.F5)) SaveGameJson();
            if (input.IsPressed(System.Windows.Forms.Keys.F9)) LoadGameJson();

            if (input.IsPressed(System.Windows.Forms.Keys.L))
                Engine.Game.RequestOpenLogbook();

            if (input.PausePressed)
                Game.Instance.Scenes.Push(new OptionsScene());
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_loadBtn.Contains(p))   { LoadGameJson(); return; }
            if (_saveBtn.Contains(p))   { SaveGameJson(); return; }
            if (_optionsBtn.Contains(p)) Game.Instance.Scenes.Push(new OptionsScene());
            if (_exitBtn.Contains(p))    Game.RequestClose();
            if (_scoresBtn.Contains(p))  Game.Instance.Scenes.Push(new HighScoreScene(0, 0, isNewEntry: false));
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Team 10 (Engine Programmer) — pixel-art rendering settings
            EngineFeatures.ApplyPixelArtSettings(g);

            if (_bg != null)
                g.DrawImage(_bg, 0, 0, W, H);
            else
            {
                // Team 12 (Art Director) — world sky color for title screen (World 1)
                Color skyTop = ArtDirectorFeatures.GetWorldSkyColor(1);
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                    skyTop, Color.FromArgb(20, 60, 120), 90f))
                    g.FillRectangle(br, 0, 0, W, H);
            }

            // Team 14 (Environment Artist) — star field for night scenes
            EnvironmentFeatures.DrawStarField(g, W, H);

            // Team 14 (Environment Artist) — parallax cloud layer
            EnvironmentFeatures.DrawParallaxClouds(g, W, H, _timer * 20f);

            // ── Title banner ──────────────────────────────────────────────────
            using (var br = new SolidBrush(Color.FromArgb(190, 255, 255, 255)))
                g.FillRectangle(br, 0, (int)(H * 0.10f), W, 190);

            float taglineY;
            using (var f = new Font("Courier New", 38, FontStyle.Bold))
            {
                const string title = "Miss Friday's Adventure Part II";
                SizeF  sz = g.MeasureString(title, f);
                float titleY = H * 0.12f;
                g.DrawString(title, f, Brushes.Black, (W - sz.Width) / 2f, titleY);
                taglineY = titleY + sz.Height;
            }

            using (var f = new Font("Courier New", 18, FontStyle.Bold | FontStyle.Italic))
            {
                const string sub = "Tide of the Lost";
                SizeF sz = g.MeasureString(sub, f);
                using (var br = new SolidBrush(Color.FromArgb(210, Color.DarkCyan)))
                    g.DrawString(sub, f, br, (W - sz.Width) / 2f, taglineY);
                taglineY += sz.Height + 2f;
            }

            using (var f = new Font("Courier New", 13, FontStyle.Bold))
            {
                const string tag = "Ice-Ice Fruit  \u2022  The Sea Serpent  \u2022  The Grand Line";
                SizeF sz = g.MeasureString(tag, f);
                g.DrawString(tag, f, Brushes.DarkSlateGray, (W - sz.Width) / 2f, taglineY);
            }

            // ── Press-to-start prompt ─────────────────────────────────────────
            if (_showPrompt)
            {
                using (var br = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
                    g.FillRectangle(br, 0, (int)(H * 0.64f), W, 54);
                using (var f = new Font("Courier New", 22, FontStyle.Bold))
                {
                    const string s = "Press  ENTER  or  Z  to  set  sail";
                    SizeF sz = g.MeasureString(s, f);
                    g.DrawString(s, f, Brushes.Yellow, (W - sz.Width) / 2f, H * 0.65f);
                }
            }

            // ── Main action buttons ───────────────────────────────────────────
            const int btnW = 150, btnH = 46, gap = 12;
            int totalW = btnW * 5 + gap * 4;
            int startX = (W - totalW) / 2;
            int btnY = (int)(H * 0.75f);

            _loadBtn    = new Rectangle(startX + (btnW + gap) * 0, btnY, btnW, btnH);
            _saveBtn    = new Rectangle(startX + (btnW + gap) * 1, btnY, btnW, btnH);
            _optionsBtn = new Rectangle(startX + (btnW + gap) * 2, btnY, btnW, btnH);
            _scoresBtn  = new Rectangle(startX + (btnW + gap) * 3, btnY, btnW, btnH);
            _exitBtn    = new Rectangle(startX + (btnW + gap) * 4, btnY, btnW, btnH);

            DrawButton(g, _loadBtn,    "LOAD",    Color.FromArgb(30, 110, 60));
            DrawButton(g, _saveBtn,    "SAVE",    Color.FromArgb(30, 80, 110));
            DrawButton(g, _optionsBtn, "OPTIONS", Color.FromArgb(40, 80, 140));
            DrawButton(g, _scoresBtn,  "SCORES",  Color.FromArgb(120, 100, 20));
            DrawButton(g, _exitBtn,    "EXIT",    Color.FromArgb(120, 30, 30));

            // ── High scores panel ─────────────────────────────────────────────
            DrawHighScoresPanel(g, W, H);

            // ── Controls panel ────────────────────────────────────────────────
            const int panelH = 80;
            using (var br = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                g.FillRectangle(br, 0, H - panelH, W, panelH);
            g.DrawLine(Pens.White, 0, H - panelH, W, H - panelH);

            using (var f = new Font("Courier New", 13, FontStyle.Bold))
            {
                g.DrawString("Move: WASD / Arrows   Jump: Space / W   Attack: Z   Dodge: X",
                             f, Brushes.White, 14, H - panelH + 10);
                g.DrawString("Ice Wall: Q   Freeze: E   Interact: F / Enter   Pause: Esc   Load: F9   Save: F5",
                             f, Brushes.LightCyan, 14, H - panelH + 38);
            }

            DrawDevMenuButton(g);

            // Team 15 (UI/UX Artist) — blinking "PRESS START" text
            if (_showPrompt)
                UIArtFeatures.DrawPressStart(g, W, H, _timer, "Press  ENTER  or  Z  to  set  sail");

            // Team 11 (Build Engineer) — version stamp in debug mode
            BuildEngineerFeatures.DrawVersionStamp(g, W, H);

            // Team 12 (Art Director) — scanlines (toggleable)
            ArtDirectorFeatures.DrawScanlines(g, W, H);

            // Team 12 (Art Director) — vignette
            ArtDirectorFeatures.DrawVignette(g, W, H);

            if (_nameActive) DrawNameEntryBox(g, W, H);
        }

        private void DrawNameEntryBox(Graphics g, int W, int H)
        {
            // Dim background
            using (var br = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            const int bw = 420, bh = 110;
            int bx = (W - bw) / 2, by = (int)(H * 0.38f);

            using (var br = new SolidBrush(Color.FromArgb(240, 10, 10, 40)))
                g.FillRectangle(br, bx, by, bw, bh);
            using (var pen = new Pen(Color.Gold, 2))
                g.DrawRectangle(pen, bx, by, bw, bh);

            using (var f = new Font("Courier New", 14, FontStyle.Bold))
                g.DrawString("Enter your name, pirate:", f, Brushes.Gold, bx + 20, by + 14);

            string cursor  = (int)(_nameCursor / 0.45f) % 2 == 0 ? "|" : " ";
            string display = _nameInput + cursor;
            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString(display, f, Brushes.White, bx + 20, by + 46);

            using (var f = new Font("Courier New", 9))
                g.DrawString("[Enter] Confirm   [Backspace] Delete", f, Brushes.DimGray, bx + 20, by + 86);
        }

        private static void DrawButton(Graphics g, Rectangle r, string label, Color bg)
        {
            using (var br = new SolidBrush(bg))
                g.FillRectangle(br, r);
            g.DrawRectangle(Pens.White, r);
            using (var f = new Font("Courier New", 14, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.White,
                    r.X + (r.Width  - sz.Width)  / 2f,
                    r.Y + (r.Height - sz.Height) / 2f);
            }
        }

        /// <summary>
        /// Saves current game state to JSON from the main menu.
        /// </summary>
        private static void SaveGameJson()
        {
            try
            {
                Game.Instance.SyncRuntimeToSaveData();
                Game.Instance.Save.SaveJson();
                DebugLogger.LogInfo("TitleScene.SaveGameJson", $"Saved JSON: {SaveData.JsonSavePath}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("TitleScene.SaveGameJson", ex);
            }
        }

        /// <summary>
        /// Loads game state from JSON from the main menu.
        /// </summary>
        private static void LoadGameJson()
        {
            try
            {
                var loaded = SaveData.LoadJson();
                Game.Instance.ApplySaveData(loaded);
                DebugLogger.LogInfo("TitleScene.LoadGameJson", $"Loaded JSON: {SaveData.JsonSavePath}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("TitleScene.LoadGameJson", ex);
            }
        }

        private void DrawHighScoresPanel(Graphics g, int W, int H)
        {
            var scores = Game.Instance.Save.GetTopScores(5);
            if (scores.Count == 0) return;

            int panelW = 340;
            int panelH = 28 + scores.Count * 22;
            int px = W - panelW - 16;
            int py = (int)(H * 0.34f);

            using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(br, px, py, panelW, panelH);
            g.DrawRectangle(Pens.Gold, px, py, panelW, panelH);

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("TOP SCORES", f, Brushes.Gold, px + 8, py + 4);

            using (var f = new Font("Courier New", 10))
            {
                for (int i = 0; i < scores.Count; i++)
                {
                    int ry = py + 26 + i * 22;
                    g.DrawString($"#{i + 1}", f, Brushes.Gold, px + 8, ry);
                    g.DrawString(scores[i].Name, f, Brushes.White, px + 44, ry);
                    string sc = $"{scores[i].Score:N0}";
                    SizeF ssz = g.MeasureString(sc, f);
                    g.DrawString(sc, f, Brushes.Yellow, px + panelW - ssz.Width - 10, ry);
                }
            }
        }
    }
}
