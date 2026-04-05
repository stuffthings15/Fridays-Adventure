using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Character selection screen — lets the player choose between the three
    /// playable crew members (Miss Friday, Orca, Swan) before entering the
    /// overworld. Shown after the title screen, before gameplay begins.
    /// When constructed with an optional <c>onConfirm</c> callback, the scene
    /// invokes that callback instead of pushing the default difficulty/overworld
    /// flow — used by the Dev Menu to pick a character before any level.
    /// </summary>
    public sealed class CharacterSelectScene : Scene
    {
        // ── Optional external callback (set by Dev Menu / tests) ─────────────
        private readonly Action _onConfirmOverride;

        /// <summary>Creates the default character-select (goes to Difficulty → Overworld).</summary>
        public CharacterSelectScene() { }

        /// <summary>
        /// Creates a character-select that invokes <paramref name="onConfirm"/>
        /// after the player picks a character, instead of the default overworld flow.
        /// </summary>
        public CharacterSelectScene(Action onConfirm) { _onConfirmOverride = onConfirm; }

        // ── Sprites ──────────────────────────────────────────────────────────
        private Bitmap _fridaySprite;   // player_missfriday.png (portrait only)
        private Bitmap _orcaSprite;     // Orca.png (optional)
        private Bitmap _swanSprite;     // Swan.png (optional)

        // Loaded-source debug labels (QA verification)
        private string _fridaySpriteSource = "(missing)";
        private string _orcaSpriteSource   = "(missing)";
        private string _swanSpriteSource   = "(missing)";

        // ── Panel hit-areas (computed each frame in Draw) ────────────────────
        private Rectangle _fridayPanel;
        private Rectangle _orcaPanel;
        private Rectangle _swanPanel;
        private Rectangle _confirmBtn;

        // ── Animation ────────────────────────────────────────────────────────
        private float _anim;

        // ── Fonts ────────────────────────────────────────────────────────────
        private static readonly Font _titleFont  = new Font("Courier New", 22, FontStyle.Bold);
        private static readonly Font _headerFont = new Font("Courier New", 14, FontStyle.Bold);
        private static readonly Font _bodyFont   = new Font("Courier New", 10);
        private static readonly Font _labelFont  = new Font("Courier New", 9, FontStyle.Bold);
        private static readonly Font _hintFont   = new Font("Courier New", 11, FontStyle.Bold);

        // ── Lifecycle ────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            Game.Instance.Audio.ContinueOrPlay("hub");
            LoadPortraits();
        }

        public override void OnExit()
        {
            _fridaySprite?.Dispose(); _fridaySprite = null;
            _orcaSprite?.Dispose();   _orcaSprite   = null;
            _swanSprite?.Dispose();   _swanSprite   = null;
        }

        /// <summary>
        /// Loads portrait sprites for each character panel.
        ///
        /// Orca/Swan now use the correct model-art assets used by the
        /// in-game player renderer so the character-select screen matches
        /// actual gameplay visuals.
        ///
        /// Fallback order:
        ///   1) model art under Assets\Models\...
        ///   2) legacy sprite in Assets\Sprites\...
        /// </summary>
        private void LoadPortraits()
        {
            string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

            // Miss Friday uses the canonical player sprite (with legacy fallback name).
            _fridaySprite = TryLoadBitmap(out _fridaySpriteSource,
                Path.Combine(assetsDir, "Sprites", "player_Miss_Friday.png"),
                Path.Combine(assetsDir, "Sprites", "player_missfriday.png"));

            // Orca: prefer model art first, then legacy/player sprite fallback.
            _orcaSprite = TryLoadBitmap(out _orcaSpriteSource,
                Path.Combine(assetsDir, "Models", "Orca", "Orca.png"),
                Path.Combine(assetsDir, "Character Models", "Boy Orca", "Orca.png"),
                Path.Combine(assetsDir, "Sprites", "player_Orca.png"));

            // Swan: prefer model art first, then legacy/player sprite fallback.
            _swanSprite = TryLoadBitmap(out _swanSpriteSource,
                Path.Combine(assetsDir, "Models", "Swan", "Swan.png"),
                Path.Combine(assetsDir, "Character Models", "Girl Swan", "Swan.png"),
                Path.Combine(assetsDir, "Sprites", "player_Swan.png"));
        }

        /// <summary>
        /// Returns the first successfully loaded bitmap from candidate paths and
        /// reports the source path for QA verification.
        /// </summary>
        private static Bitmap TryLoadBitmap(out string loadedPath, params string[] candidatePaths)
        {
            loadedPath = "(missing)";
            if (candidatePaths == null) return null;

            foreach (string p in candidatePaths)
            {
                if (string.IsNullOrWhiteSpace(p) || !File.Exists(p)) continue;
                try
                {
                    loadedPath = p;
                    return new Bitmap(p);
                }
                catch
                {
                    // Continue to next path on error
                }
            }

            return null;
        }

        // ── Input ────────────────────────────────────────────────────────────

        public override void Update(float dt)
        {
            _anim += dt;
            var input = Game.Instance.Input;

            // Quick-select via number keys
            if (input.IsPressed(Keys.D1)) Game.Instance.SelectedCharacter = PlayableCharacter.MissFriday;
            if (input.IsPressed(Keys.D2)) Game.Instance.SelectedCharacter = PlayableCharacter.Orca;
            if (input.IsPressed(Keys.D3)) Game.Instance.SelectedCharacter = PlayableCharacter.Swan;

            // Arrow keys and A/D for navigation (left/right)
            if (input.IsPressed(Keys.Left) || input.IsPressed(Keys.A))
            {
                // Move to previous character
                switch (Game.Instance.SelectedCharacter)
                {
                    case PlayableCharacter.Orca:
                        Game.Instance.SelectedCharacter = PlayableCharacter.MissFriday;
                        break;
                    case PlayableCharacter.Swan:
                        Game.Instance.SelectedCharacter = PlayableCharacter.Orca;
                        break;
                    case PlayableCharacter.MissFriday:
                        Game.Instance.SelectedCharacter = PlayableCharacter.Swan;  // Wrap around
                        break;
                }
            }

            if (input.IsPressed(Keys.Right) || input.IsPressed(Keys.D))
            {
                // Move to next character
                switch (Game.Instance.SelectedCharacter)
                {
                    case PlayableCharacter.MissFriday:
                        Game.Instance.SelectedCharacter = PlayableCharacter.Orca;
                        break;
                    case PlayableCharacter.Orca:
                        Game.Instance.SelectedCharacter = PlayableCharacter.Swan;
                        break;
                    case PlayableCharacter.Swan:
                        Game.Instance.SelectedCharacter = PlayableCharacter.MissFriday;  // Wrap around
                        break;
                }
            }

            // Confirm selection and proceed to the overworld
            if (input.InteractPressed || input.AttackPressed)
                ConfirmAndProceed();

            // Back — return to previous scene (Dev Menu) or title screen.
            if (input.PausePressed)
            {
                if (_onConfirmOverride != null)
                    Game.Instance.Scenes.Pop();   // return to Dev Menu
                else
                    Game.Instance.Scenes.Replace(new TitleScene());
            }
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;

            // Panel clicks — select the corresponding character
            if (_fridayPanel.Contains(p)) { Game.Instance.SelectedCharacter = PlayableCharacter.MissFriday; return; }
            if (_orcaPanel.Contains(p))   { Game.Instance.SelectedCharacter = PlayableCharacter.Orca;       return; }
            if (_swanPanel.Contains(p))   { Game.Instance.SelectedCharacter = PlayableCharacter.Swan;       return; }

            // Confirm button click
            if (_confirmBtn.Contains(p)) { ConfirmAndProceed(); return; }
        }

        /// <summary>
        /// PHASE 2 - Team 1: Game Director
        /// Locks in the current selection and transitions to the next screen.
        /// When an override callback was supplied (e.g. from Dev Menu), it is
        /// invoked instead of the default difficulty → overworld flow.
        /// </summary>
        private void ConfirmAndProceed()
        {
            // Persist selection marker so future map entries can skip first-time gating.
            Game.Instance.Save.SetInt("runtime.character", (int)Game.Instance.SelectedCharacter);
            Game.Instance.Save.SetInt("runtime.characterSelected", 1);
            Game.Instance.Save.Save();

            if (_onConfirmOverride != null)
            {
                // External caller controls the next scene (Dev Menu level launch, etc.).
                _onConfirmOverride.Invoke();
            }
            else
            {
                // Default path: push difficulty selection → overworld.
                Game.Instance.Scenes.Push(new DifficultySelectScene());
            }
        }

        // ── Draw ─────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Gradient background (ocean-themed, no character art) ─────────
            using (var br = new LinearGradientBrush(
                new Rectangle(0, 0, W, H),
                Color.FromArgb(6, 14, 44), Color.FromArgb(16, 48, 100), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // ── Title ────────────────────────────────────────────────────────
            const string title = "CHOOSE YOUR CHARACTER";
            SizeF titleSz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.Gold,
                (W - titleSz.Width) / 2f, 18);

            const string subtitle = "Select a crew member to lead the voyage";
            SizeF subSz = g.MeasureString(subtitle, _bodyFont);
            g.DrawString(subtitle, _bodyFont, Brushes.LightGray,
                (W - subSz.Width) / 2f, 50);

            // ── Three character panels ───────────────────────────────────────
            int panelW = (W - 80) / 3;
            int panelY = 78;
            int panelH = H - 150;

            _fridayPanel = new Rectangle(20,             panelY, panelW, panelH);
            _orcaPanel   = new Rectangle(30 + panelW,    panelY, panelW, panelH);
            _swanPanel   = new Rectangle(40 + panelW * 2, panelY, panelW, panelH);

            DrawCharacterPanel(g, _fridayPanel, PlayableCharacter.MissFriday,
                "MISS FRIDAY", "Lead  |  Age: unknown",
                _fridaySprite, Color.FromArgb(30, 107, 191), Brushes.Cyan,
                new[] { "HP: 100", "SPD: 195", "ATK: 14" },
                new[] { "Q Ice Wall", "E Flash Freeze", "R Break Wall" },
                "Cannot swim — water is deadly!");

            DrawCharacterPanel(g, _orcaPanel, PlayableCharacter.Orca,
                "ORCA", "Brawler  |  Age: 17",
                _orcaSprite, Color.FromArgb(24, 44, 82), Brushes.LightBlue,
                new[] { "HP: 160", "SPD: 155", "ATK: 20" },
                new[] { "Tidal Slam (5s CD)" },
                "Can swim freely in water");

            DrawCharacterPanel(g, _swanPanel, PlayableCharacter.Swan,
                "SWAN", "Speedster  |  Age: 14",
                _swanSprite, Color.FromArgb(60, 40, 60), Brushes.Pink,
                new[] { "HP: 110", "SPD: 230", "ATK: 12" },
                new[] { "Wing Dash (4s CD)" },
                "Fastest character on the crew");

            // ── Confirm button ───────────────────────────────────────────────
            int btnW = 260, btnH = 44;
            _confirmBtn = new Rectangle((W - btnW) / 2, H - 62, btnW, btnH);
            using (var br = new SolidBrush(Color.FromArgb(200, 20, 90, 20)))
                g.FillRectangle(br, _confirmBtn);
            using (var pen = new Pen(Color.Gold, 2))
                g.DrawRectangle(pen, _confirmBtn);

            string confirmText = $"SET SAIL AS {Game.Instance.SelectedCharacter.ToString().ToUpper()}";
            SizeF cSz = g.MeasureString(confirmText, _hintFont);
            g.DrawString(confirmText, _hintFont, Brushes.Gold,
                _confirmBtn.X + (_confirmBtn.Width - cSz.Width) / 2f,
                _confirmBtn.Y + (_confirmBtn.Height - cSz.Height) / 2f);

            // ── Control hints ────────────────────────────────────────────────
            g.DrawString("[1/2/3 or ←/→ or A/D] Select   [Enter/Z] Confirm   [Esc] Back",
                _labelFont, Brushes.DimGray, 10, H - 18);

            DrawDevMenuButton(g);
        }

        // ── Panel rendering helper ───────────────────────────────────────────

        /// <summary>
        /// Draws a single character panel with portrait, name, stats, abilities,
        /// and a gold selection border when this character is currently selected.
        /// </summary>
        private void DrawCharacterPanel(Graphics g, Rectangle panel,
            PlayableCharacter character, string name, string subtitle,
            Bitmap portrait, Color accentColor, Brush nameBrush,
            string[] stats, string[] abilities, string trait)
        {
            int x = panel.X, y = panel.Y, w = panel.Width, h = panel.Height;

            // Panel background
            using (var br = new SolidBrush(Color.FromArgb(160, 10, 15, 30)))
                g.FillRectangle(br, x, y, w, h);
            using (var pen = new Pen(Color.FromArgb(180, accentColor), 2))
                g.DrawRectangle(pen, x, y, w, h);

            // Gold selection border (highlighted when this character is selected)
            bool selected = Game.Instance.SelectedCharacter == character;
            if (selected)
            {
                // Pulsing glow effect for the active selection
                int alpha = (int)(180 + 75 * Math.Sin(_anim * 3.0));
                alpha = Math.Max(0, Math.Min(255, alpha));
                using (var pen = new Pen(Color.FromArgb(alpha, Color.Gold), 3))
                    g.DrawRectangle(pen, x + 1, y + 1, w - 2, h - 2);
            }

            // Portrait — character sprite or placeholder silhouette
            // PHASE 2 - Team 14: Environment Artist
            // Enlarged portrait display for model art visibility
            // Changed from 64x96 to 128x192 to properly display character models
            int portW = 128, portH = 192;
            int portX = x + w / 2 - portW / 2;
            int portY = y + 14;
            if (portrait != null)
            {
                g.DrawImage(portrait, portX, portY, portW, portH);
            }
            else
            {
                // Placeholder coloured silhouette (fallback if model not loaded)
                DrawPlaceholderPortrait(g, portX, portY, portW, portH, accentColor);
            }

            // Name and role
            int textY = portY + portH + 8;
            g.DrawString(name, _headerFont, nameBrush, x + 6, textY);
            textY += 22;
            g.DrawString(subtitle, _labelFont, Brushes.LightGray, x + 6, textY);
            textY += 20;

            // Stats
            g.DrawString("Stats:", _labelFont, Brushes.LightBlue, x + 6, textY);
            textY += 16;
            foreach (string stat in stats)
            {
                g.DrawString(stat, _bodyFont, Brushes.White, x + 10, textY);
                textY += 16;
            }
            textY += 4;

            // Abilities
            g.DrawString("Abilities:", _labelFont, Brushes.LightBlue, x + 6, textY);
            textY += 16;
            foreach (string ability in abilities)
            {
                g.DrawString(ability, _bodyFont, Brushes.White, x + 10, textY);
                textY += 16;
            }
            textY += 6;

            // Character trait / note
            g.DrawString(trait, _bodyFont,
                character == PlayableCharacter.MissFriday ? Brushes.OrangeRed : Brushes.SkyBlue,
                x + 6, textY);

            // Small source-file label for verification (truncated to file name)
            string src = character == PlayableCharacter.MissFriday ? _fridaySpriteSource
                       : character == PlayableCharacter.Orca       ? _orcaSpriteSource
                       : _swanSpriteSource;
            string srcLabel = "SRC: " + (src == "(missing)" ? "(missing)" : Path.GetFileName(src));
            g.DrawString(srcLabel, _labelFont, Brushes.Gray, x + 6, y + h - 38);

            // "SELECTED" label at the bottom of the active panel
            if (selected)
            {
                const string sel = "\u2605 SELECTED \u2605";
                SizeF sz = g.MeasureString(sel, _labelFont);
                g.DrawString(sel, _labelFont, Brushes.Gold,
                    x + (w - sz.Width) / 2f, y + h - 22);
            }
        }

        /// <summary>
        /// Simple coloured silhouette when no character sprite asset exists.
        /// </summary>
        private static void DrawPlaceholderPortrait(Graphics g, int x, int y,
            int w, int h, Color accent)
        {
            // Body rectangle
            using (var br = new SolidBrush(Color.FromArgb(140, accent)))
                g.FillRectangle(br, x, y + h / 3, w, h - h / 3);
            // Head circle
            using (var br = new SolidBrush(Color.FromArgb(200, accent)))
                g.FillEllipse(br, x + w / 6, y, w - w / 3, h / 3);
        }
    }
}
