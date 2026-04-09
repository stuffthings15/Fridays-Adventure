// ────────────────────────────────────────────────────────────
// TEXT RPG — Main Form + Theme Helpers + Host Interface
// Purpose: Hosts the current screen, provides screen switching,
//          and defines a shared dark-themed UI style.
//          ITextRPGHost allows screens to be hosted inside the
//          main game window (TextRPGHostPanel) or standalone (MainForm).
// ────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Windows.Forms;
using TextRPG.Screens;

namespace TextRPG
{
    // ── Host Interface — abstracts screen hosting ────────────────

    /// <summary>
    /// Abstraction that lets TextRPG screens run inside MainForm
    /// (standalone) or TextRPGHostPanel (embedded in the main game).
    /// Every screen stores this instead of a concrete MainForm reference.
    /// </summary>
    public interface ITextRPGHost
    {
        /// <summary>The shared game logic controller.</summary>
        GameManager Game { get; }

        /// <summary>Replace the current screen with a new one.</summary>
        void ShowScreen(UserControl screen);

        /// <summary>Close / exit the Text RPG.</summary>
        void Close();
    }

    // ── Theme — Shared color palette and control factory ──────────

    /// <summary>
    /// Dark-themed color palette and factory methods for styled controls.
    /// Every screen uses these constants for visual consistency.
    /// </summary>
    public static class Theme
    {
        public static readonly Color BgDark     = Color.FromArgb(30, 30, 40);
        public static readonly Color BgPanel    = Color.FromArgb(40, 42, 54);
        public static readonly Color TextLight  = Color.FromArgb(220, 220, 220);
        public static readonly Color Gold       = Color.FromArgb(255, 195, 0);
        public static readonly Color BtnBg      = Color.FromArgb(60, 63, 80);
        public static readonly Color BtnBorder  = Color.FromArgb(100, 100, 120);
        public static readonly Color BtnHover   = Color.FromArgb(80, 83, 100);
        public static readonly Color HPGreen    = Color.FromArgb(50, 200, 50);
        public static readonly Color HPRed      = Color.FromArgb(200, 50, 50);
        public static readonly Color ItemBlue   = Color.FromArgb(100, 180, 255);

        /// <summary>Create a styled flat button at the given position.</summary>
        public static Button MakeButton(string text, int x, int y, int w, int h, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                BackColor = BtnBg,
                ForeColor = TextLight,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = BtnBorder;
            btn.FlatAppearance.MouseOverBackColor = BtnHover;
            if (onClick != null) btn.Click += onClick;
            return btn;
        }

        /// <summary>Create a styled label.</summary>
        public static Label MakeLabel(string text, int x, int y, int w, int h,
            float fontSize = 11f, FontStyle style = FontStyle.Regular, Color? color = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                ForeColor = color ?? TextLight,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", fontSize, style),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }
    }

    // ── MainForm — Screen host ───────────────────────────────────

    /// <summary>
    /// The standalone application window. Hosts one UserControl (screen)
    /// at a time and exposes ShowScreen() for screen transitions.
    /// Implements ITextRPGHost so screens can work in both modes.
    /// </summary>
    public class MainForm : Form, ITextRPGHost
    {
        /// <summary>The shared game logic controller.</summary>
        public GameManager Game { get; private set; }

        private UserControl _currentScreen;

        public MainForm()
        {
            Text = "Realm of Shadows \u2014 Text RPG";
            ClientSize = new Size(880, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.BgDark;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            Game = new GameManager();
            ShowScreen(new TitleScreen(this));
        }

        /// <summary>
        /// Replace the current screen with a new one.
        /// Disposes the previous screen to free resources.
        /// </summary>
        public void ShowScreen(UserControl screen)
        {
            if (_currentScreen != null)
            {
                Controls.Remove(_currentScreen);
                _currentScreen.Dispose();
            }
            _currentScreen = screen;
            _currentScreen.Dock = DockStyle.Fill;
            Controls.Add(_currentScreen);
            _currentScreen.Focus();
        }
    }

    // ── Embeddable Host Panel ────────────────────────────────────

    /// <summary>
    /// A Panel-based host that implements ITextRPGHost so the Text RPG
    /// can run embedded inside the main game window without opening
    /// a separate Form.  Used by TextRPGScene.
    /// </summary>
    public class TextRPGHostPanel : Panel, ITextRPGHost
    {
        /// <summary>The shared game logic controller.</summary>
        public GameManager Game { get; private set; }

        /// <summary>Raised when the Text RPG wants to close (Quit button).</summary>
        public event Action CloseRequested;

        private UserControl _currentScreen;

        public TextRPGHostPanel()
        {
            BackColor = Theme.BgDark;
            Dock = DockStyle.Fill;
            Game = new GameManager();
        }

        /// <summary>
        /// Replace the current screen with a new one.
        /// Disposes the previous screen to free resources.
        /// </summary>
        public void ShowScreen(UserControl screen)
        {
            if (_currentScreen != null)
            {
                Controls.Remove(_currentScreen);
                _currentScreen.Dispose();
            }
            _currentScreen = screen;
            _currentScreen.Dock = DockStyle.Fill;
            Controls.Add(_currentScreen);
            _currentScreen.Focus();
        }

        /// <summary>
        /// Close the Text RPG — fires CloseRequested so the
        /// owning TextRPGScene can pop itself from the scene stack.
        /// </summary>
        public void Close()
        {
            CloseRequested?.Invoke();
        }
    }
}
