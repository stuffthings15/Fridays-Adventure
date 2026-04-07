// ────────────────────────────────────────────────────────────
// TEXT RPG — Combat Screen
// Purpose: Turn-based combat UI with HP bars, combat log,
//          and Attack / Use Potion / Flee actions.
// ────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TextRPG.Screens
{
    /// <summary>
    /// Turn-based combat screen. Displays player and enemy HP bars,
    /// a scrolling combat log, and action buttons.
    /// </summary>
    public class CombatScreen : UserControl
    {
        private readonly MainForm _main;
        private readonly GameManager _gm;

        // Player side
        private Label _playerName, _playerHP;
        private Panel _playerBarBg, _playerBarFill;

        // Enemy side
        private Label _enemyName, _enemyHP;
        private Panel _enemyBarBg, _enemyBarFill;

        // Combat log
        private RichTextBox _logBox;

        // Action buttons
        private Button _attackBtn, _potionBtn, _fleeBtn;

        public CombatScreen(MainForm main)
        {
            _main = main;
            _gm = main.Game;
            BackColor = Theme.BgDark;
            BuildUI();
            RefreshBars();
            AppendLog($"A wild {_gm.CurrentEnemy.Name} appears!\n");
        }

        private void BuildUI()
        {
            var enemy = _gm.CurrentEnemy;
            var player = _gm.Player;

            // ── Combat header label ──────────────────────────────
            var header = Theme.MakeLabel("\u2694  COMBAT  \u2694", 0, 8, 880, 30,
                16f, FontStyle.Bold, Theme.Gold);
            header.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(header);

            // ── Player info panel (left) ─────────────────────────
            var pPanel = new Panel { Location = new Point(20, 50), Size = new Size(400, 80), BackColor = Theme.BgPanel };
            Controls.Add(pPanel);
            _playerName = Theme.MakeLabel(player.Name, 10, 5, 380, 24, 13f, FontStyle.Bold, Theme.ItemBlue);
            pPanel.Controls.Add(_playerName);
            pPanel.Controls.Add(Theme.MakeLabel("HP:", 10, 35, 30, 18, 10f));
            _playerBarBg = new Panel { Location = new Point(42, 37), Size = new Size(280, 14), BackColor = Color.FromArgb(60, 20, 20) };
            pPanel.Controls.Add(_playerBarBg);
            _playerBarFill = new Panel { Location = new Point(0, 0), Size = new Size(280, 14), BackColor = Theme.HPGreen };
            _playerBarBg.Controls.Add(_playerBarFill);
            _playerHP = Theme.MakeLabel("", 328, 35, 70, 18, 10f);
            pPanel.Controls.Add(_playerHP);
            pPanel.Controls.Add(Theme.MakeLabel($"ATK: {player.TotalAttack}  DEF: {player.TotalDefense}", 10, 56, 300, 18, 9f, FontStyle.Regular, Color.Gray));

            // ── Enemy info panel (right) ─────────────────────────
            var ePanel = new Panel { Location = new Point(460, 50), Size = new Size(400, 80), BackColor = Theme.BgPanel };
            Controls.Add(ePanel);
            _enemyName = Theme.MakeLabel(enemy.Name, 10, 5, 380, 24, 13f, FontStyle.Bold, Theme.HPRed);
            ePanel.Controls.Add(_enemyName);
            ePanel.Controls.Add(Theme.MakeLabel("HP:", 10, 35, 30, 18, 10f));
            _enemyBarBg = new Panel { Location = new Point(42, 37), Size = new Size(280, 14), BackColor = Color.FromArgb(60, 20, 20) };
            ePanel.Controls.Add(_enemyBarBg);
            _enemyBarFill = new Panel { Location = new Point(0, 0), Size = new Size(280, 14), BackColor = Theme.HPRed };
            _enemyBarBg.Controls.Add(_enemyBarFill);
            _enemyHP = Theme.MakeLabel("", 328, 35, 70, 18, 10f);
            ePanel.Controls.Add(_enemyHP);
            ePanel.Controls.Add(Theme.MakeLabel($"ATK: {enemy.Attack}  DEF: {enemy.Defense}", 10, 56, 300, 18, 9f, FontStyle.Regular, Color.Gray));

            // ── "VS" label ───────────────────────────────────────
            var vs = Theme.MakeLabel("VS", 410, 70, 60, 30, 14f, FontStyle.Bold, Theme.Gold);
            vs.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(vs);

            // ── Combat Log ───────────────────────────────────────
            _logBox = new RichTextBox
            {
                Location = new Point(20, 145), Size = new Size(840, 340),
                BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                Font = new Font("Consolas", 11),
                ReadOnly = true, BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Controls.Add(_logBox);

            // ── Action Buttons ───────────────────────────────────
            int by = 500;
            _attackBtn = Theme.MakeButton("\u2694 Attack", 120, by, 180, 48, (s, e) => DoAttack());
            _potionBtn = Theme.MakeButton("\u2764 Use Potion", 340, by, 180, 48, (s, e) => UsePotion());
            _fleeBtn   = Theme.MakeButton("\U0001F3C3 Flee", 560, by, 180, 48, (s, e) => DoFlee());
            Controls.Add(_attackBtn); Controls.Add(_potionBtn); Controls.Add(_fleeBtn);
        }

        /// <summary>Update HP bars and labels from current game state.</summary>
        private void RefreshBars()
        {
            var p = _gm.Player;
            var e = _gm.CurrentEnemy;

            // Player HP bar
            float pPct = (float)p.Health / p.MaxHealth;
            _playerBarFill.Width = Math.Max(0, (int)(280 * pPct));
            _playerBarFill.BackColor = pPct > 0.5f ? Theme.HPGreen
                : pPct > 0.25f ? Color.Orange : Theme.HPRed;
            _playerHP.Text = $"{p.Health}/{p.MaxHealth}";

            // Enemy HP bar
            float ePct = (float)e.Health / e.MaxHealth;
            _enemyBarFill.Width = Math.Max(0, (int)(280 * ePct));
            _enemyHP.Text = $"{e.Health}/{e.MaxHealth}";

            // Potion button — only enable if player has a potion
            bool hasPotion = p.Inventory.Any(i => i.Type == ItemType.Potion);
            _potionBtn.Enabled = hasPotion;
        }

        /// <summary>Append text to the combat log and scroll to bottom.</summary>
        private void AppendLog(string text)
        {
            _logBox.AppendText(text + "\n");
            _logBox.ScrollToCaret();
        }

        private void DisableActions()
        {
            _attackBtn.Enabled = false;
            _potionBtn.Enabled = false;
            _fleeBtn.Enabled = false;
        }

        // ── Button Handlers ──────────────────────────────────────

        private void DoAttack()
        {
            var result = _gm.PlayerAttack();
            AppendLog(result.Log);
            RefreshBars();

            if (result.Victory)
            {
                DisableActions();
                AppendLog("\n\u2728 The dragon falls! You are victorious!");
                // Short delay then show victory screen
                var timer = new Timer { Interval = 1500 };
                timer.Tick += (s, e) => { timer.Stop(); _main.ShowScreen(new GameOverScreen(_main, true)); };
                timer.Start();
            }
            else if (result.EnemyDefeated)
            {
                DisableActions();
                AppendLog("\nReturning to exploration...");
                var timer = new Timer { Interval = 1200 };
                timer.Tick += (s, e) => { timer.Stop(); _main.ShowScreen(new GameScreen(_main)); };
                timer.Start();
            }
            else if (result.PlayerDied)
            {
                DisableActions();
                var timer = new Timer { Interval = 1500 };
                timer.Tick += (s, e) => { timer.Stop(); _main.ShowScreen(new GameOverScreen(_main, false)); };
                timer.Start();
            }
        }

        private void UsePotion()
        {
            var potion = _gm.Player.Inventory.FirstOrDefault(i => i.Type == ItemType.Potion);
            if (potion != null)
            {
                string msg = _gm.UseItem(potion);
                AppendLog(msg);
                RefreshBars();
            }
        }

        private void DoFlee()
        {
            if (_gm.TryFlee())
            {
                AppendLog("You successfully fled!");
                // Move player back to previous room (just go to game screen)
                var timer = new Timer { Interval = 800 };
                timer.Tick += (s, e) => { timer.Stop(); _main.ShowScreen(new GameScreen(_main)); };
                timer.Start();
                DisableActions();
            }
            else
            {
                AppendLog("You failed to flee!");
                // Enemy gets a free attack
                int dmg = Math.Max(1, _gm.CurrentEnemy.Attack - _gm.Player.TotalDefense);
                _gm.Player.Health = Math.Max(0, _gm.Player.Health - dmg);
                AppendLog($"The {_gm.CurrentEnemy.Name} strikes you for {dmg} damage!");
                RefreshBars();

                if (_gm.Player.Health <= 0)
                {
                    AppendLog("\n\u2620 You have been slain...");
                    DisableActions();
                    var timer = new Timer { Interval = 1500 };
                    timer.Tick += (s, e) => { timer.Stop(); _main.ShowScreen(new GameOverScreen(_main, false)); };
                    timer.Start();
                }
            }
        }
    }
}
