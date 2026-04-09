using System;
using System.Drawing;
using System.IO;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Entities;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Crew roster screen — shows Miss Friday, Orca, and Swan with their
    /// canonical colours, stats, abilities, and current bond level.
    /// Opened from the CREW button on the overworld map.
    /// </summary>
    public sealed class CrewScene : Scene
    {
        private Bitmap _conceptArt;    // Orca + Swan concept sheet
        private Bitmap _fridaySprite;  // player_missfriday.png
        private Bitmap _orcaSprite;    // player_Orca.png
        private Bitmap _swanSprite;    // player_Swan.png

        // QA source labels for loaded portrait files.
        private string _fridaySpriteSource = "(missing)";
        private string _orcaSpriteSource   = "(missing)";
        private string _swanSpriteSource   = "(missing)";

        private readonly OrcaCompanion _orca = new OrcaCompanion(0, 0);
        private readonly SwanCompanion _swan = new SwanCompanion(0, 0);

        // Clickable panel bounds for character selection.
        private Rectangle _fridayPanel;
        private Rectangle _orcaPanel;
        private Rectangle _swanPanel;

        private static readonly Font _titleFont   = new Font("Courier New", 20, FontStyle.Bold);
        private static readonly Font _headerFont  = new Font("Courier New", 13, FontStyle.Bold);
        private static readonly Font _bodyFont    = new Font("Courier New", 10);
        private static readonly Font _labelFont   = new Font("Courier New", 9,  FontStyle.Bold);

        public override void OnEnter()
        {
            Game.Instance.Audio.ContinueOrPlay("hub");

            string spritesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                             "Assets", "Sprites");
            // Concept art sheet (Orca + Swan)
            string conceptPath = Path.Combine(spritesDir, "character_orca_swan_concept.png");
            if (File.Exists(conceptPath))
                _conceptArt = new Bitmap(conceptPath);

            // Miss Friday sprite — player_Miss_Friday.png is the canonical model.
            string fridayPath = Path.Combine(spritesDir, "player_Miss_Friday.png");
            if (!File.Exists(fridayPath))
                fridayPath = Path.Combine(spritesDir, "player_missfriday.png");
            if (File.Exists(fridayPath))
            {
                _fridaySprite = new Bitmap(fridayPath);
                _fridaySpriteSource = fridayPath;
            }

            // Orca sprite
            string orcaPath = Path.Combine(spritesDir, "player_Orca.png");
            if (File.Exists(orcaPath))
            {
                _orcaSprite = new Bitmap(orcaPath);
                _orcaSpriteSource = orcaPath;
            }

            // Swan sprite
            string swanPath = Path.Combine(spritesDir, "player_Swan.png");
            if (File.Exists(swanPath))
            {
                _swanSprite = new Bitmap(swanPath);
                _swanSpriteSource = swanPath;
            }
        }

        public override void OnExit()
        {
            _conceptArt?.Dispose();   _conceptArt   = null;
            _fridaySprite?.Dispose(); _fridaySprite = null;
            _orcaSprite?.Dispose();   _orcaSprite   = null;
            _swanSprite?.Dispose();   _swanSprite   = null;
        }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;

            // Character quick-select shortcuts for usability.
            if (input.IsPressed(System.Windows.Forms.Keys.D1)) Game.Instance.SelectedCharacter = PlayableCharacter.MissFriday;
            if (input.IsPressed(System.Windows.Forms.Keys.D2)) Game.Instance.SelectedCharacter = PlayableCharacter.Orca;
            if (input.IsPressed(System.Windows.Forms.Keys.D3)) Game.Instance.SelectedCharacter = PlayableCharacter.Swan;

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (HandleMainMenuClick(p)) return;

            // Select character from panel click.
            if (_fridayPanel.Contains(p)) { Game.Instance.SelectedCharacter = PlayableCharacter.MissFriday; return; }
            if (_orcaPanel.Contains(p))   { Game.Instance.SelectedCharacter = PlayableCharacter.Orca; return; }
            if (_swanPanel.Contains(p))   { Game.Instance.SelectedCharacter = PlayableCharacter.Swan; return; }

            Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // ── Background ─────────────────────────────────────────────────────
            using (var br = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, W, H),
                Color.FromArgb(8, 20, 50), Color.FromArgb(20, 50, 90), 90f))
                g.FillRectangle(br, 0, 0, W, H);

            // ── Title ──────────────────────────────────────────────────────────
            string title = "SEA SERPENT CREW";
            SizeF titleSz = g.MeasureString(title, _titleFont);
            g.DrawString(title, _titleFont, Brushes.Cyan,
                (W - titleSz.Width) / 2f, 14);

            int bonds = Game.Instance.CrewBonds;
            string bondStr = $"Crew Bonds: {bonds} / 10";
            SizeF bondSz = g.MeasureString(bondStr, _labelFont);
            g.DrawString(bondStr, _labelFont, Brushes.Gold,
                (W - bondSz.Width) / 2f, 42);

            // ── Bond bar ───────────────────────────────────────────────────────
            int barX = W / 2 - 100, barY = 60;
            g.FillRectangle(Brushes.DimGray, barX, barY, 200, 10);
            if (bonds > 0)
                using (var br = new SolidBrush(Color.Gold))
                    g.FillRectangle(br, barX, barY, (int)(200 * bonds / 10f), 10);
            g.DrawRectangle(Pens.Gray, barX, barY, 200, 10);

            // ── Three character panels ─────────────────────────────────────────
            int panelW = (W - 80) / 3;
            _fridayPanel = new Rectangle(20, 90, panelW, H - 130);
            _orcaPanel   = new Rectangle(30 + panelW, 90, panelW, H - 130);
            _swanPanel   = new Rectangle(40 + panelW * 2, 90, panelW, H - 130);
            DrawFridayPanel(g, _fridayPanel.X, _fridayPanel.Y, _fridayPanel.Width, _fridayPanel.Height);
            DrawOrcaPanel  (g, _orcaPanel.X, _orcaPanel.Y, _orcaPanel.Width, _orcaPanel.Height);
            DrawSwanPanel  (g, _swanPanel.X, _swanPanel.Y, _swanPanel.Width, _swanPanel.Height);

            // ── Concept art strip (bottom) if loaded ──────────────────────────
            if (_conceptArt != null)
            {
                int artH = 90;
                int artW = (int)(_conceptArt.Width * (artH / (float)_conceptArt.Height));
                artW = Math.Min(artW, W - 40);
                g.DrawImage(_conceptArt, (W - artW) / 2, H - artH - 10, artW, artH);
                using (var br = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                    g.FillRectangle(br, (W - artW) / 2, H - artH - 10, artW, artH);
                using (var f = new Font("Courier New", 8))
                    g.DrawString("Concept Art — Orca & Swan", f, Brushes.DimGray,
                        (W - artW) / 2 + 4, H - 22);
            }

            // ── Close hint ────────────────────────────────────────────────────
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("[1/2/3 or Click Panel] Select   [Enter / Esc] Close", f, Brushes.DimGray, 10, H - 24);

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Selected: " + Game.Instance.SelectedCharacter, f, Brushes.Gold, W - 220, H - 24);
            DrawMainMenuReturnButton(g);
            DrawDevMenuButton(g);
        }

        // ── Individual panel draws ───────────────────────────────────────────

        private void DrawFridayPanel(Graphics g, int x, int y, int w, int h)
        {
            DrawPanelBg(g, x, y, w, h, Color.FromArgb(30, 107, 191));
            if (Game.Instance.SelectedCharacter == PlayableCharacter.MissFriday)
                using (var pen = new Pen(Color.Gold, 3)) g.DrawRectangle(pen, x + 1, y + 1, w - 2, h - 2);

            // Portrait
            if (_fridaySprite != null)
                g.DrawImage(_fridaySprite, x + w / 2 - 28, y + 12, 56, 84);
            else
                DrawPlaceholderPortrait(g, x + w / 2 - 28, y + 12, 56, 84,
                    Color.FromArgb(30, 107, 191), Color.AliceBlue);

            g.DrawString("MISS FRIDAY", _headerFont, Brushes.Cyan, x + 4, y + 104);
            g.DrawString("Age: unknown  |  Lead", _labelFont, Brushes.LightGray, x + 4, y + 124);
            DrawStat(g, x + 4, y + 144, w - 8, "HP",    "100",  Color.LimeGreen);
            DrawStat(g, x + 4, y + 162, w - 8, "SPD",   "195",  Color.Cyan);
            DrawStat(g, x + 4, y + 180, w - 8, "ATK",   "14",   Color.OrangeRed);
            g.DrawString("Abilities:", _labelFont, Brushes.LightBlue, x + 4, y + 202);
            g.DrawString("Q Ice Wall  E Flash Freeze", _bodyFont, Brushes.White, x + 4, y + 218);
            g.DrawString("R Break Wall  (ice powers)", _bodyFont, Brushes.White, x + 4, y + 234);
            g.DrawString("Water danger: Cannot swim", _bodyFont,
                Brushes.OrangeRed, x + 4, y + 254);
            g.DrawString("SRC: " + Path.GetFileName(_fridaySpriteSource), _labelFont, Brushes.Gray, x + 4, y + h - 22);
        }

        private void DrawOrcaPanel(Graphics g, int x, int y, int w, int h)
        {
            DrawPanelBg(g, x, y, w, h, Color.FromArgb(24, 44, 82));
            if (Game.Instance.SelectedCharacter == PlayableCharacter.Orca)
                using (var pen = new Pen(Color.Gold, 3)) g.DrawRectangle(pen, x + 1, y + 1, w - 2, h - 2);

            // Portrait
            if (_orcaSprite != null)
                g.DrawImage(_orcaSprite, x + w / 2 - 28, y + 12, 56, 84);
            else
                DrawPlaceholderPortrait(g, x + w / 2 - 28, y + 12, 56, 84,
                    Color.FromArgb(24, 44, 82), Color.WhiteSmoke);

            bool unlocked = Game.Instance.Save.GetFlag(Data.NarrativeFlags.OrcaJoinedCrew);
            g.DrawString("ORCA", _headerFont, Brushes.LightBlue, x + 4, y + 104);
            g.DrawString($"Age: 17  |  Brawler{(unlocked ? "" : "  [locked]")}", _labelFont,
                unlocked ? Brushes.LightGray : Brushes.DimGray, x + 4, y + 124);

            if (unlocked)
            {
                DrawStat(g, x + 4, y + 144, w - 8, "HP",  "160",  Color.LimeGreen);
                DrawStat(g, x + 4, y + 162, w - 8, "SPD", "155",  Color.Cyan);
                DrawStat(g, x + 4, y + 180, w - 8, "ATK", "20",   Color.OrangeRed);
                g.DrawString("Abilities:", _labelFont, Brushes.LightBlue, x + 4, y + 202);
                g.DrawString("Tidal Slam (5s CD)", _bodyFont, Brushes.White, x + 4, y + 218);
                int bonds = Game.Instance.CrewBonds;
                DrawBondAssist(g, x + 4, y + 238, w - 8, "Bond Assist", bonds >= 6,
                    "Area slam at bonds 6+");
                g.DrawString("Can swim freely", _bodyFont, Brushes.SkyBlue, x + 4, y + 274);
            }
            else
            {
                DrawLockedOverlay(g, x + 4, y + 144, w - 8);
            }

            g.DrawString("SRC: " + Path.GetFileName(_orcaSpriteSource), _labelFont, Brushes.Gray, x + 4, y + h - 22);
        }

        private void DrawSwanPanel(Graphics g, int x, int y, int w, int h)
        {
            DrawPanelBg(g, x, y, w, h, Color.FromArgb(60, 40, 60));
            if (Game.Instance.SelectedCharacter == PlayableCharacter.Swan)
                using (var pen = new Pen(Color.Gold, 3)) g.DrawRectangle(pen, x + 1, y + 1, w - 2, h - 2);

            // Portrait
            if (_swanSprite != null)
                g.DrawImage(_swanSprite, x + w / 2 - 28, y + 12, 56, 84);
            else
                DrawPlaceholderPortrait(g, x + w / 2 - 28, y + 12, 56, 84,
                    Color.White, Color.FromArgb(232, 103, 138));

            bool unlocked = Game.Instance.Save.GetFlag(Data.NarrativeFlags.SwanJoinedCrew);
            g.DrawString("SWAN", _headerFont, Brushes.Pink, x + 4, y + 104);
            g.DrawString($"Age: 14  |  Speedster{(unlocked ? "" : "  [locked]")}", _labelFont,
                unlocked ? Brushes.LightGray : Brushes.DimGray, x + 4, y + 124);

            if (unlocked)
            {
                DrawStat(g, x + 4, y + 144, w - 8, "HP",  "110",  Color.LimeGreen);
                DrawStat(g, x + 4, y + 162, w - 8, "SPD", "230",  Color.Cyan);
                DrawStat(g, x + 4, y + 180, w - 8, "ATK", "12",   Color.OrangeRed);
                g.DrawString("Abilities:", _labelFont, Brushes.LightBlue, x + 4, y + 202);
                g.DrawString("Wing Dash (4s CD)", _bodyFont, Brushes.White, x + 4, y + 218);
                int bonds = Game.Instance.CrewBonds;
                DrawBondAssist(g, x + 4, y + 238, w - 8, "Bond Assist", bonds >= 9,
                    "Speed burst at bonds 9+");
            }
            else
            {
                DrawLockedOverlay(g, x + 4, y + 144, w - 8);
            }

            g.DrawString("SRC: " + Path.GetFileName(_swanSpriteSource), _labelFont, Brushes.Gray, x + 4, y + h - 22);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void DrawPanelBg(Graphics g, int x, int y, int w, int h, Color accent)
        {
            using (var br = new SolidBrush(Color.FromArgb(160, 10, 15, 30)))
                g.FillRectangle(br, x, y, w, h);
            using (var pen = new Pen(Color.FromArgb(180, accent), 2))
                g.DrawRectangle(pen, x, y, w, h);
        }

        private static void DrawPlaceholderPortrait(Graphics g, int x, int y, int w, int h,
                                                    Color body, Color head)
        {
            using (var br = new SolidBrush(body))
                g.FillRectangle(br, x, y + h / 3, w, h - h / 3);
            using (var br = new SolidBrush(head))
                g.FillEllipse(br, x + w / 6, y, w - w / 3, h / 3);
        }

        private void DrawStat(Graphics g, int x, int y, int w, string label, string val, Color bar)
        {
            g.DrawString($"{label}:", _labelFont, Brushes.LightGray, x, y);
            g.DrawString(val, _labelFont, Brushes.White, x + 36, y);
        }

        private void DrawBondAssist(Graphics g, int x, int y, int w,
                                    string label, bool active, string desc)
        {
            Brush col = active ? Brushes.Gold : Brushes.DimGray;
            g.DrawString(active ? $"\u2605 {label}" : $"\u25cb {label}", _labelFont, col, x, y);
            g.DrawString(desc, _bodyFont, active ? Brushes.LightYellow : Brushes.DimGray, x + 4, y + 14);
        }

        private void DrawLockedOverlay(Graphics g, int x, int y, int w)
        {
            using (var br = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
                g.FillRectangle(br, x, y, w, 140);
            g.DrawString("Meet this character", _bodyFont, Brushes.DimGray, x + 4, y + 24);
            g.DrawString("to unlock their", _bodyFont, Brushes.DimGray, x + 4, y + 40);
            g.DrawString("crew card.", _bodyFont, Brushes.DimGray, x + 4, y + 56);
        }
    }
}
