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

        // AFK idle timer — auto-launches demo mode after 60 s of no input
        private float     _idleTimer  = 0f;
        private const float IdleAutoDemo = 60f;

        // Button rectangles — computed in Draw, used in HandleClick
        private Rectangle _optionsBtn;
        private Rectangle _exitBtn;
        private Rectangle _scoresBtn;
        private Rectangle _saveBtn;
        private Rectangle _loadBtn;
        private Rectangle _demoBtn;
        // QA Bot Walkthrough button — launches the full automated QA test
        private Rectangle _qaWalkthroughBtn;
        // Text RPG mini-game button — opens the RPG embedded in the main window
        private Rectangle _textRpgBtn;
        // Video Demo Mode buttons — scripted auto-play showcases
        private Rectangle _videoDemoBtn;
        private Rectangle _rpgDemoBtn;
        // Start Game button — prominent entry point to begin Miss Friday's Adventure II
        private Rectangle _startBtn;
        // Dev Menu button — opens a password prompt to access the developer menu
        private Rectangle _devMenuBtn;
        // Neon Survivor mini-game button — launches the ported arcade game
        private Rectangle _neonSurvivorBtn;

        // Name entry — shown when player has not entered a name yet
        private bool   _nameActive;
        private string _nameInput = "";
        private float  _nameCursor;

        // Password entry — shown when the player clicks the DEV MENU button
        private bool   _passwordActive;
        private string _passwordInput = "";
        private float  _passwordCursor;
        private string _passwordError = "";
        // Both "Luffy" and "Loofy" (case-insensitive) unlock the dev menu.
        private static readonly string[] SecretPasswords = { "Luffy", "Loofy" };


        public override void OnEnter()
        {
            _idleTimer = 0f;   // reset AFK counter each time we return to the title
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

            // Name entry is NOT shown automatically — the player must click START GAME first
            _nameActive = false;
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

            // ── AFK idle → auto demo ──────────────────────────────────────────
            // Any key press resets the idle counter; after 60 s of silence the
            // demo launches automatically.  We check _held so held keys also count.
            bool anyInput = input.LeftHeld || input.RightHeld || input.UpHeld ||
                            input.DownHeld || input.JumpHeld  || input.AttackHeld ||
                            input.AnyMash;
            if (anyInput)
                _idleTimer = 0f;
            else
                _idleTimer += dt;

            if (_idleTimer >= IdleAutoDemo)
            {
                _idleTimer = 0f;
                Game.Instance.Scenes.Push(new DemoModeScene(autoStart: true));
                return;
            }

            // ── Password entry for Dev Menu ──────────────────────────────────
            // Shown when the player clicks the DEV MENU button on the title screen
            if (_passwordActive)
            {
                _passwordCursor += dt;

                string typed = input.ConsumeTyped();
                if (typed.Length > 0)
                {
                    _passwordInput += typed;
                    if (_passwordInput.Length > 16) _passwordInput = _passwordInput.Substring(0, 16);
                    _passwordError = ""; // clear error on new input
                }

                if (input.IsPressed(System.Windows.Forms.Keys.Back) && _passwordInput.Length > 0)
                {
                    _passwordInput = _passwordInput.Substring(0, _passwordInput.Length - 1);
                    _passwordError = "";
                }

                // Escape cancels the password prompt
                if (input.PausePressed)
                {
                    _passwordActive = false;
                    _passwordInput = "";
                    _passwordError = "";
                    return;
                }

                if (input.IsPressed(System.Windows.Forms.Keys.Return) && _passwordInput.Length > 0)
                {
                    // Validate against accepted passwords (case-insensitive)
                    bool valid = false;
                    foreach (string pw in SecretPasswords)
                        if (string.Equals(_passwordInput, pw, StringComparison.OrdinalIgnoreCase))
                        { valid = true; break; }

                    if (valid)
                    {
                        Game.Instance.GodMode = true;
                        _passwordActive = false;
                        _passwordInput = "";
                        _passwordError = "";
                        Game.Instance.Scenes.Push(new DevMenuScene());
                    }
                    else
                    {
                        _passwordError = "Incorrect password!";
                        _passwordInput = "";
                    }
                    return;
                }

                return; // block regular navigation while entering password
            }

            // ── Name entry box ────────────────────────────────────────────────
            // Always block gameplay hotkeys (I, L, Esc, etc.) while typing a name,
            // otherwise letters like "I" in "Curtis" trigger InventoryPressed.
            if (_nameActive)
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
                        // Name confirmed — proceed to save slot selection
                        Game.Instance.PlayerName = _nameInput;
                        _nameActive = false;
                        Game.Instance.Scenes.Replace(new SaveSlotScene());
                    }
                    return;
                }

                return; // block regular navigation while entering name
            }

            // ── Regular navigation ────────────────────────────────────────────
            // START GAME is handled via the on-screen button click.
            // Keyboard shortcut: Enter also triggers start.
            if (input.IsPressed(System.Windows.Forms.Keys.Return))
                StartGame();

            // Main-menu save/load shortcuts
            if (input.IsPressed(System.Windows.Forms.Keys.F5)) SaveGameJson();
            if (input.IsPressed(System.Windows.Forms.Keys.F9)) LoadGameJson();

            if (input.IsPressed(System.Windows.Forms.Keys.L))
                Engine.Game.RequestOpenLogbook();

            // Inventory is only accessible from gameplay map scenes,
            // not the title screen — handled globally in Game.cs.

            if (input.PausePressed)
                Game.Instance.Scenes.Push(new OptionsScene());
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            // START GAME button — the primary entry point
            if (_startBtn.Contains(p))  { StartGame(); return; }
            if (_loadBtn.Contains(p))   { LoadGameJson(); return; }
            if (_saveBtn.Contains(p))   { SaveGameJson(); return; }
            if (_optionsBtn.Contains(p)) Game.Instance.Scenes.Push(new OptionsScene());
            if (_demoBtn.Contains(p))    Game.Instance.Scenes.Push(new DemoModeScene());
            if (_textRpgBtn.Contains(p)) LaunchTextRPG();
            if (_videoDemoBtn.Contains(p)) Game.Instance.Scenes.Push(new VideoDemoScene());
            if (_rpgDemoBtn.Contains(p))   LaunchTextRPGDemo();
            if (_qaWalkthroughBtn.Contains(p)) Game.Instance.Scenes.Push(new QABotWalkthroughScene());
            if (_neonSurvivorBtn.Contains(p)) Game.Instance.Scenes.Push(new NeonSurvivorScene());
            if (_exitBtn.Contains(p))    Game.RequestClose();
            if (_scoresBtn.Contains(p))  Game.Instance.Scenes.Push(new HighScoreScene(0, 0, isNewEntry: false));
            // DEV MENU button — open password prompt
            if (_devMenuBtn.Contains(p))
            {
                _passwordActive = true;
                _passwordInput = "";
                _passwordError = "";
                _passwordCursor = 0f;
            }
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

            // ── Main action buttons ───────────────────────────────────────────
            const int btnW = 150, btnH = 46, gap = 12;
            int totalW = btnW * 5 + gap * 4;
            int startX = (W - totalW) / 2;
            int btnY = (int)(H * 0.75f);

            // ── START GAME button — prominent, centered above the main row ──
            int startW = 320, startH = 56;
            int startX2 = (W - startW) / 2;
            int startY2 = btnY - startH - 20;
            _startBtn = new Rectangle(startX2, startY2, startW, startH);
            DrawButton(g, _startBtn, "\u25B6 START GAME", Color.FromArgb(20, 120, 50));

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

            // ── Secondary buttons (below main row) ──
            // Row 1: WATCH DEMO + TEXT RPG (gameplay launchers)
            int secBtnW = 180;
            int secGap  = 20;
            int secTotalW = secBtnW * 2 + secGap;
            int secX = (W - secTotalW) / 2;
            int secY = btnY + btnH + 12;
            _demoBtn    = new Rectangle(secX, secY, secBtnW, btnH);
            _textRpgBtn = new Rectangle(secX + secBtnW + secGap, secY, secBtnW, btnH);
            DrawButton(g, _demoBtn,    "WATCH DEMO",    Color.FromArgb(100, 150, 50));
            DrawButton(g, _textRpgBtn, "\u2694 TEXT RPG", Color.FromArgb(90, 60, 130));

            // Row 2: VIDEO DEMO MODE buttons — scripted auto-play showcases
            int vidY = secY + btnH + 8;
            int vidBtnW = 220;
            int vidGap  = 16;
            int vidTotalW = vidBtnW * 2 + vidGap;
            int vidX = (W - vidTotalW) / 2;
            _videoDemoBtn = new Rectangle(vidX, vidY, vidBtnW, 36);
            _rpgDemoBtn   = new Rectangle(vidX + vidBtnW + vidGap, vidY, vidBtnW, 36);
            DrawButton(g, _videoDemoBtn, "\u25B6 VIDEO DEMO: GAME",    Color.FromArgb(140, 80, 20));
            DrawButton(g, _rpgDemoBtn,   "\u25B6 VIDEO DEMO: RPG",     Color.FromArgb(140, 80, 20));

            // Row 3: Neon Survivor + QA Bot Walkthrough
            int row3Y = vidY + 36 + 8;
            int row3BtnW = 220;
            int row3Gap = 16;
            int row3TotalW = row3BtnW * 2 + row3Gap;
            int row3X = (W - row3TotalW) / 2;
            _neonSurvivorBtn  = new Rectangle(row3X, row3Y, row3BtnW, 36);
            _qaWalkthroughBtn = new Rectangle(row3X + row3BtnW + row3Gap, row3Y, row3BtnW, 36);
            DrawButton(g, _neonSurvivorBtn,  "\u26A1 NEON SURVIVOR", Color.FromArgb(0, 80, 60));
            DrawButton(g, _qaWalkthroughBtn, "\u2699 QA BOT WALKTHROUGH", Color.FromArgb(20, 100, 120));

            // AFK idle countdown — shown in the last 15 seconds before auto-demo
            if (_idleTimer >= IdleAutoDemo - 15f)
            {
                float remaining = IdleAutoDemo - _idleTimer;
                string hint = $"Demo in {remaining:F0}s — press any key to cancel";
                using (var f = new Font("Courier New", 10, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(hint, f);
                    int hx = (int)((W - sz.Width) / 2f);
                    int hy = row3Y + 36 + 6;
                    using (var br = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                        g.FillRectangle(br, hx - 6, hy - 2, (int)sz.Width + 12, (int)sz.Height + 4);
                    using (var br = new SolidBrush(Color.FromArgb(220, Color.Orange)))
                        g.DrawString(hint, f, br, hx, hy);
                }
            }

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

            // ── DEV MENU button — small button in the bottom-right area ──
            int dmW = 120, dmH = 32;
            int dmX = W - dmW - 14;
            int dmY = H - panelH - dmH - 6;
            _devMenuBtn = new Rectangle(dmX, dmY, dmW, dmH);
            DrawButton(g, _devMenuBtn, "DEV MENU", Color.FromArgb(60, 60, 60));

            // Team 15 (UI/UX Artist) — blinking prompt below START button
            if (_showPrompt)
                UIArtFeatures.DrawPressStart(g, W, H, _timer, "Click  START  GAME  or  press  ENTER");

            // Team 11 (Build Engineer) — version stamp in debug mode
            BuildEngineerFeatures.DrawVersionStamp(g, W, H);

            // Team 12 (Art Director) — scanlines (toggleable)
            ArtDirectorFeatures.DrawScanlines(g, W, H);

            // Team 12 (Art Director) — vignette
            ArtDirectorFeatures.DrawVignette(g, W, H);

            // Draw active overlays (name entry or password prompt)
            if (_nameActive) DrawNameEntryBox(g, W, H);
            if (_passwordActive) DrawPasswordEntryBox(g, W, H);
        }

        private void DrawNameEntryBox(Graphics g, int W, int H)
        {
            // No full-screen overlay — the title screen remains fully visible behind the box
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

        /// <summary>
        /// Draws the password entry popup for accessing the dev menu.
        /// Input is masked with asterisks for secrecy.
        /// </summary>
        private void DrawPasswordEntryBox(Graphics g, int W, int H)
        {
            const int bw = 420, bh = 140;
            int bx = (W - bw) / 2, by = (int)(H * 0.35f);

            // Dark box background with gold border
            using (var br = new SolidBrush(Color.FromArgb(240, 10, 10, 40)))
                g.FillRectangle(br, bx, by, bw, bh);
            using (var pen = new Pen(Color.Gold, 2))
                g.DrawRectangle(pen, bx, by, bw, bh);

            // Title label
            using (var f = new Font("Courier New", 14, FontStyle.Bold))
                g.DrawString("Enter password:", f, Brushes.Gold, bx + 20, by + 14);

            // Masked password display (asterisks) with blinking cursor
            string cursor = (int)(_passwordCursor / 0.45f) % 2 == 0 ? "|" : " ";
            string masked  = new string('*', _passwordInput.Length) + cursor;
            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString(masked, f, Brushes.White, bx + 20, by + 46);

            // Error message (wrong password)
            if (!string.IsNullOrEmpty(_passwordError))
            {
                using (var f = new Font("Courier New", 11, FontStyle.Bold))
                    g.DrawString(_passwordError, f, Brushes.Red, bx + 20, by + 84);
            }

            // Help text
            using (var f = new Font("Courier New", 9))
                g.DrawString("[Enter] Confirm   [Esc] Cancel   [Backspace] Delete", f, Brushes.DimGray, bx + 20, by + 114);
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

        /// <summary>
        /// Opens the Text RPG mini-game embedded inside the main game window.
        /// The TextRPGScene overlays WinForms controls on top of the canvas.
        /// </summary>
        private static void LaunchTextRPG()
        {
            Game.Instance.Scenes.Push(new TextRPGScene(demoMode: false));
        }

        /// <summary>
        /// Opens the Text RPG in Video Demo Mode — auto-plays through all features.
        /// Embedded inside the main game window via TextRPGScene.
        /// </summary>
        private static void LaunchTextRPGDemo()
        {
            Game.Instance.Scenes.Push(new TextRPGScene(demoMode: true));
        }

        /// <summary>
        /// Handles the START GAME flow: if the player has not entered a name yet,
        /// show the name entry box first. Otherwise go straight to save slot selection.
        /// </summary>
        private void StartGame()
        {
            if (string.IsNullOrEmpty(Game.Instance.PlayerName))
            {
                // First time — prompt for a name before proceeding
                _nameActive = true;
            }
            else
            {
                // Name already set — go to save slot / game
                Game.Instance.Scenes.Replace(new SaveSlotScene());
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
