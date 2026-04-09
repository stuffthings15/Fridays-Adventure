// ────────────────────────────────────────────────────────────
// TEXT RPG — Title Screen (Zelda-Style Save Slots)
// Purpose: Main menu with New Game, Load Game, and Quit.
//          "Load Game" opens a 3-slot selection screen showing
//          player name, HP, location, and save date for each slot
//          — just like The Legend of Zelda's file select screen.
//          "New Game" lets the player pick which slot to save into.
// ────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextRPG.Screens
{
    /// <summary>
    /// Title / main menu screen with three visual states:
    ///   1. Main menu (New Game / Load Game / Quit)
    ///   2. Slot selection (pick a save slot to load or start new)
    ///   3. Name entry (enter name for a new game)
    /// </summary>
    public class TitleScreen : UserControl
    {
        private readonly ITextRPGHost _main;

        // Which mode the slot picker is in
        private enum SlotMode { Load, NewGame }
        private SlotMode _slotMode;

        // The slot the player chose for new game (used by name entry)
        private int _selectedSlot = 1;

        public TitleScreen(ITextRPGHost main)
        {
            _main = main;
            BackColor = Theme.BgDark;
            BuildMenuUI();
        }

        // ══════════════════════════════════════════════════════════════
        // STATE 1: Main Menu — New Game / Load Game / Quit
        // ══════════════════════════════════════════════════════════════

        /// <summary>Build the initial menu buttons and title text.</summary>
        private void BuildMenuUI()
        {
            Controls.Clear();

            // Game title
            var titleLabel = Theme.MakeLabel("REALM OF SHADOWS", 0, 80, 880, 70,
                36f, FontStyle.Bold, Theme.Gold);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(titleLabel);

            // Subtitle
            var subtitleLabel = Theme.MakeLabel("A Text Adventure", 0, 150, 880, 30,
                14f, FontStyle.Italic, Theme.TextLight);
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(subtitleLabel);

            // Center X for 250px-wide buttons in 880px form
            int cx = (880 - 250) / 2;

            var newGameBtn = Theme.MakeButton("New Game", cx, 260, 250, 50,
                (s, e) => ShowModeSelection());
            Controls.Add(newGameBtn);

            // Load Game — check if ANY slot has data
            bool anySlot = false;
            for (int i = 1; i <= SaveSystem.SlotCount; i++)
                if (SaveSystem.SaveExists(i)) { anySlot = true; break; }

            var loadGameBtn = Theme.MakeButton("Load Game", cx, 330, 250, 50,
                (s, e) => ShowSlotSelection(SlotMode.Load));
            if (!anySlot)
            {
                loadGameBtn.Enabled = false;
                loadGameBtn.ForeColor = Color.Gray;
                loadGameBtn.Text = "Load Game (no saves)";
            }
            Controls.Add(loadGameBtn);

            var quitBtn = Theme.MakeButton("Quit", cx, 400, 250, 50,
                (s, e) => _main.Close());
            Controls.Add(quitBtn);

            // Video Demo Mode button — auto-plays through all features
            var demoBtn = Theme.MakeButton("\u25B6 Video Demo Mode", cx, 470, 250, 50,
                (s, e) => _main.ShowScreen(new DemoScreen(_main)));
            demoBtn.BackColor = Color.FromArgb(140, 80, 20);
            Controls.Add(demoBtn);

            // Version / credits
            var credits = Theme.MakeLabel("CS-120 Project \u2014 .NET Framework 4.7.2 \u2014 WinForms",
                0, 560, 880, 25, 9f, FontStyle.Regular, Color.Gray);
            credits.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(credits);
        }

        // ══════════════════════════════════════════════════════════════
        // STATE 1.5: Mode Selection — RPG or Miss Friday
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show the mode picker: classic RPG or Miss Friday's Adventure 2.
        /// Both modes share the same engine but differ in presentation.
        /// </summary>
        private void ShowModeSelection()
        {
            Controls.Clear();

            var header = Theme.MakeLabel("\u2694 CHOOSE YOUR MODE \u2694", 0, 50, 880, 45,
                22f, FontStyle.Bold, Theme.Gold);
            header.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(header);

            var sub = Theme.MakeLabel("Both modes use the same engine — different stories.",
                0, 100, 880, 25, 10f, FontStyle.Italic, Color.LightGray);
            sub.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(sub);

            int cx = (880 - 400) / 2;

            // RPG Mode card
            var rpgCard = new Panel
            {
                Location = new Point(cx, 150), Size = new Size(400, 100),
                BackColor = Color.FromArgb(35, 55, 75), BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            var rpgTitle = Theme.MakeLabel("\u2694 Realm of Shadows", 14, 6, 370, 28,
                14f, FontStyle.Bold, Theme.Gold);
            var rpgDesc = Theme.MakeLabel("Classic text RPG — you choose your name and explore " +
                "a dark fantasy world of goblins, trolls, and dragons.",
                14, 38, 370, 55, 9f, FontStyle.Regular, Theme.TextLight);
            rpgCard.Controls.Add(rpgTitle);
            rpgCard.Controls.Add(rpgDesc);
            // Wire click on the card AND its child labels so clicking anywhere works.
            EventHandler rpgClick = (s, e) => { _main.Game.Mode = GameMode.RPG; ShowSlotSelection(SlotMode.NewGame); };
            rpgCard.Click += rpgClick;
            rpgTitle.Click += rpgClick;
            rpgDesc.Click += rpgClick;
            Controls.Add(rpgCard);

            // Miss Friday Mode card
            var fridayCard = new Panel
            {
                Location = new Point(cx, 270), Size = new Size(400, 100),
                BackColor = Color.FromArgb(55, 35, 60), BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            var fridayTitle = Theme.MakeLabel("\u2693 Miss Friday's Adventure 2", 14, 6, 370, 28,
                14f, FontStyle.Bold, Color.FromArgb(255, 180, 100));
            var fridayDesc = Theme.MakeLabel("Play as Miss Friday — a pirate captain with narrative-rich " +
                "descriptions, unique NPCs, and a coastal setting.",
                14, 38, 370, 55, 9f, FontStyle.Regular, Theme.TextLight);
            fridayCard.Controls.Add(fridayTitle);
            fridayCard.Controls.Add(fridayDesc);
            // Wire click on the card AND its child labels so clicking anywhere works.
            EventHandler fridayClick = (s, e) => { _main.Game.Mode = GameMode.MissFriday; ShowSlotSelection(SlotMode.NewGame); };
            fridayCard.Click += fridayClick;
            fridayTitle.Click += fridayClick;
            fridayDesc.Click += fridayClick;
            Controls.Add(fridayCard);

            var backBtn = Theme.MakeButton("\u2190 Back", (880 - 200) / 2, 420, 200, 40,
                (s, e) => BuildMenuUI());
            Controls.Add(backBtn);
        }

        // ══════════════════════════════════════════════════════════════
        // STATE 2: Slot Selection — Zelda-style 3 file slots
        // ══════════════════════════════════════════════════════════════

        /// <summary>Show the 3-slot selection screen for loading or new game.</summary>
        private void ShowSlotSelection(SlotMode mode)
        {
            _slotMode = mode;
            Controls.Clear();

            // Header
            string headerText = mode == SlotMode.Load
                ? "\u2694 SELECT A SAVE FILE \u2694"
                : "\u2694 CHOOSE A SLOT \u2694";
            var header = Theme.MakeLabel(headerText, 0, 30, 880, 45,
                22f, FontStyle.Bold, Theme.Gold);
            header.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(header);

            // Subheader with instructions
            string subText = mode == SlotMode.Load
                ? "Select a save file to continue your adventure."
                : "Select a slot to start your new adventure. Existing saves will be overwritten.";
            var sub = Theme.MakeLabel(subText, 0, 75, 880, 25,
                10f, FontStyle.Italic, Color.LightGray);
            sub.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(sub);

            // Draw 3 save slot cards
            int cardW = 780, cardH = 110;
            int startX = (880 - cardW) / 2;
            int startY = 115;

            for (int i = 1; i <= SaveSystem.SlotCount; i++)
            {
                int slot = i; // capture for closure
                int cy = startY + (i - 1) * (cardH + 14);

                var summary = SaveSystem.GetSlotSummary(slot);
                bool hasData = summary != null;

                // Card background panel
                var card = new Panel
                {
                    Location = new Point(startX, cy),
                    Size = new Size(cardW, cardH),
                    BackColor = hasData ? Color.FromArgb(45, 48, 65) : Color.FromArgb(35, 35, 45),
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };

                // Slot number label (left side, large)
                var slotNum = Theme.MakeLabel($"SLOT {slot}", 16, 8, 90, 30,
                    14f, FontStyle.Bold, Theme.Gold);
                card.Controls.Add(slotNum);

                if (hasData)
                {
                    // ── Populated slot — show player info ─────────────────
                    // Player name
                    var nameLabel = Theme.MakeLabel(summary.PlayerName, 120, 8, 300, 26,
                        14f, FontStyle.Bold, Theme.TextLight);
                    card.Controls.Add(nameLabel);

                    // Game mode title (e.g. "Realm of Shadows" or "Miss Friday's Adventure 2")
                    var modeLabel = Theme.MakeLabel(
                        summary.GameModeDisplayName, 120, 34, 250, 18,
                        9f, FontStyle.Italic, Theme.Gold);
                    card.Controls.Add(modeLabel);

                    // HP bar
                    var hpText = Theme.MakeLabel(
                        $"HP: {summary.Health}/{summary.MaxHealth}", 120, 54, 180, 20,
                        10f, FontStyle.Regular, Theme.HPGreen);
                    card.Controls.Add(hpText);

                    // Location
                    var locText = Theme.MakeLabel(
                        $"\u2302 {summary.RoomDisplayName}", 120, 74, 250, 20,
                        10f, FontStyle.Regular, Theme.ItemBlue);
                    card.Controls.Add(locText);

                    // Items count
                    var itemsText = Theme.MakeLabel(
                        $"\u2726 {summary.ItemCount} items", 380, 54, 120, 20,
                        10f, FontStyle.Regular, Color.LightGray);
                    card.Controls.Add(itemsText);

                    // Save date/time
                    var timeText = Theme.MakeLabel(
                        $"\U0001F4C5 {summary.SaveTime}", 380, 74, 220, 20,
                        9f, FontStyle.Regular, Color.DarkGray);
                    card.Controls.Add(timeText);

                    // Action button — Load or Overwrite depending on mode
                    if (mode == SlotMode.Load)
                    {
                        var loadBtn = Theme.MakeButton("\u25B6 PLAY", cardW - 200, 14, 100, 36,
                            (s, e) => LoadSlot(slot));
                        loadBtn.BackColor = Color.FromArgb(40, 110, 50);
                        card.Controls.Add(loadBtn);

                        // Delete button
                        var delBtn = Theme.MakeButton("\u2716 Delete", cardW - 200, 58, 100, 36,
                            (s, e) => ConfirmDelete(slot));
                        delBtn.BackColor = Color.FromArgb(120, 30, 30);
                        delBtn.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                        card.Controls.Add(delBtn);
                    }
                    else
                    {
                        // New Game mode — warn that this will overwrite
                        var overBtn = Theme.MakeButton("Overwrite", cardW - 200, 14, 100, 36,
                            (s, e) => ConfirmOverwrite(slot));
                        overBtn.BackColor = Color.FromArgb(160, 100, 20);
                        card.Controls.Add(overBtn);
                    }
                }
                else
                {
                    // ── Empty slot ────────────────────────────────────────
                    var emptyLabel = Theme.MakeLabel("— EMPTY —", 120, 30, 300, 30,
                        13f, FontStyle.Italic, Color.Gray);
                    card.Controls.Add(emptyLabel);

                    if (mode == SlotMode.NewGame)
                    {
                        var newBtn = Theme.MakeButton("New Game", cardW - 200, 30, 120, 40,
                            (s, e) => ShowNameEntry(slot));
                        newBtn.BackColor = Color.FromArgb(40, 110, 50);
                        card.Controls.Add(newBtn);
                    }
                }

                // Make the entire card clickable for convenience
                card.Click += (s, e) =>
                {
                    if (mode == SlotMode.Load && hasData)
                        LoadSlot(slot);
                    else if (mode == SlotMode.NewGame && !hasData)
                        ShowNameEntry(slot);
                };

                Controls.Add(card);
            }

            // Back button
            int backX = (880 - 200) / 2;
            var backBtn = Theme.MakeButton("\u2190 Back", backX, 500, 200, 40,
                (s, e) => BuildMenuUI());
            Controls.Add(backBtn);
        }

        /// <summary>Confirm before overwriting an existing save with a new game.</summary>
        private void ConfirmOverwrite(int slot)
        {
            var summary = SaveSystem.GetSlotSummary(slot);
            string name = summary != null ? summary.PlayerName : "???";
            var result = MessageBox.Show(
                $"Slot {slot} already has a save for \"{name}\".\n\n" +
                "Starting a new game will OVERWRITE this save.\nAre you sure?",
                "Overwrite Save?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
                ShowNameEntry(slot);
        }

        /// <summary>Confirm before deleting a save slot.</summary>
        private void ConfirmDelete(int slot)
        {
            var summary = SaveSystem.GetSlotSummary(slot);
            string name = summary != null ? summary.PlayerName : "???";
            var result = MessageBox.Show(
                $"Delete save \"{name}\" in Slot {slot}?\n\nThis cannot be undone!",
                "Delete Save?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                SaveSystem.DeleteSlot(slot);
                // Refresh the slot selection screen
                ShowSlotSelection(_slotMode);
            }
        }

        /// <summary>Load a save slot and enter the game.</summary>
        private void LoadSlot(int slot)
        {
            if (_main.Game.LoadGame(slot))
                _main.ShowScreen(new GameScreen(_main));
            else
                MessageBox.Show($"Failed to load Slot {slot}.", "Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ══════════════════════════════════════════════════════════════
        // STATE 3: Name Entry — enter name for new game
        // ══════════════════════════════════════════════════════════════

        /// <summary>Switch to name-entry view for the chosen save slot.</summary>
        private void ShowNameEntry(int slot)
        {
            _selectedSlot = slot;
            Controls.Clear();

            bool isFriday = _main.Game.Mode == GameMode.MissFriday;
            string gameTitle = isFriday ? "MISS FRIDAY'S ADVENTURE 2" : "REALM OF SHADOWS";
            string defaultName = isFriday ? "Miss Friday" : "Hero";

            // Title
            var titleLabel = Theme.MakeLabel(gameTitle, 0, 80, 880, 50,
                28f, FontStyle.Bold, isFriday ? Color.FromArgb(255, 180, 100) : Theme.Gold);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(titleLabel);

            // Slot indicator
            var slotLabel = Theme.MakeLabel($"Saving to Slot {slot}", 0, 140, 880, 25,
                11f, FontStyle.Italic, Color.LightGray);
            slotLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(slotLabel);

            // Name prompt
            var namePrompt = Theme.MakeLabel(
                isFriday ? "Confirm your pirate name:" : "Enter your name:",
                0, 200, 880, 30,
                14f, FontStyle.Regular, Theme.TextLight);
            namePrompt.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(namePrompt);

            var nameBox = new TextBox
            {
                Location = new Point(290, 240),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 14),
                BackColor = Theme.BgPanel,
                ForeColor = Theme.TextLight,
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 20,
                Text = defaultName,
                ReadOnly = isFriday  // Miss Friday mode locks the name
            };
            // Allow pressing Enter to begin
            nameBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BeginGame(nameBox.Text); };
            Controls.Add(nameBox);
            nameBox.SelectAll();
            nameBox.Focus();

            int cx = (880 - 250) / 2;
            var beginBtn = Theme.MakeButton("Begin Adventure", cx, 300, 250, 50,
                (s, e) => BeginGame(nameBox.Text));
            Controls.Add(beginBtn);

            var backBtn = Theme.MakeButton("\u2190 Back", cx, 370, 250, 40,
                (s, e) => ShowSlotSelection(SlotMode.NewGame));
            Controls.Add(backBtn);
        }

        /// <summary>Start a new game with the entered name and save to the selected slot.</summary>
        private void BeginGame(string rawName)
        {
            string name = rawName != null ? rawName.Trim() : "";
            if (string.IsNullOrEmpty(name)) name = "Hero";

            _main.Game.StartNewGame(name);
            _main.Game.ActiveSlot = _selectedSlot;
            // Auto-save to the chosen slot immediately so it appears occupied
            _main.Game.SaveGame(_selectedSlot);
            _main.ShowScreen(new GameScreen(_main));
        }
    }
}
