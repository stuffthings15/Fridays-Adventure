// ────────────────────────────────────────────────────────────
// TEXT RPG — Video Demo Mode (Live Visual Playthrough)
// Purpose: Auto-plays through the ACTUAL game screens to
//          demonstrate all core features for a 2–4 minute video.
//          Each step shows the REAL game UI (same controls,
//          same layouts, same theme) with automated interactions.
//          The player watches the game play itself visually.
// ────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TextRPG.Screens
{
    /// <summary>
    /// Visual demo that drives the ACTUAL game screens with automated
    /// input.  Each step embeds real WinForms controls (same Theme
    /// helpers used by the live game) so the viewer sees exactly what
    /// a human player would see.  A narration banner at the top
    /// describes what is happening, and a cursor/highlight effect
    /// shows which button is being "clicked".
    /// </summary>
    public class DemoScreen : UserControl
    {
        private readonly ITextRPGHost _main;

        // ── Game logic (real GameManager drives all data) ─────────────
        private GameManager _game;

        // ── Timer ─────────────────────────────────────────────────────
        private Timer _tickTimer;
        private float _stepElapsed;
        private int   _stepIndex = -1;
        private int   _subStep;          // sub-animations within a step

        // ── Layout panels ─────────────────────────────────────────────
        private Panel _bannerPanel;      // narration bar at the top
        private Label _bannerLabel;
        private Label _bannerStep;
        private Panel _progressBg;
        private Panel _progressFill;
        private Panel _contentPanel;     // hosts the current "screen"
        private Button _skipBtn;
        private Button _exitBtn;

        // ── Typing animation state ────────────────────────────────────
        private TextBox _typingBox;
        private string  _typingTarget = "";
        private int     _typingPos;

        // ── Combat animation state ────────────────────────────────────
        private RichTextBox _combatLog;
        private List<string> _combatLines;
        private int _combatLineIndex;
        private float _combatDelay;

        // Per-round HP snapshots so bars animate along with the log text.
        // Each entry stores (playerHP, playerMax, enemyHP, enemyMax) AFTER
        // that round's combat result was applied by the GameManager.
        private List<int[]> _combatHpSnapshots;

        // ── Visual action indicator ───────────────────────────────────
        private Label _actionLabel;    // shows "▶ clicking: Attack" etc.
        private Button _highlightBtn;  // button currently being "clicked"

        // ── Step table ────────────────────────────────────────────────
        private static readonly string[] StepTitles =
        {
            "1/11  TITLE SCREEN",
            "2/11  MODE SELECTION",
            "3/11  SAVE SLOT SELECTION",
            "4/11  NAME ENTRY",
            "5/11  VILLAGE SQUARE",
            "6/11  DARK FOREST — ITEM PICKUP",
            "7/11  GOBLIN CAVE — COMBAT",
            "8/11  NPC DIALOGUE",
            "9/11  SAVE GAME",
            "10/11 MISS FRIDAY MODE PREVIEW",
            "11/11 DEMO COMPLETE"
        };
        private static readonly string[] StepNarrations =
        {
            "The game opens to the main menu with New Game, Load Game, and Quit.",
            "Two game modes share the same engine — Classic RPG or Miss Friday's Adventure 2.",
            "Three save slots — pick one to start or continue, just like Zelda.",
            "The player types their character name before the adventure begins.",
            "Exploring the starting room — Village Square — with NPC Elder Mathis.",
            "Moving north to the Dark Forest. An Iron Sword is found and equipped!",
            "A Cave Goblin attacks! Watch the full turn-based combat play out.",
            "Speaking with Elder Mathis — branching dialogue options.",
            "The player saves progress to Slot 1. All state is written to disk.",
            "Miss Friday mode: same engine, richer narrative, unique NPCs, pirate setting.",
            "All required features demonstrated across both modes. Demo complete!"
        };
        private static readonly float[] StepDurations =
        {
            5f, 6f, 5f, 7f, 7f, 8f, 14f, 10f, 6f, 8f, 8f
        };

        public DemoScreen(ITextRPGHost main)
        {
            _main = main;
            BackColor = Theme.BgDark;
            Dock = DockStyle.Fill;
            _game = new GameManager();
            BuildShell();
            StartDemo();
        }

        // ══════════════════════════════════════════════════════════════
        // SHELL — banner + content area + controls (always visible)
        // ══════════════════════════════════════════════════════════════

        private void BuildShell()
        {
            // ── Banner (top 52 px) ────────────────────────────────────
            _bannerPanel = new Panel
            {
                Location = new Point(0, 0), Size = new Size(880, 52),
                BackColor = Color.FromArgb(15, 15, 30)
            };
            Controls.Add(_bannerPanel);

            var badge = Theme.MakeLabel("\u25B6 VIDEO DEMO MODE", 10, 4, 220, 20,
                11f, FontStyle.Bold, Color.OrangeRed);
            _bannerPanel.Controls.Add(badge);

            _bannerStep = Theme.MakeLabel("", 240, 4, 400, 20,
                10f, FontStyle.Bold, Theme.Gold);
            _bannerPanel.Controls.Add(_bannerStep);

            _bannerLabel = Theme.MakeLabel("", 10, 26, 860, 22,
                10f, FontStyle.Italic, Color.LightGray);
            _bannerPanel.Controls.Add(_bannerLabel);

            // ── Progress bar (52–56 px) ───────────────────────────────
            _progressBg = new Panel
            {
                Location = new Point(0, 52), Size = new Size(880, 4),
                BackColor = Color.FromArgb(40, 40, 60)
            };
            Controls.Add(_progressBg);
            _progressFill = new Panel
            {
                Location = new Point(0, 0), Size = new Size(0, 4),
                BackColor = Color.OrangeRed
            };
            _progressBg.Controls.Add(_progressFill);

            // ── Content area (56–575 px) ──────────────────────────────
            _contentPanel = new Panel
            {
                Location = new Point(0, 56), Size = new Size(880, 520),
                BackColor = Theme.BgDark
            };
            Controls.Add(_contentPanel);

            // ── Bottom controls ───────────────────────────────────────
            _skipBtn = Theme.MakeButton("\u23E9 Skip", 600, 580, 120, 34,
                (s, e) => AdvanceStep());
            Controls.Add(_skipBtn);

            _exitBtn = Theme.MakeButton("Exit Demo", 730, 580, 130, 34,
                (s, e) => ExitDemo());
            _exitBtn.BackColor = Color.FromArgb(120, 30, 30);
            Controls.Add(_exitBtn);
        }

        // ══════════════════════════════════════════════════════════════
        // PLAYBACK CONTROL
        // ══════════════════════════════════════════════════════════════

        private void StartDemo()
        {
            _tickTimer = new Timer { Interval = 100 };
            _tickTimer.Tick += OnTick;
            _tickTimer.Start();
            AdvanceStep();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (_stepIndex >= StepTitles.Length) return;
            _stepElapsed += 0.1f;

            // Update progress fill
            float pct = _stepElapsed / StepDurations[_stepIndex];
            if (pct > 1f) pct = 1f;
            float overall = (_stepIndex + pct) / StepTitles.Length;
            _progressFill.Width = (int)(880 * overall);

            // Per-step animations
            TickAnimation();

            // Auto-advance
            if (_stepElapsed >= StepDurations[_stepIndex])
                AdvanceStep();
        }

        private void AdvanceStep()
        {
            _stepIndex++;
            _stepElapsed = 0f;
            _subStep     = 0;

            if (_stepIndex >= StepTitles.Length)
            {
                _tickTimer?.Stop();
                _skipBtn.Enabled = false;
                _skipBtn.Text = "\u2705 Done";
                return;
            }

            _bannerStep.Text  = StepTitles[_stepIndex];
            _bannerLabel.Text = StepNarrations[_stepIndex];
            _contentPanel.Controls.Clear();
            _actionLabel   = null;
            _highlightBtn  = null;

            switch (_stepIndex)
            {
                case 0:  BuildTitleScreen();       break;
                case 1:  BuildModeSelectScreen();  break;
                case 2:  BuildSlotScreen();        break;
                case 3:  BuildNameEntry();         break;
                case 4:  BuildGameScreen(false);   break;
                case 5:  BuildForestScreen();      break;
                case 6:  BuildCombatScreen();      break;
                case 7:  BuildDialogueScreen();    break;
                case 8:  BuildSaveScreen();        break;
                case 9:  BuildFridayPreview();     break;
                case 10: BuildCompleteScreen();    break;
            }
        }

        /// <summary>Per-tick animations (typing, combat rounds, action highlights).</summary>
        private void TickAnimation()
        {
            // ── Name typing animation ─────────────────────────────────
            if (_stepIndex == 3 && _typingBox != null && _typingPos < _typingTarget.Length)
            {
                if (_stepElapsed > 1.5f + _typingPos * 0.4f)
                {
                    _typingPos++;
                    _typingBox.Text = _typingTarget.Substring(0, _typingPos);
                }
            }

            // ── Combat round animation ────────────────────────────────
            if (_stepIndex == 6 && _combatLog != null && _combatLines != null)
            {
                _combatDelay += 0.1f;
                if (_combatDelay >= 1.8f && _combatLineIndex < _combatLines.Count)
                {
                    _combatLog.AppendText(_combatLines[_combatLineIndex] + "\n");
                    _combatLog.ScrollToCaret();
                    _combatLineIndex++;
                    _combatDelay = 0f;

                    // Flash the Attack button to show it being "clicked"
                    if (_highlightBtn != null)
                    {
                        _highlightBtn.BackColor = Color.FromArgb(140, 100, 50);
                        var flashTimer = new Timer { Interval = 250 };
                        var btn = _highlightBtn;
                        flashTimer.Tick += (s, ev) => { flashTimer.Stop(); flashTimer.Dispose();
                            if (!btn.IsDisposed) btn.BackColor = Theme.BtnBg; };
                        flashTimer.Start();
                    }

                    // Update HP bars from snapshot (not from final mutated state)
                    UpdateCombatBarsFromSnapshot(_combatLineIndex);
                }
            }

            // ── Action indicator blink ─────────────────────────────────
            if (_actionLabel != null)
            {
                bool show = ((int)(_stepElapsed / 0.5f)) % 2 == 0;
                _actionLabel.Visible = show;
            }
        }

        private void ExitDemo()
        {
            _tickTimer?.Stop();
            _tickTimer?.Dispose();
            _main.ShowScreen(new TitleScreen(_main));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _tickTimer?.Stop(); _tickTimer?.Dispose(); }
            base.Dispose(disposing);
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 0: TITLE SCREEN — real layout
        // ══════════════════════════════════════════════════════════════

        private void BuildTitleScreen()
        {
            var p = _contentPanel;
            int cx = (880 - 250) / 2;

            var title = Theme.MakeLabel("REALM OF SHADOWS", 0, 60, 880, 50,
                30f, FontStyle.Bold, Theme.Gold);
            title.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(title);

            var sub = Theme.MakeLabel("A Text Adventure", 0, 110, 880, 25,
                13f, FontStyle.Italic, Theme.TextLight);
            sub.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(sub);

            // Highlight "New Game" as the button being clicked
            var newGameBtn = MakeBtn("New Game",  cx, 190, 250, 48,
                Color.FromArgb(40, 110, 50));
            p.Controls.Add(newGameBtn);
            p.Controls.Add(MakeBtn("Load Game", cx, 250, 250, 48));
            p.Controls.Add(MakeBtn("Quit",      cx, 310, 250, 48));
            p.Controls.Add(MakeBtn("\u25B6 Video Demo Mode", cx, 380, 250, 48,
                Color.FromArgb(140, 80, 20)));

            _actionLabel = Theme.MakeLabel("\u25B6 Player clicks: New Game", 0, 445, 880, 20,
                10f, FontStyle.Bold, Color.Orange);
            _actionLabel.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(_actionLabel);

            var credits = Theme.MakeLabel("CS-120 Project \u2014 .NET Framework 4.7.2 \u2014 WinForms",
                0, 470, 880, 20, 9f, FontStyle.Regular, Color.Gray);
            credits.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(credits);
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 1: MODE SELECTION — RPG or Miss Friday
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Shows the mode picker: same engine, two presentations.
        /// Highlights the RPG card as the selected mode for the demo flow.
        /// </summary>
        private void BuildModeSelectScreen()
        {
            var p = _contentPanel;

            var header = Theme.MakeLabel("\u2694 CHOOSE YOUR MODE \u2694", 0, 15, 880, 40,
                22f, FontStyle.Bold, Theme.Gold);
            header.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(header);

            var sub = Theme.MakeLabel("Both modes use the same engine — different stories and presentation.",
                0, 58, 880, 22, 10f, FontStyle.Italic, Color.LightGray);
            sub.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(sub);

            int cx = (880 - 500) / 2;

            // ── RPG Mode card (highlighted as selected) ───────────────
            var rpgCard = new Panel
            {
                Location = new Point(cx, 95), Size = new Size(500, 110),
                BackColor = Color.FromArgb(35, 55, 75), BorderStyle = BorderStyle.FixedSingle
            };
            rpgCard.Controls.Add(Theme.MakeLabel("\u2694 Realm of Shadows", 14, 6, 470, 28,
                14f, FontStyle.Bold, Theme.Gold));
            rpgCard.Controls.Add(Theme.MakeLabel(
                "Classic text RPG — you choose your name and explore a dark " +
                "fantasy world of goblins, trolls, and dragons.",
                14, 38, 470, 50, 10f, FontStyle.Regular, Theme.TextLight));
            // Gold border highlights the selected card
            var rpgHighlight = new Panel
            {
                Location = new Point(0, 0), Size = new Size(500, 110),
                BackColor = Color.Transparent
            };
            rpgHighlight.Paint += (s, ev) =>
            {
                using (var pen = new Pen(Color.Gold, 3))
                    ev.Graphics.DrawRectangle(pen, 1, 1, 497, 107);
            };
            rpgCard.Controls.Add(rpgHighlight);
            rpgHighlight.BringToFront();
            p.Controls.Add(rpgCard);

            // ── Miss Friday Mode card ─────────────────────────────────
            var fridayCard = new Panel
            {
                Location = new Point(cx, 220), Size = new Size(500, 110),
                BackColor = Color.FromArgb(45, 30, 50), BorderStyle = BorderStyle.FixedSingle
            };
            fridayCard.Controls.Add(Theme.MakeLabel("\u2693 Miss Friday's Adventure 2", 14, 6, 470, 28,
                14f, FontStyle.Bold, Color.FromArgb(255, 180, 100)));
            fridayCard.Controls.Add(Theme.MakeLabel(
                "Play as Miss Friday — pirate captain with narrative-rich descriptions, " +
                "unique NPCs (Captain Crow), and a coastal setting.",
                14, 38, 470, 50, 10f, FontStyle.Regular, Theme.TextLight));
            p.Controls.Add(fridayCard);

            // ── Shared engine note ────────────────────────────────────
            var shared = Theme.MakeLabel(
                "Shared: GameManager, Player, Enemy, Room, Item, Combat, Inventory, Save/Load",
                0, 350, 880, 20, 9f, FontStyle.Italic, Color.Gray);
            shared.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(shared);

            _actionLabel = Theme.MakeLabel("\u25B6 Player selects: Realm of Shadows (Classic RPG Mode)",
                0, 390, 880, 20, 10f, FontStyle.Bold, Color.Orange);
            _actionLabel.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(_actionLabel);
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 2: SAVE SLOT SELECTION — 3 Zelda-style cards
        // ══════════════════════════════════════════════════════════════

        private void BuildSlotScreen()
        {
            var p = _contentPanel;
            var header = Theme.MakeLabel("\u2694 CHOOSE A SLOT \u2694", 0, 15, 880, 35,
                18f, FontStyle.Bold, Theme.Gold);
            header.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(header);

            int cardW = 760, cardH = 100, startX = (880 - cardW) / 2;
            for (int i = 1; i <= 3; i++)
            {
                int cy = 70 + (i - 1) * (cardH + 12);
                var card = new Panel
                {
                    Location = new Point(startX, cy),
                    Size = new Size(cardW, cardH),
                    BackColor = i == 1 ? Color.FromArgb(55, 58, 75) : Color.FromArgb(35, 35, 45),
                    BorderStyle = BorderStyle.FixedSingle
                };
                card.Controls.Add(Theme.MakeLabel($"SLOT {i}", 14, 10, 80, 25,
                    13f, FontStyle.Bold, Theme.Gold));
                card.Controls.Add(Theme.MakeLabel("\u2014 EMPTY \u2014", 110, 30, 200, 25,
                    12f, FontStyle.Italic, Color.Gray));

                if (i == 1)
                {
                    // Highlight slot 1 — "selected"
                    var btn = MakeBtn("New Game", cardW - 160, 30, 130, 38,
                        Color.FromArgb(40, 110, 50));
                    card.Controls.Add(btn);
                    // Animated highlight border
                    var highlight = new Panel
                    {
                        Location = new Point(0, 0), Size = new Size(cardW, cardH),
                        BackColor = Color.Transparent
                    };
                    highlight.Paint += (s, ev) =>
                    {
                        using (var pen = new Pen(Color.Gold, 3))
                            ev.Graphics.DrawRectangle(pen, 1, 1, cardW - 3, cardH - 3);
                    };
                    card.Controls.Add(highlight);
                    highlight.BringToFront();
                }
                p.Controls.Add(card);
            }

            var back = MakeBtn("\u2190 Back", (880 - 200) / 2, 420, 200, 38);
            p.Controls.Add(back);

            _actionLabel = Theme.MakeLabel("\u25B6 Player selects: Slot 1 \u2192 New Game", 0, 460, 880, 20,
                10f, FontStyle.Bold, Color.Orange);
            _actionLabel.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(_actionLabel);
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 2: NAME ENTRY — animated typing
        // ══════════════════════════════════════════════════════════════

        private void BuildNameEntry()
        {
            var p = _contentPanel;

            var title = Theme.MakeLabel("REALM OF SHADOWS", 0, 30, 880, 40,
                24f, FontStyle.Bold, Theme.Gold);
            title.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(title);

            var slot = Theme.MakeLabel("Saving to Slot 1", 0, 80, 880, 22,
                10f, FontStyle.Italic, Color.LightGray);
            slot.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(slot);

            var prompt = Theme.MakeLabel("Enter your name:", 0, 140, 880, 25,
                13f, FontStyle.Regular, Theme.TextLight);
            prompt.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(prompt);

            _typingBox = new TextBox
            {
                Location = new Point(290, 175),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 14),
                BackColor = Theme.BgPanel,
                ForeColor = Theme.TextLight,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                Text = ""
            };
            p.Controls.Add(_typingBox);
            _typingTarget = "Luffy";
            _typingPos = 0;

            p.Controls.Add(MakeBtn("Begin Adventure", (880 - 250) / 2, 230, 250, 48,
                Color.FromArgb(40, 110, 50)));
            p.Controls.Add(MakeBtn("\u2190 Back", (880 - 250) / 2, 295, 250, 38));

            _actionLabel = Theme.MakeLabel("\u25B6 Player types \"Luffy\" \u2192 clicks Begin Adventure", 0, 340, 880, 20,
                10f, FontStyle.Bold, Color.Orange);
            _actionLabel.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(_actionLabel);

            // Start the game behind the scenes
            _game.StartNewGame("Luffy");
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 3: GAME SCREEN — Village Square (real layout)
        // ══════════════════════════════════════════════════════════════

        private void BuildGameScreen(bool showSaveToast)
        {
            var p = _contentPanel;
            var pl = _game.Player;
            var room = _game.CurrentRoom;

            // ── HUD bar ───────────────────────────────────────────────
            var hud = new Panel
            {
                Location = new Point(10, 5), Size = new Size(860, 56),
                BackColor = Theme.BgPanel
            };
            p.Controls.Add(hud);
            hud.Controls.Add(Theme.MakeLabel(pl.Name, 10, 4, 160, 22,
                12f, FontStyle.Bold, Theme.Gold));
            hud.Controls.Add(Theme.MakeLabel("HP:", 10, 30, 28, 18, 10f));
            var hpBg = new Panel { Location = new Point(40, 32), Size = new Size(200, 14),
                BackColor = Color.FromArgb(60, 20, 20) };
            hud.Controls.Add(hpBg);
            float hpPct = (float)pl.Health / pl.MaxHealth;
            var hpFill = new Panel { Location = new Point(0, 0),
                Size = new Size((int)(200 * hpPct), 14),
                BackColor = hpPct > 0.5f ? Theme.HPGreen : Color.Orange };
            hpBg.Controls.Add(hpFill);
            hud.Controls.Add(Theme.MakeLabel($"{pl.Health}/{pl.MaxHealth}", 246, 30, 80, 18, 10f));
            hud.Controls.Add(Theme.MakeLabel($"ATK: {pl.TotalAttack}" +
                (pl.EquippedWeapon != null ? $" ({pl.EquippedWeapon.Name})" : ""),
                380, 8, 260, 18, 10f, FontStyle.Regular, Theme.ItemBlue));
            hud.Controls.Add(Theme.MakeLabel($"DEF: {pl.TotalDefense}" +
                (pl.EquippedArmor != null ? $" ({pl.EquippedArmor.Name})" : ""),
                380, 30, 260, 18, 10f, FontStyle.Regular, Theme.ItemBlue));

            // ── Room title ────────────────────────────────────────────
            p.Controls.Add(Theme.MakeLabel(room.Name, 10, 66, 860, 28,
                15f, FontStyle.Bold, Theme.Gold));

            // ── Room description ──────────────────────────────────────
            var desc = new RichTextBox
            {
                Location = new Point(10, 98), Size = new Size(860, 200),
                BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 11), ReadOnly = true,
                BorderStyle = BorderStyle.None
            };
            desc.Text = room.Description;
            p.Controls.Add(desc);

            // ── Navigation buttons ────────────────────────────────────
            int ny = 310;
            p.Controls.Add(Theme.MakeLabel("Navigate:", 10, ny + 4, 80, 28, 10f,
                FontStyle.Regular, Color.Gray));
            var dirs = new[] { Direction.North, Direction.South, Direction.East, Direction.West };
            var arrows = new[] { "\u2191 North", "\u2193 South", "\u2192 East", "\u2190 West" };
            for (int i = 0; i < 4; i++)
            {
                var btn = MakeBtn(arrows[i], 100 + i * 130, ny, 120, 36);
                btn.Enabled = room.Exits.ContainsKey(dirs[i]);
                p.Controls.Add(btn);
            }

            // ── Action buttons ────────────────────────────────────────
            int ay = 358;
            if (room.Npc != null)
                p.Controls.Add(MakeBtn("\U0001F4AC Talk", 100, ay, 120, 36));
            if (!string.IsNullOrEmpty(room.PortalTargetId))
                p.Controls.Add(MakeBtn("\u2728 Portal", 230, ay, 120, 36));
            p.Controls.Add(MakeBtn("\U0001F392 Inventory", 360, ay, 150, 36));
            p.Controls.Add(MakeBtn($"\U0001F4BE Save (Slot {_game.ActiveSlot})", 520, ay, 150, 36));

            // ── Status label ──────────────────────────────────────────
            if (showSaveToast)
            {
                var toast = Theme.MakeLabel("\u2705 Game saved to Slot 1!", 10, 410, 860, 25,
                    11f, FontStyle.Bold, Color.LimeGreen);
                p.Controls.Add(toast);

                _actionLabel = Theme.MakeLabel("\u25B6 Player clicks: Save (Slot 1) \u2014 state written to disk",
                    10, 440, 860, 20, 10f, FontStyle.Bold, Color.Orange);
                p.Controls.Add(_actionLabel);
            }
            else
            {
                _actionLabel = Theme.MakeLabel("\u25B6 Player explores the room \u2014 reads description, sees exits and NPC",
                    10, 410, 860, 20, 10f, FontStyle.Bold, Color.Orange);
                p.Controls.Add(_actionLabel);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 4: DARK FOREST — item pickup + inventory
        // ══════════════════════════════════════════════════════════════

        private void BuildForestScreen()
        {
            // Move the game state forward
            var moveResult = _game.MovePlayer(Direction.North);
            var sword = _game.Player.Inventory.Find(i => i.Name == "Iron Sword");
            if (sword != null) _game.EquipItem(sword);

            var p = _contentPanel;
            var pl = _game.Player;
            var room = _game.CurrentRoom;

            // ── HUD (same as game screen, with updated ATK) ───────────
            BuildHudBar(p, pl);

            // ── Room ──────────────────────────────────────────────────
            p.Controls.Add(Theme.MakeLabel(room.Name, 10, 66, 860, 28,
                15f, FontStyle.Bold, Theme.Gold));

            var desc = new RichTextBox
            {
                Location = new Point(10, 98), Size = new Size(860, 130),
                BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 11), ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Text = room.Description
            };
            p.Controls.Add(desc);

            // ── Item pickup toast ─────────────────────────────────────
            var pickupPanel = new Panel
            {
                Location = new Point(40, 240), Size = new Size(800, 60),
                BackColor = Color.FromArgb(20, 50, 30)
            };
            p.Controls.Add(pickupPanel);
            pickupPanel.Controls.Add(Theme.MakeLabel("\u2726 Found: Iron Sword \u2014 A sturdy blade with a leather grip (+7 ATK)",
                10, 6, 780, 22, 11f, FontStyle.Bold, Theme.Gold));
            pickupPanel.Controls.Add(Theme.MakeLabel("Equipped Iron Sword. Attack is now " + pl.TotalAttack + ".",
                10, 32, 780, 20, 10f, FontStyle.Regular, Color.LimeGreen));

            // ── Mini inventory panel ──────────────────────────────────
            var invPanel = new Panel
            {
                Location = new Point(40, 315), Size = new Size(800, 130),
                BackColor = Theme.BgPanel
            };
            p.Controls.Add(invPanel);
            invPanel.Controls.Add(Theme.MakeLabel("\U0001F392 INVENTORY", 10, 6, 300, 24,
                12f, FontStyle.Bold, Theme.Gold));
            int iy = 34;
            foreach (var item in pl.Inventory)
            {
                string eq = item == pl.EquippedWeapon ? "  [EQUIPPED]" : "";
                invPanel.Controls.Add(Theme.MakeLabel(
                    $"\u2726 {item.Name} \u2014 {item.Description}{eq}",
                    20, iy, 760, 20, 10f, FontStyle.Regular, Theme.TextLight));
                iy += 22;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 5: COMBAT — animated round-by-round (real CombatScreen)
        // ══════════════════════════════════════════════════════════════

        private Panel _playerBarFill, _enemyBarFill;
        private Label _playerHPLabel, _enemyHPLabel;
        private int   _combatPlayerHP, _combatEnemyHP;
        private int   _combatPlayerMax, _combatEnemyMax;

        private void BuildCombatScreen()
        {
            // Move east into Goblin Cave (triggers combat)
            _game.MovePlayer(Direction.East);
            var enemy = _game.CurrentEnemy;
            var pl    = _game.Player;
            var p     = _contentPanel;

            _combatPlayerHP  = pl.Health;
            _combatPlayerMax = pl.MaxHealth;
            _combatEnemyHP   = enemy.Health;
            _combatEnemyMax  = enemy.MaxHealth;

            // ── Header ────────────────────────────────────────────────
            var header = Theme.MakeLabel("\u2694  COMBAT  \u2694", 0, 4, 880, 28,
                15f, FontStyle.Bold, Theme.Gold);
            header.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(header);

            // ── Player panel (left) ───────────────────────────────────
            var pPanel = new Panel { Location = new Point(20, 38), Size = new Size(400, 72),
                BackColor = Theme.BgPanel };
            p.Controls.Add(pPanel);
            pPanel.Controls.Add(Theme.MakeLabel(pl.Name, 10, 4, 380, 22,
                12f, FontStyle.Bold, Theme.ItemBlue));
            pPanel.Controls.Add(Theme.MakeLabel("HP:", 10, 30, 28, 16, 10f));
            var ppBg = new Panel { Location = new Point(40, 32), Size = new Size(280, 14),
                BackColor = Color.FromArgb(60, 20, 20) };
            pPanel.Controls.Add(ppBg);
            _playerBarFill = new Panel { Location = new Point(0, 0),
                Size = new Size(280, 14), BackColor = Theme.HPGreen };
            ppBg.Controls.Add(_playerBarFill);
            _playerHPLabel = Theme.MakeLabel($"{pl.Health}/{pl.MaxHealth}", 326, 30, 70, 16, 10f);
            pPanel.Controls.Add(_playerHPLabel);
            pPanel.Controls.Add(Theme.MakeLabel($"ATK: {pl.TotalAttack}  DEF: {pl.TotalDefense}",
                10, 50, 300, 16, 9f, FontStyle.Regular, Color.Gray));

            // ── Enemy panel (right) ───────────────────────────────────
            var ePanel = new Panel { Location = new Point(460, 38), Size = new Size(400, 72),
                BackColor = Theme.BgPanel };
            p.Controls.Add(ePanel);
            ePanel.Controls.Add(Theme.MakeLabel(enemy.Name, 10, 4, 380, 22,
                12f, FontStyle.Bold, Theme.HPRed));
            ePanel.Controls.Add(Theme.MakeLabel("HP:", 10, 30, 28, 16, 10f));
            var epBg = new Panel { Location = new Point(40, 32), Size = new Size(280, 14),
                BackColor = Color.FromArgb(60, 20, 20) };
            ePanel.Controls.Add(epBg);
            _enemyBarFill = new Panel { Location = new Point(0, 0),
                Size = new Size(280, 14), BackColor = Theme.HPRed };
            epBg.Controls.Add(_enemyBarFill);
            _enemyHPLabel = Theme.MakeLabel($"{enemy.Health}/{enemy.MaxHealth}", 326, 30, 70, 16, 10f);
            ePanel.Controls.Add(_enemyHPLabel);
            ePanel.Controls.Add(Theme.MakeLabel($"ATK: {enemy.Attack}  DEF: {enemy.Defense}",
                10, 50, 300, 16, 9f, FontStyle.Regular, Color.Gray));

            // ── VS ────────────────────────────────────────────────────
            var vs = Theme.MakeLabel("VS", 410, 58, 50, 26, 13f, FontStyle.Bold, Theme.Gold);
            vs.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(vs);

            // ── Combat log ────────────────────────────────────────────
            _combatLog = new RichTextBox
            {
                Location = new Point(20, 120), Size = new Size(840, 290),
                BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                Font = new Font("Consolas", 11), ReadOnly = true,
                BorderStyle = BorderStyle.None
            };
            _combatLog.AppendText($"A wild {enemy.Name} appears!\n\n");
            p.Controls.Add(_combatLog);

            // ── Action buttons (visual only — demo auto-plays) ────────
            int by = 420;
            var atkBtn = MakeBtn("\u2694 Attack", 120, by, 180, 44);
            p.Controls.Add(atkBtn);
            _highlightBtn = atkBtn;  // flash this button each round
            p.Controls.Add(MakeBtn("\u2764 Use Potion", 340, by, 180, 44));
            p.Controls.Add(MakeBtn("\U0001F3C3 Flee", 560, by, 180, 44));

            // ── Action indicator ──────────────────────────────────────
            _actionLabel = Theme.MakeLabel("\u25B6 Bot clicks: Attack", 20, 470, 300, 20,
                10f, FontStyle.Bold, Color.Orange);
            p.Controls.Add(_actionLabel);

            // ── Pre-compute all combat rounds + HP snapshots ──────────
            _combatLines       = new List<string>();
            _combatHpSnapshots = new List<int[]>();
            int round = 1;
            while (_game.CurrentEnemy != null && _game.CurrentEnemy.IsAlive
                   && _game.Player.Health > 0 && round <= 10)
            {
                var cr = _game.PlayerAttack();
                _combatLines.Add($"── Round {round} ──");
                foreach (var line in cr.Log.Split('\n'))
                    if (!string.IsNullOrEmpty(line.Trim()))
                        _combatLines.Add("  " + line.Trim());
                _combatLines.Add("");

                // Snapshot HP AFTER this round so bars animate per-round
                _combatHpSnapshots.Add(new[]
                {
                    _game.Player.Health, _game.Player.MaxHealth,
                    _game.CurrentEnemy != null ? _game.CurrentEnemy.Health : 0,
                    _game.CurrentEnemy != null ? _game.CurrentEnemy.MaxHealth : 1
                });

                if (cr.EnemyDefeated)
                    _combatLines.Add("\u2605 Enemy defeated! Returning to exploration...");
                if (cr.Victory)
                    _combatLines.Add("\u2728 VICTORY! The Shadow Dragon is slain!");
                if (cr.EnemyDefeated || cr.PlayerDied || cr.Victory) break;
                round++;
            }
            _combatLineIndex = 0;
            _combatDelay = 0f;
        }

        /// <summary>
        /// Update HP bars from the per-round snapshot list so bars
        /// animate in sync with the scrolling combat log — not from
        /// the final mutated GameManager state.
        /// </summary>
        private void UpdateCombatBarsFromSnapshot(int lineIndex)
        {
            if (_playerBarFill == null || _combatHpSnapshots == null) return;

            // Map the current line index to the closest round snapshot.
            // Snapshots are stored once per round (after each "── Round N ──").
            // Lines between rounds should keep showing the previous round's HP.
            int snapIdx = -1;
            int roundsSeen = 0;
            for (int i = 0; i < lineIndex && i < _combatLines.Count; i++)
                if (_combatLines[i].StartsWith("── Round")) { snapIdx = roundsSeen; roundsSeen++; }

            if (snapIdx < 0 || snapIdx >= _combatHpSnapshots.Count) return;

            int[] snap = _combatHpSnapshots[snapIdx];
            int pHP = snap[0], pMax = snap[1], eHP = snap[2], eMax = snap[3];

            float pp = pMax > 0 ? (float)pHP / pMax : 0f;
            _playerBarFill.Width = Math.Max(0, (int)(280 * pp));
            _playerBarFill.BackColor = pp > 0.5f ? Theme.HPGreen
                : pp > 0.25f ? Color.Orange : Theme.HPRed;
            _playerHPLabel.Text = $"{pHP}/{pMax}";

            if (eMax > 0)
            {
                float ep = (float)eHP / eMax;
                if (ep < 0f) ep = 0f;
                _enemyBarFill.Width = Math.Max(0, (int)(280 * ep));
                _enemyHPLabel.Text = $"{eHP}/{eMax}";
            }
            else
            {
                _enemyBarFill.Width = 0;
                _enemyHPLabel.Text = "0/0 \u2605";
            }
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 6: NPC DIALOGUE — real layout
        // ══════════════════════════════════════════════════════════════

        private void BuildDialogueScreen()
        {
            // Navigate back to village: West from cave → forest, South → village
            _game.MovePlayer(Direction.West);
            _game.MovePlayer(Direction.South);
            var npc = _game.CurrentRoom.Npc;
            var p   = _contentPanel;

            // ── NPC banner ────────────────────────────────────────────
            var banner = new Panel
            {
                Location = new Point(20, 10), Size = new Size(840, 46),
                BackColor = Theme.BgPanel
            };
            p.Controls.Add(banner);
            banner.Controls.Add(Theme.MakeLabel("\U0001F464 " + npc.Name, 14, 8, 810, 30,
                15f, FontStyle.Bold, Theme.Gold));

            // ── Greeting text ─────────────────────────────────────────
            var greet = new RichTextBox
            {
                Location = new Point(20, 70), Size = new Size(840, 120),
                BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 12), ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Text = npc.Greeting
            };
            p.Controls.Add(greet);

            // ── Dialogue options ──────────────────────────────────────
            var optsPanel = new Panel
            {
                Location = new Point(20, 200), Size = new Size(840, 260),
                BackColor = Color.Transparent
            };
            p.Controls.Add(optsPanel);

            int y = 6;
            for (int i = 0; i < npc.Options.Count; i++)
            {
                var btn = MakeBtn($"\u25B6 {npc.Options[i].Text}", 40, y, 760, 42);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(10, 0, 0, 0);
                // Highlight first option as if player clicked it
                if (i == 0) btn.BackColor = Color.FromArgb(60, 90, 120);
                optsPanel.Controls.Add(btn);
                y += 52;
            }

            // ── Simulated response (from option 1) ────────────────────
            var resp = Theme.MakeLabel(
                $"{npc.Name}: \"{npc.Options[0].Response}\"",
                20, 460, 840, 30, 11f, FontStyle.Italic, Theme.TextLight);
            p.Controls.Add(resp);

            _actionLabel = Theme.MakeLabel("\u25B6 Player clicks: Talk \u2192 selects dialogue option 1",
                20, 492, 840, 20, 10f, FontStyle.Bold, Color.Orange);
            p.Controls.Add(_actionLabel);
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 7: SAVE GAME — game screen with save toast
        // ══════════════════════════════════════════════════════════════

        private void BuildSaveScreen()
        {
            BuildGameScreen(showSaveToast: true);
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 9: MISS FRIDAY MODE PREVIEW — same engine, pirate flavor
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Shows what Miss Friday's Adventure 2 mode looks like:
        /// a second GameManager in MissFriday mode displays the Harbor
        /// Docks room with Captain Crow's unique NPC dialogue.
        /// Proves both modes share the same engine.
        /// </summary>
        private void BuildFridayPreview()
        {
            // Spin up a second GameManager in Miss Friday mode to show the
            // alternate world — this proves both modes use the same engine.
            var fridayGame = new GameManager { Mode = GameMode.MissFriday };
            fridayGame.StartNewGame("Miss Friday");

            var p   = _contentPanel;
            var pl  = fridayGame.Player;
            var room = fridayGame.CurrentRoom;

            // ── Mode badge ────────────────────────────────────────────
            var badge = new Panel
            {
                Location = new Point(10, 4), Size = new Size(860, 32),
                BackColor = Color.FromArgb(55, 35, 60)
            };
            badge.Controls.Add(Theme.MakeLabel("\u2693 MISS FRIDAY'S ADVENTURE 2 — Same Engine, Different Story",
                10, 4, 840, 24, 12f, FontStyle.Bold, Color.FromArgb(255, 180, 100)));
            p.Controls.Add(badge);

            // ── HUD bar ───────────────────────────────────────────────
            var hud = new Panel
            {
                Location = new Point(10, 42), Size = new Size(860, 50),
                BackColor = Theme.BgPanel
            };
            p.Controls.Add(hud);
            hud.Controls.Add(Theme.MakeLabel(pl.Name, 10, 4, 200, 22,
                12f, FontStyle.Bold, Color.FromArgb(255, 180, 100)));
            hud.Controls.Add(Theme.MakeLabel("HP:", 10, 28, 28, 18, 10f));
            var hpBg = new Panel { Location = new Point(40, 30), Size = new Size(200, 12),
                BackColor = Color.FromArgb(60, 20, 20) };
            hud.Controls.Add(hpBg);
            hpBg.Controls.Add(new Panel { Location = new Point(0, 0),
                Size = new Size(200, 12), BackColor = Theme.HPGreen });
            hud.Controls.Add(Theme.MakeLabel($"{pl.Health}/{pl.MaxHealth}", 246, 28, 80, 16, 10f));
            hud.Controls.Add(Theme.MakeLabel($"ATK: {pl.TotalAttack}  DEF: {pl.TotalDefense}",
                380, 14, 260, 18, 10f, FontStyle.Regular, Theme.ItemBlue));

            // ── Room title + description ──────────────────────────────
            p.Controls.Add(Theme.MakeLabel(room.Name, 10, 98, 860, 26,
                14f, FontStyle.Bold, Color.FromArgb(255, 180, 100)));

            var desc = new RichTextBox
            {
                Location = new Point(10, 128), Size = new Size(860, 100),
                BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 10.5f), ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Text = room.Description
            };
            p.Controls.Add(desc);

            // ── Captain Crow NPC dialogue preview ─────────────────────
            if (room.Npc != null)
            {
                var npc = room.Npc;
                var npcBanner = new Panel
                {
                    Location = new Point(20, 236), Size = new Size(840, 36),
                    BackColor = Color.FromArgb(50, 40, 55)
                };
                npcBanner.Controls.Add(Theme.MakeLabel("\U0001F464 " + npc.Name, 10, 6, 400, 24,
                    12f, FontStyle.Bold, Color.FromArgb(255, 180, 100)));
                p.Controls.Add(npcBanner);

                var npcText = new RichTextBox
                {
                    Location = new Point(20, 276), Size = new Size(840, 70),
                    BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                    Font = new Font("Segoe UI", 10.5f), ReadOnly = true,
                    BorderStyle = BorderStyle.None,
                    Text = npc.Greeting
                };
                p.Controls.Add(npcText);

                // Show dialogue options (unique to Friday mode — Captain Crow has 4 options)
                int optY = 354;
                for (int i = 0; i < Math.Min(npc.Options.Count, 4); i++)
                {
                    var optBtn = MakeBtn($"\u25B6 {npc.Options[i].Text}", 60, optY, 740, 32);
                    optBtn.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                    optBtn.TextAlign = ContentAlignment.MiddleLeft;
                    optBtn.Padding = new Padding(8, 0, 0, 0);
                    if (i == 3)  // highlight the unique 4th option
                        optBtn.BackColor = Color.FromArgb(60, 50, 75);
                    p.Controls.Add(optBtn);
                    optY += 38;
                }
            }

            // ── Side-by-side comparison label ─────────────────────────
            _actionLabel = Theme.MakeLabel(
                "\u25B6 Same engine: GameManager, Player, Room, Combat, Save/Load \u2014 different world + NPCs",
                0, 500, 880, 18, 9f, FontStyle.Bold, Color.Orange);
            _actionLabel.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(_actionLabel);
        }

        // ══════════════════════════════════════════════════════════════
        // STEP 10: DEMO COMPLETE — feature checklist
        // ══════════════════════════════════════════════════════════════

        private void BuildCompleteScreen()
        {
            var p = _contentPanel;

            var title = Theme.MakeLabel("\u2605  VIDEO DEMO COMPLETE  \u2605", 0, 20, 880, 40,
                22f, FontStyle.Bold, Theme.Gold);
            title.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(title);

            string[] checks =
            {
                "\u2705  Title screen / main menu",
                "\u2705  Mode selection (RPG vs Miss Friday \u2014 shared engine)",
                "\u2705  Save slot selection (Zelda-style 3 slots)",
                "\u2705  Player entering name / starting new game",
                "\u2705  Moving through 3 different rooms",
                "\u2705  Picking up item (Iron Sword) + equipping",
                "\u2705  Full combat encounter (Cave Goblin defeated)",
                "\u2705  Checking inventory with equipped items",
                "\u2705  Talking to NPC (Elder Mathis \u2014 branching dialogue)",
                "\u2705  Saving the game to Slot 1",
                "\u2705  Miss Friday mode preview (Captain Crow + pirate setting)"
            };

            int y = 70;
            foreach (var c in checks)
            {
                p.Controls.Add(Theme.MakeLabel(c, 120, y, 640, 22,
                    11f, FontStyle.Regular, Color.LimeGreen));
                y += 26;
            }

            y += 10;
            p.Controls.Add(Theme.MakeLabel("RPG Mode: Realm of Shadows  |  Friday Mode: Miss Friday's Adventure 2",
                120, y, 640, 24, 10f, FontStyle.Regular, Theme.Gold));
            p.Controls.Add(Theme.MakeLabel(".NET Framework 4.7.2 + WinForms",
                120, y + 26, 640, 20, 9f, FontStyle.Regular, Color.Gray));
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>Build the HUD bar used by multiple steps.</summary>
        private void BuildHudBar(Panel parent, Player pl)
        {
            var hud = new Panel
            {
                Location = new Point(10, 5), Size = new Size(860, 56),
                BackColor = Theme.BgPanel
            };
            parent.Controls.Add(hud);
            hud.Controls.Add(Theme.MakeLabel(pl.Name, 10, 4, 160, 22,
                12f, FontStyle.Bold, Theme.Gold));
            hud.Controls.Add(Theme.MakeLabel("HP:", 10, 30, 28, 18, 10f));
            var hpBg = new Panel { Location = new Point(40, 32), Size = new Size(200, 14),
                BackColor = Color.FromArgb(60, 20, 20) };
            hud.Controls.Add(hpBg);
            float hpPct = (float)pl.Health / pl.MaxHealth;
            hpBg.Controls.Add(new Panel { Location = new Point(0, 0),
                Size = new Size((int)(200 * hpPct), 14),
                BackColor = hpPct > 0.5f ? Theme.HPGreen : Color.Orange });
            hud.Controls.Add(Theme.MakeLabel($"{pl.Health}/{pl.MaxHealth}", 246, 30, 80, 18, 10f));
            hud.Controls.Add(Theme.MakeLabel($"ATK: {pl.TotalAttack}" +
                (pl.EquippedWeapon != null ? $" ({pl.EquippedWeapon.Name})" : ""),
                380, 8, 260, 18, 10f, FontStyle.Regular, Theme.ItemBlue));
            hud.Controls.Add(Theme.MakeLabel($"DEF: {pl.TotalDefense}" +
                (pl.EquippedArmor != null ? $" ({pl.EquippedArmor.Name})" : ""),
                380, 30, 260, 18, 10f, FontStyle.Regular, Theme.ItemBlue));
        }

        /// <summary>Create a styled button (display-only in demo).</summary>
        private static Button MakeBtn(string text, int x, int y, int w, int h,
            Color? bg = null)
        {
            var btn = Theme.MakeButton(text, x, y, w, h, null);
            if (bg.HasValue) btn.BackColor = bg.Value;
            return btn;
        }
    }
}
