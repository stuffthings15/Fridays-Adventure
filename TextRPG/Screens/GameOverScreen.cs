// ────────────────────────────────────────────────────────────
// TEXT RPG — Game Over / Victory Screen
// Purpose: Displays victory or defeat message with
//          Play Again and Quit buttons.
// ────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TextRPG.Screens
{
    /// <summary>
    /// End-of-game screen. Shows a victory or defeat message
    /// with options to restart or quit.
    /// </summary>
    public class GameOverScreen : UserControl
    {
        private readonly ITextRPGHost _main;
        private readonly bool _victory;

        public GameOverScreen(ITextRPGHost main, bool victory)
        {
            _main = main;
            _victory = victory;
            BackColor = Theme.BgDark;
            BuildUI();
        }

        private void BuildUI()
        {
            if (_victory)
            {
                // ── Victory ──────────────────────────────────────
                var stars = Theme.MakeLabel("\u2605 \u2605 \u2605", 0, 80, 880, 50,
                    30f, FontStyle.Bold, Theme.Gold);
                stars.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(stars);

                var title = Theme.MakeLabel("VICTORY!", 0, 140, 880, 60,
                    40f, FontStyle.Bold, Theme.Gold);
                title.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(title);

                // Show mode-appropriate victory message
                string bossName = _main.Game.Mode == GameMode.MissFriday ? "Sea Serpent" : "Shadow Dragon";
                var msg = Theme.MakeLabel(
                    $"You have slain the {bossName} and saved the realm!\n" +
                    "Your name will be remembered for generations.",
                    0, 220, 880, 60, 13f, FontStyle.Regular, Theme.TextLight);
                msg.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(msg);

                // Bonus reward message for fun
                var bonusMsg = Theme.MakeLabel(
                    "\u2726 BONUS: You unlocked the Legendary Hero title! \u2726",
                    0, 275, 880, 30, 11f, FontStyle.Bold, Theme.Gold);
                bonusMsg.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(bonusMsg);

                var statsLabel = Theme.MakeLabel(
                    $"Final Stats  \u2014  HP: {_main.Game.Player.Health}/{_main.Game.Player.MaxHealth}" +
                    $"  |  ATK: {_main.Game.Player.TotalAttack}" +
                    $"  |  DEF: {_main.Game.Player.TotalDefense}" +
                    $"  |  Items: {_main.Game.Player.Inventory.Count}",
                    0, 310, 880, 30, 10f, FontStyle.Regular, Theme.ItemBlue);
                statsLabel.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(statsLabel);
            }
            else
            {
                // ── Defeat ───────────────────────────────────────
                var skull = Theme.MakeLabel("\u2620", 0, 90, 880, 60,
                    40f, FontStyle.Bold, Theme.HPRed);
                skull.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(skull);

                var title = Theme.MakeLabel("DEFEAT", 0, 160, 880, 50,
                    36f, FontStyle.Bold, Theme.HPRed);
                title.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(title);

                var msg = Theme.MakeLabel(
                    "You have fallen in battle.\nThe realm remains in darkness...",
                    0, 230, 880, 50, 13f, FontStyle.Regular, Theme.TextLight);
                msg.TextAlign = ContentAlignment.MiddleCenter;
                Controls.Add(msg);
            }

            // ── Buttons ──────────────────────────────────────────
            int bx = (880 - 250) / 2;

            var playAgainBtn = Theme.MakeButton("Play Again", bx, 400, 250, 50,
                (s, e) => _main.ShowScreen(new TitleScreen(_main)));
            Controls.Add(playAgainBtn);

            var quitBtn = Theme.MakeButton("Quit", bx, 470, 250, 50,
                (s, e) => _main.Close());
            Controls.Add(quitBtn);
        }
    }
}
