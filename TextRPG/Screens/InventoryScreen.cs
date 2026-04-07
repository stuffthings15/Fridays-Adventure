// ────────────────────────────────────────────────────────────
// TEXT RPG — Inventory Screen
// Purpose: Display collected items with Equip, Use, and Back
//          actions. Items are listed with type icons and stats.
// ────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextRPG.Screens
{
    /// <summary>
    /// Inventory management screen. Lists all player items with
    /// type-specific actions (Equip for gear, Use for potions).
    /// </summary>
    public class InventoryScreen : UserControl
    {
        private readonly MainForm _main;
        private readonly GameManager _gm;

        private ListBox _itemList;
        private Label _detailLabel;
        private Button _equipBtn, _useBtn, _backBtn;
        private Label _statusLabel;

        public InventoryScreen(MainForm main)
        {
            _main = main;
            _gm = main.Game;
            BackColor = Theme.BgDark;
            BuildUI();
            RefreshList();
        }

        private void BuildUI()
        {
            // Header
            var header = Theme.MakeLabel("\U0001F392  INVENTORY", 20, 10, 400, 35,
                18f, FontStyle.Bold, Theme.Gold);
            Controls.Add(header);

            // Player stats summary
            var p = _gm.Player;
            var stats = Theme.MakeLabel(
                $"{p.Name}  |  HP: {p.Health}/{p.MaxHealth}  |  ATK: {p.TotalAttack}  |  DEF: {p.TotalDefense}",
                20, 48, 840, 22, 10f, FontStyle.Regular, Theme.ItemBlue);
            Controls.Add(stats);

            // Item list (left side)
            _itemList = new ListBox
            {
                Location = new Point(20, 80),
                Size = new Size(400, 400),
                BackColor = Theme.BgPanel,
                ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = SelectionMode.One
            };
            _itemList.SelectedIndexChanged += (s, e) => ShowItemDetails();
            Controls.Add(_itemList);

            // Detail panel (right side)
            var detailPanel = new Panel
            {
                Location = new Point(440, 80),
                Size = new Size(420, 250),
                BackColor = Theme.BgPanel
            };
            Controls.Add(detailPanel);

            detailPanel.Controls.Add(Theme.MakeLabel("Item Details:", 10, 10, 400, 24,
                12f, FontStyle.Bold, Theme.Gold));

            _detailLabel = Theme.MakeLabel("Select an item to see details.", 10, 40, 400, 200,
                11f, FontStyle.Regular, Theme.TextLight);
            _detailLabel.AutoSize = false;
            detailPanel.Controls.Add(_detailLabel);

            // Action buttons (right side, below details)
            _equipBtn = Theme.MakeButton("\u2694 Equip", 440, 345, 200, 44, (s, e) => EquipSelected());
            _useBtn   = Theme.MakeButton("\u2764 Use",   440, 400, 200, 44, (s, e) => UseSelected());
            _backBtn  = Theme.MakeButton("\u2190 Back",  440, 460, 200, 44, (s, e) => GoBack());
            Controls.Add(_equipBtn); Controls.Add(_useBtn); Controls.Add(_backBtn);

            // Status label
            _statusLabel = Theme.MakeLabel("", 20, 495, 840, 25, 10f, FontStyle.Italic, Color.LimeGreen);
            Controls.Add(_statusLabel);

            // Empty inventory message
            if (_gm.Player.Inventory.Count == 0)
            {
                _detailLabel.Text = "Your inventory is empty.\nExplore rooms to find items!";
            }
        }

        /// <summary>Populate the list box with current inventory items.</summary>
        private void RefreshList()
        {
            _itemList.Items.Clear();
            foreach (var item in _gm.Player.Inventory)
            {
                // Icon prefix by type
                string icon;
                switch (item.Type)
                {
                    case ItemType.Weapon: icon = "\u2694"; break; // sword
                    case ItemType.Armor:  icon = "\U0001F6E1"; break; // shield
                    case ItemType.Potion: icon = "\u2764"; break; // heart
                    case ItemType.Key:    icon = "\U0001F511"; break; // key
                    default:              icon = "\u2022"; break; // bullet
                }

                string equipped = "";
                if (item == _gm.Player.EquippedWeapon) equipped = " [EQUIPPED]";
                if (item == _gm.Player.EquippedArmor) equipped = " [EQUIPPED]";

                _itemList.Items.Add($"{icon} {item.Name}{equipped}");
            }

            // Disable buttons until selection
            _equipBtn.Enabled = false;
            _useBtn.Enabled = false;
        }

        /// <summary>Show details of the selected item and enable actions.</summary>
        private void ShowItemDetails()
        {
            int idx = _itemList.SelectedIndex;
            if (idx < 0 || idx >= _gm.Player.Inventory.Count) return;

            var item = _gm.Player.Inventory[idx];
            string details = $"Name: {item.Name}\n" +
                             $"Type: {item.Type}\n" +
                             $"Description: {item.Description}\n";

            if (item.Type == ItemType.Weapon)
                details += $"Attack Bonus: +{item.StatBonus}";
            else if (item.Type == ItemType.Armor)
                details += $"Defense Bonus: +{item.StatBonus}";
            else if (item.Type == ItemType.Potion)
                details += $"Heals: {item.StatBonus} HP";

            _detailLabel.Text = details;

            // Enable appropriate buttons
            _equipBtn.Enabled = (item.Type == ItemType.Weapon || item.Type == ItemType.Armor);
            _useBtn.Enabled = (item.Type == ItemType.Potion);
        }

        private void EquipSelected()
        {
            int idx = _itemList.SelectedIndex;
            if (idx < 0 || idx >= _gm.Player.Inventory.Count) return;

            string msg = _gm.EquipItem(_gm.Player.Inventory[idx]);
            _statusLabel.Text = msg;
            RefreshList();
        }

        private void UseSelected()
        {
            int idx = _itemList.SelectedIndex;
            if (idx < 0 || idx >= _gm.Player.Inventory.Count) return;

            string msg = _gm.UseItem(_gm.Player.Inventory[idx]);
            _statusLabel.Text = msg;
            RefreshList();
        }

        private void GoBack()
        {
            _main.ShowScreen(new GameScreen(_main));
        }
    }
}
