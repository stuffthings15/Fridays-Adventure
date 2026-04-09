// ────────────────────────────────────────────────────────────
// TEXT RPG — Dialogue Screen
// Purpose: NPC dialogue with greeting text and branching
//          response options as clickable buttons.
// ────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextRPG.Screens
{
    /// <summary>
    /// Dialogue screen for NPC interaction. Shows the NPC's greeting
    /// and multiple response options. Clicking a response shows the
    /// NPC's reply, then offers to continue or leave.
    /// </summary>
    public class DialogueScreen : UserControl
    {
        private readonly ITextRPGHost _main;
        private readonly NPC _npc;

        private Label _npcName;
        private RichTextBox _dialogueBox;
        private Panel _optionsPanel;

        public DialogueScreen(ITextRPGHost main, NPC npc)
        {
            _main = main;
            _npc = npc;
            BackColor = Theme.BgDark;
            BuildUI();
            ShowGreeting();
        }

        private void BuildUI()
        {
            // NPC name banner
            var banner = new Panel
            {
                Location = new Point(20, 15), Size = new Size(840, 50),
                BackColor = Theme.BgPanel
            };
            Controls.Add(banner);

            _npcName = Theme.MakeLabel("\U0001F464 " + _npc.Name, 15, 8, 810, 34,
                16f, FontStyle.Bold, Theme.Gold);
            banner.Controls.Add(_npcName);

            // Dialogue text area
            _dialogueBox = new RichTextBox
            {
                Location = new Point(20, 80), Size = new Size(840, 200),
                BackColor = Theme.BgPanel, ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 13),
                ReadOnly = true, BorderStyle = BorderStyle.None
            };
            Controls.Add(_dialogueBox);

            // Options panel — holds dynamically created response buttons
            _optionsPanel = new Panel
            {
                Location = new Point(20, 300), Size = new Size(840, 280),
                BackColor = Color.Transparent
            };
            Controls.Add(_optionsPanel);
        }

        /// <summary>Display the NPC's greeting and response options.</summary>
        private void ShowGreeting()
        {
            _dialogueBox.Text = _npc.Greeting;
            ShowOptions();
        }

        /// <summary>Create a button for each dialogue option.</summary>
        private void ShowOptions()
        {
            _optionsPanel.Controls.Clear();

            int y = 10;
            foreach (var opt in _npc.Options)
            {
                var btn = Theme.MakeButton("\u25B6 " + opt.Text, 40, y, 760, 44, null);
                var capturedOpt = opt; // capture for closure
                btn.Click += (s, e) => ShowResponse(capturedOpt);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(10, 0, 0, 0);
                _optionsPanel.Controls.Add(btn);
                y += 54;
            }

            // "Leave" button at the bottom
            var leaveBtn = Theme.MakeButton("\u2190 Leave Conversation", 40, y + 10, 300, 40,
                (s, e) => _main.ShowScreen(new GameScreen(_main)));
            leaveBtn.BackColor = Color.FromArgb(80, 40, 40);
            _optionsPanel.Controls.Add(leaveBtn);
        }

        /// <summary>Show the NPC's response to the selected option.</summary>
        private void ShowResponse(DialogueOption opt)
        {
            _dialogueBox.Text = $"You: \"{opt.Text}\"\n\n{_npc.Name}: \"{opt.Response}\"";

            _optionsPanel.Controls.Clear();

            // After reading the response, offer to ask more or leave
            var moreBtn = Theme.MakeButton("Ask something else", 40, 10, 300, 44,
                (s, e) => { _dialogueBox.Text = _npc.Greeting; ShowOptions(); });
            _optionsPanel.Controls.Add(moreBtn);

            var leaveBtn = Theme.MakeButton("\u2190 Leave", 360, 10, 200, 44,
                (s, e) => _main.ShowScreen(new GameScreen(_main)));
            leaveBtn.BackColor = Color.FromArgb(80, 40, 40);
            _optionsPanel.Controls.Add(leaveBtn);
        }
    }
}
