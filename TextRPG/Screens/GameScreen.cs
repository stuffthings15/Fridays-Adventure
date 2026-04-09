// ────────────────────────────────────────────────────────────
// TEXT RPG — Game Screen (Main Exploration View)
// Purpose: Player HUD, room description, navigation buttons,
//          and context-sensitive action buttons (Talk, Portal,
//          Inventory, Save).
// ────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextRPG.Screens
{
    /// <summary>
    /// Main gameplay screen. Shows the current room, player stats,
    /// directional navigation, and context-sensitive actions.
    /// </summary>
    public class GameScreen : UserControl
    {
        private readonly ITextRPGHost _main;
        private readonly GameManager _gm;

        // HUD labels
        private Label _nameLabel, _hpLabel, _atkLabel, _defLabel;
        private Panel _hpBarBg, _hpBarFill;

        // Room display
        private Label _roomTitle;
        private RichTextBox _descBox;

        // Direction buttons
        private Button _northBtn, _southBtn, _eastBtn, _westBtn;

        // Action buttons
        private Button _talkBtn, _portalBtn, _invBtn, _saveBtn;

        // Status message
        private Label _statusLabel;

        public GameScreen(ITextRPGHost main)
        {
            _main = main;
            _gm = main.Game;
            BackColor = Theme.BgDark;
            BuildUI();
            RefreshDisplay(null);
        }

        private void BuildUI()
        {
            // ── HUD Bar (y=5..65) ─────────────────────────────────
            var hudPanel = new Panel
            {
                Location = new Point(10, 5), Size = new Size(860, 60),
                BackColor = Theme.BgPanel
            };
            Controls.Add(hudPanel);

            _nameLabel = Theme.MakeLabel("", 10, 5, 200, 22, 12f, FontStyle.Bold, Theme.Gold);
            hudPanel.Controls.Add(_nameLabel);

            hudPanel.Controls.Add(Theme.MakeLabel("HP:", 10, 32, 30, 20, 10f));
            _hpBarBg = new Panel { Location = new Point(42, 34), Size = new Size(200, 16), BackColor = Color.FromArgb(60,20,20) };
            hudPanel.Controls.Add(_hpBarBg);
            _hpBarFill = new Panel { Location = new Point(0, 0), Size = new Size(200, 16), BackColor = Theme.HPGreen };
            _hpBarBg.Controls.Add(_hpBarFill);
            _hpLabel = Theme.MakeLabel("", 248, 32, 100, 20, 10f);
            hudPanel.Controls.Add(_hpLabel);

            _atkLabel = Theme.MakeLabel("", 400, 10, 200, 20, 10f, FontStyle.Regular, Theme.ItemBlue);
            hudPanel.Controls.Add(_atkLabel);
            _defLabel = Theme.MakeLabel("", 400, 34, 200, 20, 10f, FontStyle.Regular, Theme.ItemBlue);
            hudPanel.Controls.Add(_defLabel);

            // Equipped display
            hudPanel.Controls.Add(Theme.MakeLabel("Weapon:", 620, 10, 60, 20, 9f, FontStyle.Regular, Color.Gray));
            hudPanel.Controls.Add(Theme.MakeLabel("Armor:",  620, 34, 60, 20, 9f, FontStyle.Regular, Color.Gray));

            // ── Room Display (y=75..400) ──────────────────────────
            _roomTitle = Theme.MakeLabel("", 10, 72, 860, 30, 16f, FontStyle.Bold, Theme.Gold);
            Controls.Add(_roomTitle);

            _descBox = new RichTextBox
            {
                Location = new Point(10, 105), Size = new Size(860, 280),
                BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 11),
                ReadOnly = true, BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Controls.Add(_descBox);

            // ── Navigation Buttons (y=395) ────────────────────────
            int ny = 395;
            Controls.Add(Theme.MakeLabel("Navigate:", 10, ny + 5, 80, 30, 10f, FontStyle.Regular, Color.Gray));
            _northBtn = Theme.MakeButton("\u2191 North", 100, ny, 120, 38, (s, e) => DoMove(Direction.North));
            _southBtn = Theme.MakeButton("\u2193 South", 230, ny, 120, 38, (s, e) => DoMove(Direction.South));
            _eastBtn  = Theme.MakeButton("\u2192 East",  360, ny, 120, 38, (s, e) => DoMove(Direction.East));
            _westBtn  = Theme.MakeButton("\u2190 West",  490, ny, 120, 38, (s, e) => DoMove(Direction.West));
            Controls.Add(_northBtn); Controls.Add(_southBtn);
            Controls.Add(_eastBtn);  Controls.Add(_westBtn);

            // ── Action Buttons (y=445) ────────────────────────────
            int ay = 445;
            _talkBtn   = Theme.MakeButton("\U0001F4AC Talk",    100, ay, 120, 38, (s, e) => TalkToNpc());
            _portalBtn = Theme.MakeButton("\u2728 Portal",     230, ay, 120, 38, (s, e) => EnterPortal());
            _invBtn    = Theme.MakeButton("\U0001F392 Inventory", 360, ay, 150, 38, (s, e) => OpenInventory());
            _saveBtn   = Theme.MakeButton($"\U0001F4BE Save (Slot {_gm.ActiveSlot})", 520, ay, 150, 38, (s, e) => SaveGame());
            Controls.Add(_talkBtn); Controls.Add(_portalBtn);
            Controls.Add(_invBtn);  Controls.Add(_saveBtn);

            // Main Menu return button — lets the player go back to the title screen
            var menuBtn = Theme.MakeButton("\u2190 Main Menu", 700, ay, 150, 38, (s, e) => _main.ShowScreen(new TitleScreen(_main)));
            menuBtn.BackColor = Color.FromArgb(80, 40, 40);
            Controls.Add(menuBtn);

            // ── Status Label (y=495) ──────────────────────────────
            _statusLabel = Theme.MakeLabel("", 10, 500, 860, 30, 10f, FontStyle.Italic, Color.LimeGreen);
            Controls.Add(_statusLabel);
        }

        /// <summary>Refresh all UI elements from current GameManager state.</summary>
        private void RefreshDisplay(string extraMessage)
        {
            var p = _gm.Player;
            var room = _gm.CurrentRoom;

            // HUD
            _nameLabel.Text = p.Name;
            _hpLabel.Text = $"{p.Health} / {p.MaxHealth}";
            float hpPct = (float)p.Health / p.MaxHealth;
            _hpBarFill.Width = (int)(200 * hpPct);
            _hpBarFill.BackColor = hpPct > 0.5f ? Theme.HPGreen
                : hpPct > 0.25f ? Color.Orange : Theme.HPRed;
            _atkLabel.Text = $"ATK: {p.TotalAttack}" +
                (p.EquippedWeapon != null ? $" ({p.EquippedWeapon.Name})" : "");
            _defLabel.Text = $"DEF: {p.TotalDefense}" +
                (p.EquippedArmor != null ? $" ({p.EquippedArmor.Name})" : "");

            // Room
            _roomTitle.Text = room.Name;
            string desc = room.Description;
            if (!string.IsNullOrEmpty(extraMessage))
                desc += "\n\n" + extraMessage;
            _descBox.Text = desc;

            // Direction button enable/disable
            _northBtn.Enabled = room.Exits.ContainsKey(Direction.North);
            _southBtn.Enabled = room.Exits.ContainsKey(Direction.South);
            _eastBtn.Enabled  = room.Exits.ContainsKey(Direction.East);
            _westBtn.Enabled  = room.Exits.ContainsKey(Direction.West);

            // Context buttons
            _talkBtn.Visible = room.Npc != null;
            _portalBtn.Visible = !string.IsNullOrEmpty(room.PortalTargetId);

            _statusLabel.Text = "";
        }

        // ── Event Handlers ───────────────────────────────────────

        private void DoMove(Direction dir)
        {
            var result = _gm.MovePlayer(dir);
            if (result.EnteredCombat)
            {
                // Show the room briefly, then enter combat
                RefreshDisplay(result.Message);
                _main.ShowScreen(new CombatScreen(_main));
            }
            else
            {
                RefreshDisplay(result.Message);
            }
        }

        private void TalkToNpc()
        {
            if (_gm.CurrentRoom.Npc != null)
                _main.ShowScreen(new DialogueScreen(_main, _gm.CurrentRoom.Npc));
        }

        private void EnterPortal()
        {
            var result = _gm.UsePortal();
            if (result.EnteredCombat)
            {
                RefreshDisplay(result.Message);
                _main.ShowScreen(new CombatScreen(_main));
            }
            else
                RefreshDisplay(result.Message);
        }

        private void OpenInventory()
        {
            _main.ShowScreen(new InventoryScreen(_main));
        }

        private void SaveGame()
        {
            _gm.SaveGame();
            _statusLabel.Text = $"\u2705 Game saved to Slot {_gm.ActiveSlot}!";
        }
    }
}
