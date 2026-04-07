// ────────────────────────────────────────────────────────────
// TEXT RPG — Title Screen
// Purpose: Main menu with New Game, Load Game, and Quit.
//          Clicking "New Game" transitions to a name entry view.
// ────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextRPG.Screens
{
    /// <summary>
    /// Title / main menu screen. Displays the game title and menu buttons.
    /// Has two visual states: menu (buttons) and name entry (text field).
    /// </summary>
    public class TitleScreen : UserControl
    {
        private readonly MainForm _main;

        // Menu-state controls
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Button _newGameBtn;
        private Button _loadGameBtn;
        private Button _quitBtn;

        // Name-entry-state controls
        private Label _namePrompt;
        private TextBox _nameBox;
        private Button _beginBtn;
        private Button _backBtn;

        public TitleScreen(MainForm main)
        {
            _main = main;
            BackColor = Theme.BgDark;
            BuildMenuUI();
        }

        /// <summary>Build the initial menu buttons and title text.</summary>
        private void BuildMenuUI()
        {
            Controls.Clear();

            // Game title
            _titleLabel = Theme.MakeLabel("REALM OF SHADOWS", 0, 100, 880, 70,
                36f, FontStyle.Bold, Theme.Gold);
            _titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_titleLabel);

            // Subtitle
            _subtitleLabel = Theme.MakeLabel("A Text Adventure", 0, 170, 880, 30,
                14f, FontStyle.Italic, Theme.TextLight);
            _subtitleLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_subtitleLabel);

            // Center X for 250px-wide buttons in 880px form
            int cx = (880 - 250) / 2;

            _newGameBtn = Theme.MakeButton("New Game", cx, 280, 250, 50, (s, e) => ShowNameEntry());
            Controls.Add(_newGameBtn);

            _loadGameBtn = Theme.MakeButton("Load Game", cx, 350, 250, 50, (s, e) => LoadGame());
            _loadGameBtn.Enabled = SaveSystem.SaveExists();
            if (!_loadGameBtn.Enabled)
            {
                _loadGameBtn.ForeColor = Color.Gray;
                _loadGameBtn.Text = "Load Game (no save)";
            }
            Controls.Add(_loadGameBtn);

            _quitBtn = Theme.MakeButton("Quit", cx, 420, 250, 50, (s, e) => Application.Exit());
            Controls.Add(_quitBtn);

            // Version / credits
            var credits = Theme.MakeLabel("CS-120 Project \u2014 .NET Framework 4.7.2 \u2014 WinForms",
                0, 560, 880, 25, 9f, FontStyle.Regular, Color.Gray);
            credits.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(credits);
        }

        /// <summary>Switch to name-entry view within the same screen.</summary>
        private void ShowNameEntry()
        {
            Controls.Clear();

            // Keep the title visible
            Controls.Add(_titleLabel);

            _namePrompt = Theme.MakeLabel("Enter your name:", 0, 220, 880, 30,
                14f, FontStyle.Regular, Theme.TextLight);
            _namePrompt.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(_namePrompt);

            _nameBox = new TextBox
            {
                Location = new Point(290, 260),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 14),
                BackColor = Theme.BgPanel,
                ForeColor = Theme.TextLight,
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 20,
                Text = "Hero"
            };
            // Allow pressing Enter to begin
            _nameBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BeginGame(); };
            Controls.Add(_nameBox);
            _nameBox.SelectAll();
            _nameBox.Focus();

            int cx = (880 - 250) / 2;
            _beginBtn = Theme.MakeButton("Begin Adventure", cx, 320, 250, 50, (s, e) => BeginGame());
            Controls.Add(_beginBtn);

            _backBtn = Theme.MakeButton("\u2190 Back", cx, 390, 250, 40, (s, e) => BuildMenuUI());
            Controls.Add(_backBtn);
        }

        /// <summary>Start a new game with the entered name.</summary>
        private void BeginGame()
        {
            string name = _nameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) name = "Hero";

            _main.Game.StartNewGame(name);
            _main.ShowScreen(new GameScreen(_main));
        }

        /// <summary>Load saved game and go to the game screen.</summary>
        private void LoadGame()
        {
            if (_main.Game.LoadGame())
                _main.ShowScreen(new GameScreen(_main));
            else
                MessageBox.Show("No save file found.", "Load Game",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
